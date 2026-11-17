using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Warehouse.Api.Authentication;
using Warehouse.Api.Authorization;
using Warehouse.Api.Middleware;
using Warehouse.Application;
using Warehouse.Application.Abstractions;
using Warehouse.Infrastructure;
using Warehouse.Infrastructure.Persistence;
using Warehouse.Infrastructure.Seeding;

namespace Warehouse.Api;

public partial class Program
{
    private const string DisabledAccountMessage =
        "Your account has been disabled. Please contact support.";

    private const string CustomerPasswordRateLimitPolicy =
        "customer-password";

    // Set only after validating the JWT and checking the database.
    private static readonly object DisabledAccountKey = new();

    public static async Task Main(string[] args)
    {
        #region Bootstrap Logger

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        #endregion

        try
        {
            var builder = WebApplication.CreateBuilder(args);

            #region Serilog

            builder.Services.AddSerilog((services, configuration) =>
            {
                configuration
                    .ReadFrom.Configuration(builder.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.WithProperty(
                        "Environment",
                        builder.Environment.EnvironmentName);
            });

            #endregion

            #region Controllers And Error Handling

            builder.Services.AddControllers();

            builder.Services.AddProblemDetails();

            builder.Services.AddExceptionHandler<ApiExceptionHandler>();

            #endregion

            #region Application And Infrastructure

            builder.Services.AddApplication();

            builder.Services.AddInfrastructure(builder.Configuration);

            // Registers password invitation services, SMTP worker,
            // token lifetime and persistent Data Protection keys.
            builder.Services.AddCustomerPasswords(
                builder.Configuration);

            builder.Services.AddHttpContextAccessor();

            #endregion

            #region Rate Limiting

            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode =
                    StatusCodes.Status429TooManyRequests;

                options.AddPolicy(
                    CustomerPasswordRateLimitPolicy,
                    context =>
                        RateLimitPartition.GetFixedWindowLimiter(
                            partitionKey:
                                context.Connection.RemoteIpAddress?
                                    .ToString()
                                ?? "unknown",
                            factory: _ =>
                                new FixedWindowRateLimiterOptions
                                {
                                    PermitLimit = 10,
                                    Window = TimeSpan.FromMinutes(1),
                                    QueueLimit = 0,
                                    AutoReplenishment = true
                                }));

                options.OnRejected = async (context, cancellationToken) =>
                {
                    if (context.HttpContext.Response.HasStarted)
                    {
                        return;
                    }

                    if (context.Lease.TryGetMetadata(
                            MetadataName.RetryAfter,
                            out var retryAfter))
                    {
                        context.HttpContext.Response.Headers[
                            "Retry-After"] =
                            Math.Max(
                                    1,
                                    (int)Math.Ceiling(
                                        retryAfter.TotalSeconds))
                                .ToString(
                                    System.Globalization
                                        .CultureInfo.InvariantCulture);
                    }

                    await Results.Problem(
                            statusCode:
                                StatusCodes.Status429TooManyRequests,
                            title: "Too many requests",
                            detail:
                                "Too many attempts. Please try again later.")
                        .ExecuteAsync(context.HttpContext);
                };
            });

            #endregion

            #region JWT Configuration

            var jwt = builder.Configuration
                .GetSection("Jwt")
                .Get<JwtSettings>()
                ?? throw new InvalidOperationException(
                    "JWT configuration is missing.");

            if (string.IsNullOrWhiteSpace(jwt.Issuer) ||
                string.IsNullOrWhiteSpace(jwt.Audience) ||
                string.IsNullOrWhiteSpace(jwt.Secret) ||
                Encoding.UTF8.GetByteCount(jwt.Secret) < 32 ||
                jwt.ExpirationMinutes is < 1 or > 120)
            {
                throw new InvalidOperationException(
                    "Invalid JWT settings. Configure issuer, audience, " +
                    "a secret of at least 32 bytes, and expiration " +
                    "of 1–120 minutes.");
            }

            builder.Services.AddSingleton(jwt);

            builder.Services.AddSingleton<
                ITokenService,
                JwtTokenService>();

            #endregion

            #region Authentication

            builder.Services
                .AddAuthentication(
                    JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.MapInboundClaims = false;

                    options.TokenValidationParameters =
                        new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,

                            RequireSignedTokens = true,
                            RequireExpirationTime = true,

                            ValidIssuer = jwt.Issuer,
                            ValidAudience = jwt.Audience,

                            IssuerSigningKey = new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(jwt.Secret)),

                            ValidAlgorithms =
                            [
                                SecurityAlgorithms.HmacSha256
                            ],

                            NameClaimType = "name",

                            ClockSkew = TimeSpan.FromSeconds(30)
                        };

                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            var userId = context.Principal?
                                .FindFirst("sub")?.Value;

                            if (string.IsNullOrWhiteSpace(userId))
                            {
                                context.Fail("Invalid user.");
                                return;
                            }

                            var services = context.HttpContext
                                .RequestServices;

                            var cancellationToken = context.HttpContext
                                .RequestAborted;

                            var dbContext = services
                                .GetRequiredService<ApplicationDbContext>();

                            // Read the current customer status on every
                            // authenticated request.
                            var isDisabledRider = await dbContext.Riders
                                .AsNoTracking()
                                .AnyAsync(
                                    rider =>
                                        rider.AccountId == userId &&
                                        !rider.IsAvailable,
                                    cancellationToken);

                            if (isDisabledRider)
                            {
                                context.HttpContext.Items[
                                    DisabledAccountKey] = true;

                                context.Fail(DisabledAccountMessage);
                                return;
                            }

                            // Preserve the existing Identity checks.
                            var identity = services
                                .GetRequiredService<IIdentityService>();

                            var isAllowed =
                                await identity.IsUserAllowedAsync(
                                    userId,
                                    cancellationToken);

                            if (!isAllowed)
                            {
                                context.Fail("User is unavailable.");
                            }
                        },

                        OnChallenge = async context =>
                        {
                            context.HandleResponse();

                            if (context.Response.HasStarted)
                            {
                                return;
                            }

                            context.Response.Headers[
                                "WWW-Authenticate"] = "Bearer";

                            var isDisabledAccount =
                                context.HttpContext.Items.ContainsKey(
                                    DisabledAccountKey);

                            var detail = isDisabledAccount
                                ? DisabledAccountMessage
                                : "A valid access token is required.";

                            await Results.Problem(
                                    statusCode:
                                        StatusCodes.Status401Unauthorized,
                                    title: "Unauthorized",
                                    detail: detail)
                                .ExecuteAsync(context.HttpContext);
                        },

                        OnForbidden = async context =>
                        {
                            if (context.Response.HasStarted)
                            {
                                return;
                            }

                            await Results.Problem(
                                    statusCode:
                                        StatusCodes.Status403Forbidden,
                                    title: "Forbidden",
                                    detail:
                                        "You do not have the required permission.")
                                .ExecuteAsync(context.HttpContext);
                        }
                    };
                });

            #endregion

            #region Authorization

            builder.Services.AddAuthorization(options =>
            {
                options.FallbackPolicy =
                    new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build();
            });

            builder.Services.AddSingleton<
                IAuthorizationPolicyProvider,
                PermissionPolicyProvider>();

            builder.Services.AddScoped<
                IAuthorizationHandler,
                PermissionAuthorizationHandler>();

            #endregion

            #region Swagger

            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Warehouse Management API",
                    Version = "v1",
                    Description =
                        "Warehouse and customer management with JWT, " +
                        "dynamic permission-based authorization " +
                        "and customer password invitations."
                });

                options.AddSecurityDefinition(
                    "Bearer",
                    new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        Description = "Paste the access token only."
                    });

                options.AddSecurityRequirement(
                    new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            Array.Empty<string>()
                        }
                    });
            });

            #endregion

            var app = builder.Build();

            #region Request Logging

            app.UseSerilogRequestLogging(options =>
            {
                options.MessageTemplate =
                    "HTTP {RequestMethod} {RequestPath} responded " +
                    "{StatusCode} in {Elapsed:0.0000} ms";

                options.EnrichDiagnosticContext =
                    (diagnosticContext, httpContext) =>
                    {
                        diagnosticContext.Set(
                            "RequestId",
                            httpContext.TraceIdentifier);
                    };
            });

            #endregion

            #region Exception Handling

            app.UseExceptionHandler();

            #endregion

            #region Development Seeding And Swagger

            if (app.Environment.IsDevelopment())
            {
                if (app.Configuration.GetValue<bool>("Seed:Enabled"))
                {
                    var password =
                        app.Configuration["Seed:Password"];

                    if (string.IsNullOrWhiteSpace(password))
                    {
                        throw new InvalidOperationException(
                            "Seed:Password is required " +
                            "when seeding is enabled.");
                    }

                    app.Logger.LogInformation(
                        "Starting development Identity seeding");

                    await using var scope =
                        app.Services.CreateAsyncScope();

                    var seeder = scope.ServiceProvider
                        .GetRequiredService<IdentitySeeder>();

                    await seeder.SeedAsync(
                        password,
                        CancellationToken.None);

                    app.Logger.LogInformation(
                        "Development Identity seeding completed");
                }

                app.UseSwagger();

                app.UseSwaggerUI();
            }

            #endregion

            #region HTTP Pipeline

            app.UseHttpsRedirection();

            // Endpoint-specific rate limiting requires routing first.
            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseRateLimiter();

            app.MapControllers();

            #endregion

            app.Logger.LogInformation(
                "Starting {Application} in {Environment}",
                "Warehouse.Api",
                app.Environment.EnvironmentName);

            await app.RunAsync();
        }
        catch (Exception exception)
            when (exception is not HostAbortedException)
        {
            Log.Fatal(
                exception,
                "Warehouse API terminated unexpectedly");

            throw;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}