using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Warehouse.Application.Abstractions;
using Warehouse.Infrastructure.Identity;
using Warehouse.Infrastructure.Persistence;
using Warehouse.Infrastructure.Persistence.Repositories;
using Warehouse.Infrastructure.Seeding;
using Warehouse.Application.Services;
using Warehouse.Infrastructure.Services;


namespace Warehouse.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. Read the connection string.
        var connectionString =
            configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is missing.");
        }

        // 2. Register the SQL Server DbContext.
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        // 3. Register Identity and configure its rules.
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                // Email.
                options.User.RequireUniqueEmail = true;

                // Password.
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;

                // Account lockout.
                options.Lockout.DefaultLockoutTimeSpan =
                    TimeSpan.FromMinutes(15);

                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // 4. Register Infrastructure services.
        services.AddScoped<IIdentityService, IdentityService>();

        services.AddScoped<IInventoryData, InventoryData>();
        services.AddScoped<IRiderService, RiderService>();

        services.AddScoped<IdentitySeeder>();

        return services;
    }
}