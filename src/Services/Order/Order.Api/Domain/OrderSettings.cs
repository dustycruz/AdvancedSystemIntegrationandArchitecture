namespace Order.Api.Domain;

/// <summary>Single-row settings table holding the order failure mode, so it survives restarts.</summary>
public sealed class OrderSettings
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;
    public OrderFailureMode FailureMode { get; set; } = OrderFailureMode.None;
    public DateTimeOffset UpdatedAt { get; set; }
}
