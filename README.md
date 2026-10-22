# Warehouse Management API

ASP.NET Core Web API for managing warehouses, products, stock transactions, users, roles, and permissions. Uses SQL Server, ASP.NET Core Identity, JWT authentication, and database-backed permission authorization.

## Technology

- .NET 9 / ASP.NET Core, C#
- Entity Framework Core and Microsoft SQL Server
- ASP.NET Core Identity
- JWT Bearer authentication (HS256)
- FluentValidation
- Serilog structured logging and Seq sink
- Swagger / OpenAPI 3

This README describes the supplied source and the logging/validation updates provided alongside it. Apply those updates before following their verification steps. A successful runtime start and delivery to Seq have not yet been confirmed from the supplied logs. No automated test suite or project files were supplied for independent build verification.

## Architecture

| Project | Responsibility |
| --- | --- |
| Warehouse.Domain | Product, Warehouse, WarehouseStock, StockTransaction entities |
| Warehouse.Application | DTOs, abstractions, AuthService, InventoryService, validators, permission catalog |
| Warehouse.Infrastructure | EF Core persistence, Identity, repositories, configurations, development seeding |
| Warehouse.Api | Controllers, JWT setup, authorization handlers, exception handling, Swagger, logging configuration |

InventoryService validates commands before persistence. Infrastructure implements the identity and data abstractions. Controllers apply permission policies and delegate operations to services.

## Prerequisites

- .NET 9 SDK (the supplied application targets net9.0).
- SQL Server accessible to the application process.
- Visual Studio 2022 with suitable .NET support, or a terminal/editor.
- Docker Desktop in Linux-container mode, if running Seq with the command below.
- dotnet-ef 9.x for EF Core commands; the relevant project must reference Microsoft.EntityFrameworkCore.Design.

Commands below use PowerShell from the solution root and assume the project folder names shown above.

## 1. Restore and configure

```powershell
dotnet restore
```

Default development connection string in `Warehouse.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=WarehouseManagementDb;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Jwt": {
    "Issuer": "Warehouse.Api",
    "Audience": "Warehouse.Client",
    "ExpirationMinutes": 30
  }
}
```

Adjust `Server` for your SQL Server instance. Windows authentication uses the account running the API. `TrustServerCertificate=True` is a local-development setting.

Initialize User Secrets if the API project does not already have a UserSecretsId:

```powershell
dotnet user-secrets init --project .\Warehouse.Api\Warehouse.Api.csproj
```

Generate a random JWT signing secret and store it without printing it:

```powershell
$warehouseJwtSecret = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
dotnet user-secrets set "Jwt:Secret" "$warehouseJwtSecret" --project .\Warehouse.Api\Warehouse.Api.csproj
Remove-Variable warehouseJwtSecret
```

JWT startup validation requires an issuer, audience, signing secret of at least 32 UTF-8 bytes, and expiration between 1 and 120 minutes. Keep the signing secret out of source control. Outside Development, provide secrets through your hosting environment (for example `Jwt__Secret`), because User Secrets are development-only configuration.

## 2. Prepare the database

Install the EF tool if it is not already available:

```powershell
dotnet tool install --global dotnet-ef --version "9.*"
```

Apply existing migrations:

```powershell
dotnet ef database update --project .\Warehouse.Infrastructure\Warehouse.Infrastructure.csproj --startup-project .\Warehouse.Api\Warehouse.Api.csproj --context ApplicationDbContext
```

If the repository has no migrations yet, create the initial migration first:

```powershell
dotnet ef migrations add InitialCreate --project .\Warehouse.Infrastructure\Warehouse.Infrastructure.csproj --startup-project .\Warehouse.Api\Warehouse.Api.csproj --context ApplicationDbContext
```

Then run `database update`. Do not create a second initial migration if migrations already exist. The supplied seeder expects the schema to exist; it does not apply migrations itself.

## 3. Enable development accounts

The seeder runs only in Development when `Seed:Enabled` is true.

```powershell
dotnet user-secrets set "Seed:Enabled" "true" --project .\Warehouse.Api\Warehouse.Api.csproj
$warehouseSeedPassword = Read-Host "Enter a strong LOCAL demo password"
dotnet user-secrets set "Seed:Password" "$warehouseSeedPassword" --project .\Warehouse.Api\Warehouse.Api.csproj
Remove-Variable warehouseSeedPassword
```

Choose at least 8 characters with uppercase, lowercase, a digit, and a non-alphanumeric character. The password prompt above is visible input; use it only on a private local terminal.

| Role | Email | Initial permissions |
| --- | --- | --- |
| Admin | admin@example.com | All built-in permissions |
| Supervisor | supervisor@example.com | Warehouses.View, Products.View, Transactions.View, Transactions.Create, Stock.View |
| Vendor | vendor@example.com | Products.View, Stock.View |
| Customer | customer@example.com | Products.View |

New demo accounts use the configured Seed:Password. Startup does not reset existing account passwords or existing role permission assignments. Newly added custom permissions are not automatically granted to existing Admin roles.

Disable seeding when it is no longer needed:

```powershell
dotnet user-secrets set "Seed:Enabled" "false" --project .\Warehouse.Api\Warehouse.Api.csproj
```

## 4. Start Seq locally

Start Docker Desktop and wait until its Linux engine is running. Check both Client and Server output:

```powershell
docker version
```

Create a local Seq container:

```powershell
docker run --name warehouse-seq -d --restart unless-stopped -e ACCEPT_EULA=Y -e SEQ_FIRSTRUN_NOAUTHENTICATION=True -p 127.0.0.1:8081:80 -p 127.0.0.1:5341:5341 -v warehouse-seq-data:/data datalust/seq:latest
```

- UI: http://localhost:8081
- Serilog ingestion URL: http://localhost:5341
- Persistent data: Docker volume `warehouse-seq-data`.

This configuration disables Seq authentication and binds ports to localhost for development. Configure authentication and access controls for shared deployments. Pin an image version for reproducible deployments.

If the container already exists:

```powershell
docker start warehouse-seq
docker ps
```

The ingestion URL assumes the API runs on the host. An API running inside a container needs an address reachable from its container network.

## 5. Logging configuration

Install Serilog packages in Warehouse.Api, using the ASP.NET Core integration matching .NET 9:

```powershell
dotnet add .\Warehouse.Api\Warehouse.Api.csproj package Serilog.AspNetCore --version "9.*"
dotnet add .\Warehouse.Api\Warehouse.Api.csproj package Serilog.Settings.Configuration
dotnet add .\Warehouse.Api\Warehouse.Api.csproj package Serilog.Sinks.Console
dotnet add .\Warehouse.Api\Warehouse.Api.csproj package Serilog.Sinks.Seq
```

Merge this section into `appsettings.json`:

```json
"Serilog": {
  "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.Seq"],
  "MinimumLevel": {
    "Default": "Information",
    "Override": {
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "Microsoft.EntityFrameworkCore": "Warning",
      "System": "Warning"
    }
  },
  "Enrich": ["FromLogContext"],
  "Properties": { "Application": "Warehouse.Api" },
  "WriteTo": [
    { "Name": "Console" },
    { "Name": "Seq", "Args": { "serverUrl": "http://localhost:5341" } }
  ]
}
```

This is a JSON property fragment, not a complete replacement configuration file. Program.cs must also register AddSerilog, enable UseSerilogRequestLogging, and flush the logger on shutdown, as in the provided updated Program.cs.

Use message templates for searchable properties:

```csharp
_logger.LogInformation(
    "Product {ProductId} created with SKU {SKU}",
    product.Id,
    product.SKU);
```

The updated services log successful writes after saving and stock transaction events after commit. Do not log passwords, signing keys, access tokens, or complete login requests.

Example Seq filters:

```sql
Application = 'Warehouse.Api'
```

```sql
SourceContext = 'Warehouse.Application.Services.InventoryService'
```

```sql
ProductId = 1
```

## 6. Validation configuration

```powershell
dotnet add .\Warehouse.Application\Warehouse.Application.csproj package FluentValidation.DependencyInjectionExtensions
dotnet add .\Warehouse.Application\Warehouse.Application.csproj package Microsoft.Extensions.Logging.Abstractions --version "9.*"
```

The updated AddApplication registers AuthService, InventoryService, and validators with AddValidatorsFromAssemblyContaining<LoginRequestValidator>(). Keep a single definition of AddApplication.

Services call ValidateAndThrowAsync; ApiExceptionHandler converts ValidationException into a 400 ValidationProblemDetails response. Do not run the same service validation again in controllers.

| Input | Rules |
| --- | --- |
| Login | Required valid email, maximum 256 characters; required password, maximum 128 |
| Warehouse | Name contains 2–100 characters after trimming |
| Product | Name 2–150 and SKU 2–50 characters after trimming; description maximum 1000 |
| Stock transaction | Positive warehouse/product IDs; nonzero signed quantity fitting decimal(18,3) |
| Transaction update/delete | Base64 RowVersion decoding to 8 bytes |

Identity-related DTOs also use Data Annotations, and identity services enforce account/business rules. These checks are separate from the inventory validators.

## 7. Build and run

```powershell
dotnet build
dotnet dev-certs https --trust
dotnet run --project .\Warehouse.Api\Warehouse.Api.csproj --launch-profile https
```

Keep the process running. Successful startup should show `Now listening` and `Application started`.

| Resource | Local URL |
| --- | --- |
| Swagger UI | https://localhost:7284/swagger |
| OpenAPI document | https://localhost:7284/swagger/v1/swagger.json |
| HTTPS API | https://localhost:7284 |
| HTTP listener | http://localhost:5188 |
| Seq UI | http://localhost:8081 |

Swagger and seeding are Development-only. The HTTP pipeline uses HTTPS redirection.

## Authentication and authorization

1. POST credentials to `/api/auth/login`.
2. Copy `accessToken` from the response.
3. In Swagger, select **Authorize** and paste only the token.
4. Other clients send `Authorization: Bearer <accessToken>`.
5. GET `/api/me` to inspect the current account and permissions.

Authentication is required by default; login is anonymous. JWT validation checks signature, issuer, audience, lifetime, and the user ID. Each validated token is also checked against the user store; missing or locked-out users are rejected.

Policies use names such as `Permission:Products.Create`. PermissionAuthorizationHandler queries current database role-permission assignments. Role/permission changes therefore affect subsequent permission checks without reissuing the JWT. Defining a new permission does not automatically attach it to an endpoint; the endpoint must require the corresponding policy.

No refresh-token, logout, or public self-registration endpoint is exposed in the supplied controllers. New accounts are created through the permission-protected Users API.

## API endpoints

| Group | Method | Route |
| --- | --- | --- |
| Auth | POST | /api/auth/login |
| Me | GET | /api/me |
| Permissions | GET, POST | /api/permissions |
| Permissions | PUT, DELETE | /api/permissions/{id} |
| Roles | GET, POST | /api/roles |
| Roles | PUT | /api/roles/{id} |
| Roles | PUT | /api/roles/{id}/permissions |
| Roles | GET | /api/roles/{id}/users |
| Users | GET, POST | /api/users |
| Users | GET, PUT | /api/users/{id} |
| Users | PUT | /api/users/{id}/role |
| Products | GET, POST | /api/products |
| Products | GET, PUT, DELETE | /api/products/{id} |
| Products | GET | /api/products/{id}/transactions |
| Warehouses | GET, POST | /api/warehouses |
| Warehouses | GET, PUT, DELETE | /api/warehouses/{id} |
| Warehouses | GET | /api/warehouses/{id}/stock |
| Warehouses | GET | /api/warehouses/{warehouseId}/stock/{productId} |
| Warehouses | GET | /api/warehouses/{id}/transactions |
| Transactions | GET, POST | /api/transactions |
| Transactions | GET, PUT, DELETE | /api/transactions/{id} |

See Swagger for request/response schemas. Role and user IDs are strings; inventory and permission IDs are integers. No pagination/search parameters are exposed by the supplied product list or transaction list actions.

## Example workflow

Login with a seeded Admin account (replace the password):

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "admin@example.com",
  "password": "<your-local-seed-password>"
}
```

Create a warehouse, then a product. Use returned IDs rather than assuming they are 1.

```json
{ "name": "Main Warehouse" }
```

```json
{
  "name": "Wireless Mouse",
  "sku": "MOUSE-001",
  "description": "Wireless optical mouse"
}
```

POST `/api/transactions` to receive stock:

```json
{
  "warehouseId": 1,
  "productId": 1,
  "quantity": 100
}
```

POST another transaction with quantity `-10` to issue stock. On a new balance of 100, the resulting balance should be 90. Query `/api/warehouses/1/stock/1` to verify it.

For PUT `/api/transactions/{id}`, include warehouseId, productId, quantity, and the latest `rowVersion` returned by GET. Updates reverse the old balance effect and apply the new effect within a database transaction.

DELETE `/api/transactions/{id}` takes `RowVersion` in the **query string**, not a JSON body. Supply the latest Base64 value using Swagger so special characters are URL-encoded correctly. Deletion reverses the original balance effect and may be rejected if it would make stock negative.

## Inventory rules and errors

- Quantities are signed: positive receipts and negative issues.
- Balances cannot become negative or exceed the supported decimal range.
- Warehouse names and product SKUs are normalized for uniqueness checks.
- Products/warehouses with stock rows or transaction dependencies cannot be deleted.
- Updates/deletes use transaction RowVersion checks; conflicts require reloading data.
- Stock movement and balance changes are committed together.
- Transaction boundaries and RowVersion checks do not, by themselves, establish complete concurrency safety for every simultaneous balance operation; verify concurrent stock scenarios against SQL Server before relying on them in production.

| Status | Meaning |
| --- | --- |
| 200 | Successful read, login, or transaction update |
| 201 | Resource created |
| 204 | Successful operation with no response body |
| 400 | Invalid input or business-rule rejection |
| 401 | Missing/invalid/expired token, rejected user, or invalid login |
| 403 | Authenticated user lacks the required permission |
| 404 | Resource not found |
| 409 | Duplicate, dependency, or concurrency conflict |
| 500 | Unexpected server error |

ApiExceptionHandler returns Problem Details with a traceId for handled errors. FluentValidation errors additionally include an errors dictionary. Unexpected exceptions are logged without exposing internal exception details to clients.

## Manual verification

- Login and GET `/api/me` with Admin.
- Call a protected endpoint without a token: expect 401.
- Use Customer to attempt product creation: expect 403.
- Create a product with an invalid name/SKU: expect 400 and field errors.
- Create a valid product: expect 201 and a structured ProductId event in Seq.
- Submit a duplicate SKU: expect 409.
- Receive stock, issue stock, and check the balance.
- Attempt an issue larger than available stock: expect 400 and no persisted balance change.
- Edit/delete using a stale RowVersion: expect 409.
- Change a role permission and verify the next protected request reflects it.
- Exercise simultaneous stock operations separately to assess concurrency behavior.

## Troubleshooting

| Symptom | Action |
| --- | --- |
| dockerDesktopLinuxEngine pipe not found | Start Docker Desktop, wait for the Linux engine, then retry docker version |
| Container name already in use | Run docker start warehouse-seq; inspect docker ps -a |
| Cannot find a project | Run from the solution root or provide the correct .csproj path |
| Invalid JWT settings at startup | Set Jwt:Secret for Warehouse.Api and confirm issuer/audience/expiration |
| SQL connection failure | Verify SQL Server service, instance name, credentials, and database access |
| Seed failure | Apply migrations first; use an Identity-compliant password |
| No events in Seq | Verify the container, ingestion URL, Serilog registration/configuration, and generate an API request |
| Validator cannot be resolved | Register the validator assembly in AddApplication and avoid duplicate/missing registrations |
| Visual Studio shows only Loaded DLL / Skipped symbols | These are debugger messages; run dotnet run in a terminal to see actual application logs |
| Process exits with 0xffffffff | That code alone does not identify the cause; capture the terminal exception/startup output |

## Scope

This repository is an API backend. The supplied source does not include a frontend, refresh-token flow, deployment pipeline, or automated test suite. Add environment-specific deployment settings and verification appropriate to your hosting environment. No license was supplied with the source reviewed for this README.
