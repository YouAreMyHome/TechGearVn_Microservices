using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Product.Domain.Entities;

namespace Product.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration cho Category Entity
/// Infrastructure concern: Map Category aggregate tới database schema
/// </summary>
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        // ================================
        // 🏗️ TABLE CONFIGURATION
        // ================================

        builder.ToTable("categories");

        // Primary Key
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasColumnName("id")
            .IsRequired();

        // ================================
        // 📊 PRIMITIVE PROPERTIES
        // ================================

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(c => c.ParentId)
            .HasColumnName("parent_category_id");

        builder.Property(c => c.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(c => c.DisplayOrder)
            .HasColumnName("display_order")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.Slug)
            .HasColumnName("slug")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Level)
            .HasColumnName("level")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.Path)
            .HasColumnName("category_path")
            .HasMaxLength(500)
            .IsRequired();

        // ================================
        // 🕒 AUDIT PROPERTIES
        // ================================

        builder.Property(c => c.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(c => c.UpdatedBy)
            .HasColumnName("updated_by")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // ================================
        // 🔗 RELATIONSHIPS
        // ================================

        // Self-referencing relationship cho hierarchy
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(c => c.ParentId)
            .HasConstraintName("FK_categories_parent_category")
            .OnDelete(DeleteBehavior.Restrict); // Không cho phép delete parent nếu có children

        // ================================
        // 🗂️ INDEXES
        // ================================

        // Unique constraint cho name
        builder.HasIndex(c => c.Name)
            .IsUnique()
            .HasDatabaseName("IX_categories_name");

        // Index cho active categories
        builder.HasIndex(c => c.IsActive)
            .HasDatabaseName("IX_categories_is_active");

        // Index cho parent-child relationship
        builder.HasIndex(c => c.ParentId)
            .HasDatabaseName("IX_categories_parent_id");

        // Composite index cho active categories với display order
        builder.HasIndex(c => new { c.IsActive, c.DisplayOrder })
            .HasDatabaseName("IX_categories_active_display_order");

        // Index cho category path (cho search)
        builder.HasIndex(c => c.Path)
            .HasDatabaseName("IX_categories_path");

        // Index cho slug (unique)
        builder.HasIndex(c => c.Slug)
            .IsUnique()
            .HasDatabaseName("IX_categories_slug_unique");

        // ================================
        // 🚫 IGNORE DOMAIN EVENTS
        // ================================

        // Domain Events không persist vào database
        builder.Ignore(c => c.DomainEvents);
    }
}