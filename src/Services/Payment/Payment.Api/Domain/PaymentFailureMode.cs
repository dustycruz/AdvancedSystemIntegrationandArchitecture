namespace Payment.Api.Domain;

/// <summary>On-demand failure switch used to demonstrate saga compensation.</summary>
public enum PaymentFailureMode
{
    /// <summary>Charges succeed normally.</summary>
    None,

    /// <summary>Every charge is declined, which makes the saga release stock and cancel the order.</summary>
    DeclineCharge
}
