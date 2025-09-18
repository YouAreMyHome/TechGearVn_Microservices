using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Product.Domain.Entities;
using Product.Domain.ValueObjects;

namespace Product.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration cho Product Entity
/// Infrastructure concern: Map Domain entities tới database schema
/// DDD compliance: Value Objects được map như Owned Entities
/// </summary>
public class ProductConfiguration : IEntityTypeConfiguration<Domain.Entities.Product>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Product> builder)
    {
        // ================================
        // 🏗️ TABLE CONFIGURATION
        // ================================

        builder.ToTable("products");

        // Primary Key
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasColumnName("id")
            .IsRequired();

        // ================================
        // 💰 MONEY VALUE OBJECT CONFIGURATION
        // ================================

        // TEMPORARY: Map Money as primitive properties
        builder.Property<decimal>("PriceAmount")
            .HasColumnName("price_amount")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property<string>("PriceCurrency")
            .HasColumnName("price_currency")
            .HasMaxLength(3)
            .IsRequired()
            .HasDefaultValue("VND");

        // ================================
        // 🏷️ VALUE OBJECTS CONFIGURATION  
        // ================================

        // TEMPORARY: Map as primitive properties  
        builder.Property<string>("ProductName")
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property<string>("ProductSku")
            .HasColumnName("sku")
            .HasMaxLength(50)
            .IsRequired();

        // Business constraint: SKU phải unique
        builder.HasIndex("ProductSku")
            .IsUnique()
            .HasDatabaseName("IX_products_sku");

        // ================================
        // 📊 PRIMITIVE PROPERTIES
        // ================================

        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(p => p.StockQuantity)
            .HasColumnName("stock_quantity")
            .IsRequired();

        builder.Property(p => p.CategoryId)
            .HasColumnName("category_id")
            .IsRequired();

        builder.Property(p => p.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        // ================================
        // 🕒 AUDIT PROPERTIES
        // ================================

        builder.Property(p => p.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(p => p.UpdatedBy)
            .HasColumnName("updated_by")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // ================================
        // � RELATIONSHIPS
        // ================================

        // Foreign key relationship với Category
        builder.HasOne<Domain.Entities.Category>()
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .HasConstraintName("FK_products_category")
            .OnDelete(DeleteBehavior.Restrict); // Không cho phép delete category nếu có products

        // ================================
        // �📈 INDEXES FOR PERFORMANCE
        // ================================

        // Index cho category browsing
        builder.HasIndex(p => p.CategoryId)
            .HasDatabaseName("IX_products_category_id");

        // Index cho active products
        builder.HasIndex(p => p.IsActive)
            .HasDatabaseName("IX_products_is_active");

        // Composite index cho active products trong category
        builder.HasIndex(p => new { p.CategoryId, p.IsActive })
            .HasDatabaseName("IX_products_category_active");

        // ================================
        // 🚫 IGNORE DOMAIN EVENTS
        // ================================

        // Domain Events không persist vào database
        builder.Ignore(p => p.DomainEvents);
    }
}