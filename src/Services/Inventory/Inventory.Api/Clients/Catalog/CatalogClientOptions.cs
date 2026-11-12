using System.ComponentModel.DataAnnotations;

namespace Inventory.Api.Clients.Catalog;

/// <summary>Bound from Services:Catalog (Services__Catalog__BaseUrl in docker-compose).</summary>
public sealed class CatalogClientOptions
{
    public const string SectionName = "Services:Catalog";

    [Required, Url] public string BaseUrl { get; set; } = "http://localhost:5002";
}
