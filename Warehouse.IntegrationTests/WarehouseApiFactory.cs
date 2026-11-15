using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Warehouse.Api;
using Warehouse.Application.Abstractions;
using Warehouse.Application.Services;

namespace Warehouse.IntegrationTests;

public class WarehouseApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // إعدادات خاصة بالاختبار.
        builder.ConfigureAppConfiguration((context, configuration) =>
        {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Jwt:Issuer"] = "Warehouse.Tests",
                    ["Jwt:Audience"] = "Warehouse.Tests.Client",
                    ["Jwt:Secret"] =
                        "Warehouse-Integration-Tests-Only-Secret-123456789!",
                    ["Jwt:ExpirationMinutes"] = "30",
                    ["Seed:Enabled"] = "false"
                });
        });

        builder.ConfigureTestServices(services =>
        {
            // 1. استبدال قراءة المخزون ببيانات ثابتة للاختبار.
            services.RemoveAll<IStockReader>();
            services.RemoveAll<StockAvailabilityService>();

            services.AddScoped<IStockReader, TestStockReader>();
            services.AddScoped<StockAvailabilityService>();

            // 2. تسجيل Authentication خاصة بالاختبار.
            services
                .AddAuthentication()
                .AddScheme<
                    AuthenticationSchemeOptions,
                    TestAuthenticationHandler>(
                    TestAuthenticationHandler.SchemeName,
                    _ => { });

            // 3. اختيار Test كـAuthentication الافتراضية.
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultScheme =
                    TestAuthenticationHandler.SchemeName;

                options.DefaultAuthenticateScheme =
                    TestAuthenticationHandler.SchemeName;

                options.DefaultChallengeScheme =
                    TestAuthenticationHandler.SchemeName;

                options.DefaultForbidScheme =
                    TestAuthenticationHandler.SchemeName;
            });

            // 4. استبدال PermissionPolicyProvider داخل الاختبار فقط.
            services.RemoveAll<IAuthorizationPolicyProvider>();

            services.AddSingleton<
                IAuthorizationPolicyProvider,
                DefaultAuthorizationPolicyProvider>();

            // 5. ربط Authorization صراحةً بـTest Authentication.
            services.PostConfigure<AuthorizationOptions>(options =>
            {
                var testPolicy = new AuthorizationPolicyBuilder(
                        TestAuthenticationHandler.SchemeName)
                    .RequireAuthenticatedUser()
                    .Build();

                options.DefaultPolicy = testPolicy;
                options.FallbackPolicy = testPolicy;
            });
        });
    }

    private sealed class TestStockReader : IStockReader
    {
        public Task<int> GetAvailableStockAsync(int productId)
        {
            return Task.FromResult(10);
        }
    }
}

public sealed class TestAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";

    public TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim("sub", "integration-test-user"),

            new Claim(
                ClaimTypes.NameIdentifier,
                "integration-test-user"),

            new Claim(
                ClaimTypes.Name,
                "Integration Test User")
        };

        var identity = new ClaimsIdentity(
            claims,
            SchemeName);

        var principal = new ClaimsPrincipal(identity);

        var ticket = new AuthenticationTicket(
            principal,
            SchemeName);

        return Task.FromResult(
            AuthenticateResult.Success(ticket));
    }
}