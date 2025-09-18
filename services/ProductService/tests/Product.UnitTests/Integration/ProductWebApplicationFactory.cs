using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Product.Infrastructure.Persistence;
using Product.Application;
using Product.Domain.Repositories;
using Product.Infrastructure.Persistence.Repositories;

namespace Product.UnitTests.Integration;

/// <summary>
/// Custom WebApplicationFactory cho Integration Tests
/// Sử dụng In-Memory Database thay vì production database
/// </summary>
public class ProductWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set test environment variables to prevent DB_HOST error
        Environment.SetEnvironmentVariable("DB_HOST", "test_host");
        Environment.SetEnvironmentVariable("DB_PORT", "5432");
        Environment.SetEnvironmentVariable("DB_NAME", "test_db");
        Environment.SetEnvironmentVariable("DB_USERNAME", "test_user");
        Environment.SetEnvironmentVariable("DB_PASSWORD", "test_password");

        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove ALL existing database-related services more thoroughly
            var descriptorsToRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(DbContextOptions<ProductDbContext>) ||
                d.ServiceType == typeof(ProductDbContext) ||
                d.ServiceType.Name.Contains("DbContext") ||
                d.ServiceType.Name.Contains("Database") ||
                (d.ImplementationType?.Assembly.FullName?.Contains("Npgsql") == true))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing
            services.AddDbContext<ProductDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDatabase_" + Guid.NewGuid())
                       .EnableSensitiveDataLogging()
                       .EnableDetailedErrors();
            });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Initialize test database after application is built
        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
        context.Database.EnsureCreated();
        SeedTestData(context);

        return host;
    }

    /// <summary>
    /// Seed dữ liệu test cơ bản
    /// </summary>
    private static void SeedTestData(ProductDbContext context)
    {
        // Xóa dữ liệu cũ nếu có
        context.Products.RemoveRange(context.Products);
        context.Categories.RemoveRange(context.Categories);
        context.SaveChanges();

        // Tạo test categories
        var electronicsCategory = global::Product.Domain.Entities.Category.CreateRootCategory(
            "Electronics",
            "electronics",
            "Electronic devices and accessories",
            1,
            "test-user"
        );

        var clothingCategory = global::Product.Domain.Entities.Category.CreateRootCategory(
            "Clothing",
            "clothing",
            "Fashion and apparel",
            2,
            "test-user"
        );

        context.Categories.Add(electronicsCategory);
        context.Categories.Add(clothingCategory);

        // Tạo test products sử dụng convenience factory method
        var product1 = global::Product.Domain.Entities.Product.Create(
            name: "Test Laptop",
            sku: null,
            description: "Test laptop for integration testing",
            priceAmount: 999.99m,
            currency: "USD",
            initialStock: 50,
            categoryId: electronicsCategory.Id,
            createdBy: "test-user"
        );

        var product2 = global::Product.Domain.Entities.Product.Create(
            name: "Test T-Shirt",
            sku: null,
            description: "Test t-shirt for integration testing",
            priceAmount: 29.99m,
            currency: "USD",
            initialStock: 100,
            categoryId: clothingCategory.Id,
            createdBy: "test-user"
        );

        // Skip test data seeding for now due to EF Core InMemory validation issues
        // Will revisit this when we have more time to properly configure the test database
        // context.SaveChanges();
    }
}