using Microsoft.EntityFrameworkCore;

namespace Product.Infrastructure.Persistence.Configurations;

/// <summary>
/// Advanced Database Indexes để optimize các query patterns phổ biến
/// Phân tích từ ProductRepository và business requirements
/// </summary>
public static class PerformanceIndexes
{
    /// <summary>
    /// Configure additional performance indexes cho ProductDbContext
    /// Call method này trong OnModelCreating để add thêm indexes
    /// </summary>
    public static void ConfigurePerformanceIndexes(this ModelBuilder modelBuilder)
    {
        ConfigureProductPerformanceIndexes(modelBuilder);
        ConfigureCategoryPerformanceIndexes(modelBuilder);
    }

    private static void ConfigureProductPerformanceIndexes(ModelBuilder modelBuilder)
    {
        var productEntity = modelBuilder.Entity<Domain.Entities.Product>();

        // Index cho price range filtering (minPrice, maxPrice queries)
        productEntity.HasIndex("Price_Amount")
            .HasDatabaseName("IX_products_price_amount")
            .HasAnnotation("Comment", "Price range filtering performance");

        // Index cho stock quantity filtering (low stock alerts)
        productEntity.HasIndex(p => p.StockQuantity)
            .HasDatabaseName("IX_products_stock_quantity")
            .HasAnnotation("Comment", "Stock level queries");

        // Composite index cho price + active status (common filter combination)
        productEntity.HasIndex("Price_Amount", nameof(Domain.Entities.Product.IsActive))
            .HasDatabaseName("IX_products_price_active")
            .HasAnnotation("Comment", "Price filtering with active status");

        // Index cho created date sorting (product listings)
        productEntity.HasIndex(p => p.CreatedAt)
            .HasDatabaseName("IX_products_created_at")
            .HasAnnotation("Comment", "Product listing date sorting");

        // Text search composite index cho ProductName và Description
        // Note: SQL Server full-text search index sẽ cần configure riêng
        productEntity.HasIndex("ProductName")
            .HasDatabaseName("IX_products_name_search")
            .HasAnnotation("Comment", "Product name text search");

        // Composite index cho active products sorted by name (default listing)
        productEntity.HasIndex(nameof(Domain.Entities.Product.IsActive), "ProductName")
            .HasDatabaseName("IX_products_active_name_sorted")
            .HasAnnotation("Comment", "Default product listing optimization");

        // Index cho category + price range (category browsing với price filter)
        productEntity.HasIndex(
                nameof(Domain.Entities.Product.CategoryId), 
                "Price_Amount", 
                nameof(Domain.Entities.Product.IsActive))
            .HasDatabaseName("IX_products_category_price_active")
            .HasAnnotation("Comment", "Category browsing with price filtering");

        // Index cho stock alerts (low stock products query)
        productEntity.HasIndex(
                nameof(Domain.Entities.Product.IsActive), 
                nameof(Domain.Entities.Product.StockQuantity))
            .HasDatabaseName("IX_products_active_stock")
            .HasAnnotation("Comment", "Active products stock monitoring");
    }

    private static void ConfigureCategoryPerformanceIndexes(ModelBuilder modelBuilder)
    {
        var categoryEntity = modelBuilder.Entity<Domain.Entities.Category>();

        // Composite index cho hierarchy traversal
        categoryEntity.HasIndex(
                nameof(Domain.Entities.Category.ParentId), 
                nameof(Domain.Entities.Category.IsActive), 
                nameof(Domain.Entities.Category.DisplayOrder))
            .HasDatabaseName("IX_categories_parent_active_order")
            .HasAnnotation("Comment", "Category hierarchy with ordering");

        // Index cho level-based queries (breadcrumb generation)
        categoryEntity.HasIndex(
                nameof(Domain.Entities.Category.Level), 
                nameof(Domain.Entities.Category.IsActive))
            .HasDatabaseName("IX_categories_level_active")
            .HasAnnotation("Comment", "Level-based category queries");

        // Index cho category navigation (path-based lookups)
        categoryEntity.HasIndex(
                nameof(Domain.Entities.Category.Path), 
                nameof(Domain.Entities.Category.IsActive))
            .HasDatabaseName("IX_categories_path_active")
            .HasAnnotation("Comment", "Path-based category navigation");
    }
}

/// <summary>
/// Extension methods để setup advanced database optimizations
/// </summary>
public static class DatabaseOptimizations
{
    /// <summary>
    /// Configure advanced database settings cho performance
    /// </summary>
    public static void ConfigureDatabaseOptimizations(this ModelBuilder modelBuilder)
    {
        // Set default schema nếu cần
        modelBuilder.HasDefaultSchema("product");

        // Configure sequences cho high-performance ID generation
        modelBuilder.HasSequence<long>("ProductSequence")
            .StartsAt(1)
            .IncrementsBy(1)
            .HasMin(1)
            .HasMax(long.MaxValue);

        modelBuilder.HasSequence<long>("CategorySequence")
            .StartsAt(1)
            .IncrementsBy(1)
            .HasMin(1)
            .HasMax(long.MaxValue);

        // Configure table-level settings
        ConfigureTableOptimizations(modelBuilder);
    }

    private static void ConfigureTableOptimizations(ModelBuilder modelBuilder)
    {
        // Products table optimizations
        modelBuilder.Entity<Domain.Entities.Product>()
            .ToTable("products", options => 
            {
                options.HasComment("Main products table with performance optimizations");
            });

        // Categories table optimizations
        modelBuilder.Entity<Domain.Entities.Category>()
            .ToTable("categories", options =>
            {
                options.HasComment("Product categories with hierarchy support");
            });
    }
}