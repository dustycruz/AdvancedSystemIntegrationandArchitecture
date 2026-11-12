using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Storefront.Web.Clients;
using Storefront.Web.Infrastructure;
using Storefront.Web.Models;

namespace Storefront.Web.Controllers;

/// <summary>My Orders and Order Details with the live saga timeline.</summary>
[Authorize]
public sealed class OrdersController(IOrderClient orders) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var all = User.IsAdmin();
        var list = all
            ? await orders.ListOrdersAsync(cancellationToken)
            : await orders.ListMyOrdersAsync(cancellationToken);
        return View(new OrdersViewModel(list.ToList(), all));
    }

    [HttpGet("/orders/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken) =>
        View(await orders.GetOrderByIdAsync(id, cancellationToken));

    /// <summary>Polled every 2 seconds by order-timeline.js until the order is Confirmed or Cancelled.</summary>
    [HttpGet("/orders/{id:guid}/status")]
    public async Task<IActionResult> Status(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var order = await orders.GetOrderByIdAsync(id, cancellationToken);
            return Json(new
            {
                status = order.Status.ToString(),
                isFinal = order.Status is OrderStatus.Confirmed or OrderStatus.Cancelled,
                cancellationReason = order.CancellationReason,
                history = order.StatusHistory.Select(h => new
                {
                    status = h.Status.ToString(),
                    reason = h.Reason,
                    occurredAt = h.OccurredAt
                })
            });
        }
        catch (GatewayApiException ex)
        {
            var error = ApiErrors.From(ex);
            return StatusCode(ex.StatusCode, new { error = error.Message });
        }
    }
}
