namespace Order.Api.Domain;

/// <summary>On-demand failure switch used to demonstrate compensation after a successful charge.</summary>
public enum OrderFailureMode
{
    /// <summary>The saga confirms orders normally.</summary>
    None,

    /// <summary>The saga fails right after the payment is charged: refund, release stock, cancel.</summary>
    FailAfterPayment
}
