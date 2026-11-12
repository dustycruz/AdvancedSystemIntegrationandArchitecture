namespace Order.Api.Domain;

/// <summary>What the order saga is waiting for next.</summary>
public enum SagaStep
{
    AwaitingStockReservation,
    AwaitingPayment,
    AwaitingRefund,
    AwaitingStockRelease,
    Completed
}
