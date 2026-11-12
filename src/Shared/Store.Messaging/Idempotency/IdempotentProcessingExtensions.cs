using Microsoft.EntityFrameworkCore;
using Store.Messaging.Consuming;

namespace Store.Messaging.Idempotency;

public static class IdempotentProcessingExtensions
{
    /// <summary>
    /// Runs <paramref name="work"/> exactly once per message id and consumer queue:
    /// opens a transaction, skips the work when a processed_messages row already exists, otherwise runs it,
    /// records the message as processed and commits — business change and idempotency record together.
    /// Returns false when the message had already been processed.
    /// </summary>
    /// <remarks>
    /// Events published inside <paramref name="work"/> are published before the commit. Derive their ids
    /// with <see cref="MessageIds.Derive"/> so a retry after a failed commit republishes the same ids and
    /// downstream consumers discard the duplicates.
    /// </remarks>
    public static async Task<bool> ProcessOnceAsync(
        this DbContext db,
        MessageContext context,
        string messageType,
        Func<CancellationToken, Task> work,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var alreadyProcessed = await db.Set<ProcessedMessage>()
            .AnyAsync(m => m.MessageId == context.MessageId && m.Consumer == context.QueueName, cancellationToken);
        if (alreadyProcessed)
        {
            return false;
        }

        await work(cancellationToken);

        db.Set<ProcessedMessage>().Add(new ProcessedMessage
        {
            MessageId = context.MessageId,
            Consumer = context.QueueName,
            MessageType = messageType,
            ProcessedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
