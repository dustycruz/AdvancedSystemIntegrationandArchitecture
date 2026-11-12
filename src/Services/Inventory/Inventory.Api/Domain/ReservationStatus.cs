namespace Inventory.Api.Domain;

public enum ReservationStatus
{
    /// <summary>Units are held for the order.</summary>
    Reserved,

    /// <summary>The hold was given back because the order was cancelled.</summary>
    Released,

    /// <summary>The order was confirmed; the units left the warehouse.</summary>
    Committed
}
