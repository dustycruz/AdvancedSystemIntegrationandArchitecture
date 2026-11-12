using System.ComponentModel.DataAnnotations;

namespace Storefront.Web.Infrastructure;

/// <summary>Where the gateway is (Gateway__BaseUrl). The Storefront never calls a service directly.</summary>
public sealed class GatewayOptions
{
    public const string SectionName = "Gateway";

    [Required, Url] public string BaseUrl { get; set; } = "http://localhost:5000";
}
