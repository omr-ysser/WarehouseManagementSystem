using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Warehouse.Application.Abstractions;
using Warehouse.Application.Services;
using Warehouse.Application.Validation;

namespace Warehouse.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        // Authentication application service.
        services.AddScoped<IAuthService, AuthService>();

        // Existing inventory service.
        services.AddScoped<IInventoryService, InventoryService>();

        // Register MediatR and all handlers in the Application assembly:
        // - Existing product handlers.
        // - Rider command/query handlers.
        // - Tournament command/query handlers.
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(
                typeof(DependencyInjection).Assembly);
        });

        // Discover and register FluentValidation validators.
        // Validators must still be invoked by a service or pipeline behavior.
        services.AddValidatorsFromAssemblyContaining<
            LoginRequestValidator>();

        return services;
    }
}