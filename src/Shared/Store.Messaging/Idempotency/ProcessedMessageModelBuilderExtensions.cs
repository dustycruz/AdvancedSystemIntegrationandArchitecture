using Microsoft.EntityFrameworkCore;

namespace Store.Messaging.Idempotency;

public static class ProcessedMessageModelBuilderExtensions
{
    /// <summary>Maps the processed_messages table. Call from OnModelCreating of every consuming service.</summary>
    public static ModelBuilder AddProcessedMessages(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProcessedMessage>(entity =>
        {
            entity.ToTable("processed_messages");
            entity.HasKey(m => new { m.MessageId, m.Consumer });
            entity.Property(m => m.MessageId).HasColumnName("message_id");
            entity.Property(m => m.Consumer).HasColumnName("consumer").HasMaxLength(200);
            entity.Property(m => m.MessageType).HasColumnName("message_type").HasMaxLength(200).IsRequired();
            entity.Property(m => m.ProcessedAt).HasColumnName("processed_at");
        });

        return modelBuilder;
    }
}
