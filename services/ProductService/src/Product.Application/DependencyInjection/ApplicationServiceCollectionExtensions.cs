using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Product.Application.Mappings;
using System.Reflection;

namespace Product.Application.DependencyInjection;

/// <summary>
/// Application Layer Dependency Injection Extensions
/// Đăng ký tất cả Application services vào DI container
/// Tuân thủ Clean Architecture: Application chỉ depend on Domain
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Đăng ký Assembly hiện tại cho MediatR scanning
        var applicationAssembly = Assembly.GetExecutingAssembly();

        // MediatR - CQRS Pattern implementation
        services.AddMediatR(config =>
        {
            // Scan assembly để tìm tất cả IRequestHandler implementations
            config.RegisterServicesFromAssembly(applicationAssembly);

            // TODO: Add pipeline behaviors (Validation, Logging, Performance)
            // config.AddBehavior<ValidationBehavior<,>>();
            // config.AddBehavior<LoggingBehavior<,>>();
        });

        // FluentValidation - Input validation cho Commands và Queries
        services.AddValidatorsFromAssembly(applicationAssembly);

        // AutoMapper - Domain Entities ↔ Application DTOs mapping
        services.AddAutoMapper(typeof(ApplicationMappingProfile));

        return services;
    }
}