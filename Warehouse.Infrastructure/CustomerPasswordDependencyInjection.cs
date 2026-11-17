using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net.Mail;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Warehouse.Application.Abstractions;
using Warehouse.Infrastructure.Services;

namespace Warehouse.Infrastructure;

public static class CustomerPasswordDependencyInjection
{
    public static IServiceCollection AddCustomerPasswords(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<CustomerPasswordOptions>()
            .Bind(configuration.GetSection(
                CustomerPasswordOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(
                    options.DataProtectionKeysPath),
                "CustomerPasswords:DataProtectionKeysPath is required.")
            .Validate(
                options =>
                    !options.EmailEnabled ||
                    IsValidEmailConfiguration(options),
                "Configure an HTTPS password page, sender address and SMTP settings.")
            .ValidateOnStart();

        var keysPath = configuration[
            "CustomerPasswords:DataProtectionKeysPath"];

        if (string.IsNullOrWhiteSpace(keysPath))
        {
            keysPath = "App_Data/DataProtectionKeys";
        }

        var absoluteKeysPath = Path.IsPathRooted(keysPath)
            ? keysPath
            : Path.Combine(
                AppContext.BaseDirectory,
                keysPath);

        services
            .AddDataProtection()
            .SetApplicationName("Warehouse.CustomerPasswords")
            .PersistKeysToFileSystem(
                new DirectoryInfo(absoluteKeysPath));

        services.Configure<DataProtectionTokenProviderOptions>(
            options =>
            {
                options.TokenLifespan =
                    TimeSpan.FromHours(24);
            });

        services.AddScoped<
            ICustomerPasswordService,
            CustomerPasswordService>();

        services.AddHostedService<
            CustomerPasswordEmailWorker>();

        return services;
    }

    private static bool IsValidEmailConfiguration(
        CustomerPasswordOptions options)
    {
        return
            Uri.TryCreate(
                options.PasswordPageUrl,
                UriKind.Absolute,
                out var passwordUri) &&
            passwordUri.Scheme == Uri.UriSchemeHttps &&
            string.IsNullOrEmpty(passwordUri.Fragment) &&
            MailAddress.TryCreate(
                options.FromAddress,
                out _) &&
            !string.IsNullOrWhiteSpace(options.Smtp.Host) &&
            options.Smtp.Port is >= 1 and <= 65535;
    }
}
