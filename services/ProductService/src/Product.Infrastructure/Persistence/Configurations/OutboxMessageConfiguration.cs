using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Product.Infrastructure.Persistence.Outbox;

namespace Product.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Configuration cho OutboxMessage
/// Optimize cho high-throughput event processing
/// </summary>
public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(o => o.Type)
            .HasColumnName("type")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(o => o.Content)
            .HasColumnName("content")
            .HasColumnType("jsonb") // PostgreSQL JSONB cho performance
            .IsRequired();

        builder.Property(o => o.OccurredOn)
            .HasColumnName("occurred_on")
            .IsRequired();

        builder.Property(o => o.ProcessedOn)
            .HasColumnName("processed_on");

        builder.Property(o => o.Error)
            .HasColumnName("error")
            .HasMaxLength(2000);

        builder.Property(o => o.RetryCount)
            .HasColumnName("retry_count")
            .HasDefaultValue(0);

        builder.Property(o => o.MaxRetryCount)
            .HasColumnName("max_retry_count")
            .HasDefaultValue(3);

        // Indexes để optimize event processing queries
        builder.HasIndex(o => o.ProcessedOn)
            .HasDatabaseName("ix_outbox_messages_processed_on");

        builder.HasIndex(o => o.OccurredOn)
            .HasDatabaseName("ix_outbox_messages_occurred_on");

        builder.HasIndex(o => new { o.ProcessedOn, o.RetryCount })
            .HasDatabaseName("ix_outbox_messages_processing");
    }
}