namespace Identity.Api.Data;

/// <summary>
/// Demo accounts created at first start. Bound from the "Seed" section, which docker-compose fills from
/// SEED_ADMIN_EMAIL, SEED_ADMIN_PASSWORD, SEED_CUSTOMER_EMAIL and SEED_CUSTOMER_PASSWORD in .env.
/// </summary>
public sealed class SeedAccountOptions
{
    public const string SectionName = "Seed";

    public string? AdminEmail { get; set; }
    public string? AdminPassword { get; set; }
    public string AdminFullName { get; set; } = "Store Administrator";

    public string? CustomerEmail { get; set; }
    public string? CustomerPassword { get; set; }
    public string CustomerFullName { get; set; } = "Demo Customer";
}
