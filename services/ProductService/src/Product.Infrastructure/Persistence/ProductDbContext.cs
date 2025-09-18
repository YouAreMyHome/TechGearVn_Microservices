using Microsoft.EntityFrameworkCore;
using Product.Infrastructure.Persistence.Outbox;
using Product.Infrastructure.Persistence.Configurations;

namespace Product.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext cho Product Service
/// Tuân thủ Clean Architecture: Infrastructure layer quản lý technical concerns
/// Không chứa business logic, chỉ handle database operations và persistence
/// Outbox Pattern: Domain Events được convert thành Outbox Messages để publish reliable
/// </summary>
public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
    {
    }

    #region DbSets - Aggregate Roots

    /// <summary>
    /// Products table - chính là Aggregate Root của Product bounded context
    /// Mỗi Product entity sẽ quản lý toàn bộ lifecycle và business rules của sản phẩm
    /// </summary>
    public DbSet<Domain.Entities.Product> Products { get; set; } = default!;

    /// <summary>
    /// Categories table - Aggregate Root cho category management
    /// Hierarchical structure với parent-child relationships
    /// </summary>
    public DbSet<Domain.Entities.Category> Categories { get; set; } = default!;

    #endregion

    #region Outbox Pattern - Domain Events

    /// <summary>
    /// Outbox Messages table cho Domain Events publishing
    /// Outbox Pattern đảm bảo Domain Events được publish reliable trong microservices
    /// Events được save cùng transaction với business data, sau đó publish async
    /// </summary>
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = default!;

    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // IMPORTANT: Ignore Value Objects as standalone entities
        modelBuilder.Ignore<Domain.ValueObjects.Money>();
        modelBuilder.Ignore<Domain.ValueObjects.ProductName>();
        modelBuilder.Ignore<Domain.ValueObjects.ProductSku>();

        // Apply configuration manually để debug
        modelBuilder.ApplyConfiguration(new ProductConfiguration());

        // Global database settings
        ConfigureGlobalSettings(modelBuilder);
    }

    /// <summary>
    /// Global settings cho toàn bộ database schema
    /// Đảm bảo consistent naming convention và data types
    /// </summary>
    private static void ConfigureGlobalSettings(ModelBuilder modelBuilder)
    {
        // 1. Default string length để tránh nvarchar(max) performance issues
        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(string)))
        {
            // Nếu chưa set MaxLength thì set default 500
            if (property.GetMaxLength() == null)
            {
                property.SetMaxLength(500);
            }
        }

        // 2. Naming convention: snake_case cho PostgreSQL
        // Chuyển từ PascalCase (C#) sang snake_case (Database)
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            // Table names: Product → products
            entity.SetTableName(entity.GetTableName()?.ToSnakeCase());

            // Column names: ProductName → product_name
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(property.GetColumnName()?.ToSnakeCase());
            }

            // Index names: IX_Products_CategoryId → ix_products_category_id
            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(index.GetDatabaseName()?.ToSnakeCase());
            }
        }

        // 3. Decimal precision cho Money values
        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            // Default decimal(18,2) cho money values
            if (property.GetColumnType() == null)
            {
                property.SetColumnType("decimal(18,2)");
            }
        }
    }

    /// <summary>
    /// Override SaveChangesAsync để handle Domain Events
    /// Outbox Pattern: Process Domain Events trước khi commit transaction
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Process Domain Events thành Outbox Messages trước khi save
        ProcessDomainEvents();

        // Save tất cả changes trong single transaction
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Convert Domain Events thành Outbox Messages
    /// Đảm bảo Domain Events được publish reliable qua Outbox Pattern
    /// </summary>
    private void ProcessDomainEvents()
    {
        // Lấy tất cả entities có Domain Events
        var entitiesWithEvents = ChangeTracker
            .Entries<Domain.Common.AggregateRoot<Guid>>()
            .Where(e => e.Entity.DomainEvents.Any())
            .ToList();

        // Convert Domain Events → Outbox Messages
        foreach (var entity in entitiesWithEvents)
        {
            foreach (var domainEvent in entity.Entity.DomainEvents)
            {
                var outboxMessage = new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    Type = domainEvent.GetType().FullName!,
                    Content = System.Text.Json.JsonSerializer.Serialize(domainEvent, new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                        WriteIndented = false
                    }),
                    OccurredOn = DateTime.UtcNow
                };

                OutboxMessages.Add(outboxMessage);
            }

            // Clear Domain Events sau khi đã convert thành Outbox Messages
            entity.Entity.ClearDomainEvents();
        }
    }
}

/// <summary>
/// Extension method để convert PascalCase → snake_case
/// Tuân thủ PostgreSQL naming convention
/// </summary>
internal static class StringExtensions
{
    public static string ToSnakeCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Convert PascalCase → snake_case
        // ProductName → product_name
        return string.Concat(
            input.Select((x, i) => i > 0 && char.IsUpper(x)
                ? "_" + x.ToString().ToLower()
                : x.ToString().ToLower()));
    }
}