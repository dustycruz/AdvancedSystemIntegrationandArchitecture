namespace Identity.Api.Domain;

public enum UserRole
{
    /// <summary>Can browse, place orders and read their own orders.</summary>
    Customer,

    /// <summary>Can also manage products, stock, failure modes and chaos settings.</summary>
    Admin
}
