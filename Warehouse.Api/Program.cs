using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Warehouse.Api.Authentication;
using Warehouse.Api.Authorization;
using Warehouse.Api.Middleware;
using Warehouse.Application.Abstractions;
using Warehouse.Application.Services;
using Warehouse.Infrastructure;
using Warehouse.Infrastructure.Seeding;

namespace Warehouse.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();

        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<ApiExceptionHandler>();

        builder.Services.AddInfrastructure(builder.Configuration);

        builder.Services.AddScoped<IAuthService, AuthService>();

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
                "a secret of at least 32 bytes, and expiration of 1–120 minutes.");
        }

        builder.Services.AddSingleton(jwt);
        builder.Services.AddSingleton<ITokenService, JwtTokenService>();

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

                        if (!await identity.IsUserAllowedAsync(
                            userId,
                            context.HttpContext.RequestAborted))
                        {
                            context.Fail("User is unavailable.");
                        }
                    },

                    OnChallenge = async context =>
                    {
                        context.HandleResponse();

                        await Results.Problem(
                            statusCode: 401,
                            title: "Unauthorized",
                            detail: "A valid access token is required.")
                            .ExecuteAsync(context.HttpContext);
                    },

                    OnForbidden = async context =>
                    {
                        await Results.Problem(
                            statusCode: 403,
                            title: "Forbidden",
                            detail: "You do not have the required permission.")
                            .ExecuteAsync(context.HttpContext);
                    }
                };
            });

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

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Warehouse Management API",
                Version = "v1"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Paste the access token only."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

        var app = builder.Build();

        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            if (app.Configuration.GetValue<bool>("Seed:Enabled"))
            {
                var password = app.Configuration["Seed:Password"];

                if (string.IsNullOrWhiteSpace(password))
                {
                    throw new InvalidOperationException(
                        "Seed:Password is required when seeding is enabled.");
                }

                await using var scope =
                    app.Services.CreateAsyncScope();

                var seeder = scope.ServiceProvider
                    .GetRequiredService<IdentitySeeder>();

                await seeder.SeedAsync(
                    password,
                    CancellationToken.None);
            }

            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        await app.RunAsync();
    }
}