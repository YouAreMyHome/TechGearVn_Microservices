using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Product.Application.Behaviors;

namespace Product.Application.DependencyInjection;

/// <summary>
/// Extension methods để register Application Layer services
/// Tuân thủ Clean Architecture: Application layer tự quản lý dependencies của mình
/// API layer sẽ gọi AddApplication() để setup toàn bộ Application dependencies
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Register tất cả Application Layer dependencies
    /// Gọi method này từ API layer Program.cs
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Get Assembly của Application layer để scan các types
        var applicationAssembly = typeof(ApplicationServiceCollectionExtensions).Assembly;

        // Register MediatR với Commands, Queries, Handlers
        services.AddMediatRServices(applicationAssembly);

        // Register AutoMapper với Profiles
        services.AddAutoMapperServices(applicationAssembly);

        // Register FluentValidation với Validators
        services.AddFluentValidationServices(applicationAssembly);

        // Register Pipeline Behaviors cho MediatR
        services.AddMediatRPipelineBehaviors();

        return services;
    }

    /// <summary>
    /// Register MediatR services
    /// Scan assembly để tìm tất cả IRequestHandler implementations
    /// </summary>
    private static IServiceCollection AddMediatRServices(
        this IServiceCollection services,
        Assembly assembly)
    {
        // Register MediatR với assembly chứa Handlers
        services.AddMediatR(config =>
        {
            // Scan assembly để register Commands, Queries, Handlers
            config.RegisterServicesFromAssembly(assembly);
        });

        return services;
    }

    /// <summary>
    /// Register AutoMapper services
    /// Scan assembly để tìm tất cả Profile implementations
    /// </summary>
    private static IServiceCollection AddAutoMapperServices(
        this IServiceCollection services,
        Assembly assembly)
    {
        // Register AutoMapper với assembly chứa Mapping Profiles
        services.AddAutoMapper(assembly);

        return services;
    }

    /// <summary>
    /// Register FluentValidation services
    /// Scan assembly để tìm tất cả AbstractValidator implementations
    /// </summary>
    private static IServiceCollection AddFluentValidationServices(
        this IServiceCollection services,
        Assembly assembly)
    {
        // Register tất cả validators trong assembly
        services.AddValidatorsFromAssembly(assembly);

        // Configure FluentValidation behavior
        ValidatorOptions.Global.LanguageManager.Enabled = false; // Disable localization
        ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Stop; // Stop on first failure

        return services;
    }

    /// <summary>
    /// Register MediatR Pipeline Behaviors
    /// Pipeline Behaviors = Interceptors cho MediatR requests
    /// Chạy theo thứ tự: Validation → Logging → Handler
    /// </summary>
    private static IServiceCollection AddMediatRPipelineBehaviors(this IServiceCollection services)
    {
        // Register ValidationBehavior (chạy trước Handler)
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // Register LoggingBehavior (chạy sau Validation, trước Handler)
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        return services;
    }
}