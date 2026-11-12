namespace Order.Api.Domain;

/// <summary>
/// What the order saga is waiting for next. The values are ordered: the orchestrator compares them to tell a
/// reply that arrived early (retry) from one that arrived late (ignore).
/// </summary>
public enum SagaStep
{
    AwaitingStockReservation,
    AwaitingPayment,
    AwaitingRefund,
    AwaitingStockRelease,
    Completed
}
