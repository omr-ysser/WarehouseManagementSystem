using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Warehouse.Api.Authentication;
using Warehouse.Api.Authorization;
using Warehouse.Api.Middleware;
using Warehouse.Application;
using Warehouse.Application.Abstractions;
using Warehouse.Infrastructure;
using Warehouse.Infrastructure.Seeding;

namespace Warehouse.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Captures errors that happen before the full logger is configured.
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            var builder = WebApplication.CreateBuilder(args);

            // Serilog: configuration, DI enrichers and configured sinks.
            builder.Services.AddSerilog((services, configuration) =>
            {
                configuration
                    .ReadFrom.Configuration(builder.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.WithProperty(
                        "Environment",
                        builder.Environment.EnvironmentName);
            });

            // Controllers and global error handling.
            builder.Services.AddControllers();

            builder.Services.AddProblemDetails();
            builder.Services.AddExceptionHandler<ApiExceptionHandler>();

            // Application services and existing FluentValidation registration.
            builder.Services.AddApplication();

            // Database, Identity, repositories and seeder.
            builder.Services.AddInfrastructure(builder.Configuration);

            // JWT settings.
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

            // JWT authentication.
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

                            var identity = context.HttpContext
                                .RequestServices
                                .GetRequiredService<IIdentityService>();

                            var isAllowed =
                                await identity.IsUserAllowedAsync(
                                    userId,
                                    context.HttpContext.RequestAborted);

                            if (!isAllowed)
                            {
                                context.Fail("User is unavailable.");
                            }
                        },

                        OnChallenge = async context =>
                        {
                            context.HandleResponse();

                            context.Response.Headers[
                                "WWW-Authenticate"] = "Bearer";

                            await Results.Problem(
                                statusCode:
                                    StatusCodes.Status401Unauthorized,
                                title: "Unauthorized",
                                detail:
                                    "A valid access token is required.")
                                .ExecuteAsync(context.HttpContext);
                        },

                        OnForbidden = async context =>
                        {
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

            // Authentication is required unless [AllowAnonymous] is applied.
            builder.Services.AddAuthorization(options =>
            {
                options.FallbackPolicy =
                    new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build();
            });

            // Dynamic permission policies.
            builder.Services.AddSingleton<
                IAuthorizationPolicyProvider,
                PermissionPolicyProvider>();

            builder.Services.AddScoped<
                IAuthorizationHandler,
                PermissionAuthorizationHandler>();

            builder.Services.AddHttpContextAccessor();

            // Swagger.
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Warehouse Management API",
                    Version = "v1",
                    Description =
                        "Warehouse and stock management with JWT " +
                        "and dynamic permission-based authorization."
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
                                    Type =
                                        ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            Array.Empty<string>()
                        }
                    });
            });

            var app = builder.Build();

            // One structured completion event for each HTTP request.
            // Runs outside the exception handler so handled errors
            // are recorded with their resulting response status.
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

            // Existing global exception handling.
            app.UseExceptionHandler();

            // Development-only seeding and Swagger.
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

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Logger.LogInformation(
                "Starting {Application} in {Environment}",
                "Warehouse.Api",
                app.Environment.EnvironmentName);

            await app.RunAsync();
        }
        catch (Exception exception)
        {
            Log.Fatal(
                exception,
                "Warehouse API terminated unexpectedly");

            // Preserve the failure exit status for tooling and hosting.
            throw;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}