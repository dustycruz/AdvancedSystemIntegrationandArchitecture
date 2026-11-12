namespace Payment.Api.Domain;

public enum PaymentStatus
{
    /// <summary>The customer was charged.</summary>
    Charged,

    /// <summary>The charge was refused; nothing was taken.</summary>
    Declined,

    /// <summary>A previous charge was given back to the customer.</summary>
    Refunded
}
