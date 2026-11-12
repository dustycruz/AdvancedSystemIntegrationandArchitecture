namespace Store.Messaging.Idempotency;

/// <summary>
/// Row recorded in the consuming service's own database, in the same transaction as the business change,
/// when a message has been handled. A redelivered message with the same id is then skipped.
/// </summary>
public sealed class ProcessedMessage
{
    public Guid MessageId { get; set; }
    public string Consumer { get; set; } = string.Empty;
    public string MessageType { get; set; } = string.Empty;
    public DateTimeOffset ProcessedAt { get; set; }
}
