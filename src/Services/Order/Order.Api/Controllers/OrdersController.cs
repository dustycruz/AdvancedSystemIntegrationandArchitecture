using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Order.Api.Contracts;
using Order.Api.Data;
using Order.Api.Services;
using Store.Observability.ProblemDetails;

namespace Order.Api.Controllers;

/// <summary>Orders (tag Orders in contracts/order.v1.yaml). Every action requires a JWT.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/orders")]
[Authorize]
[Produces("application/json")]
public sealed class OrdersController(OrderDbContext db, OrderPlacementService placement) : ControllerBase
{
    private const string AdminRole = "Admin";
    private const string CustomerRole = "Customer";

    /// <summary>Places an order. Answers 202 because stock and payment are decided asynchronously by the saga.</summary>
    [HttpPost]
    [Authorize(Roles = CustomerRole)]
    [Consumes("application/json")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<ActionResult<OrderResponse>> PlaceOrder(PlaceOrderRequest request, CancellationToken cancellationToken)
    {
        var (customerId, email) = GetCaller();
        var order = await placement.PlaceOrderAsync(customerId, email, request, cancellationToken);
        return AcceptedAtAction(nameof(GetOrderById),
            new { orderId = order.Id, version = RouteData.Values["version"] },
            OrderResponse.From(order));
    }

    [HttpGet]
    [Authorize(Roles = AdminRole)]
    [ProducesResponseType<IReadOnlyList<OrderSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    public async Task<ActionResult<IReadOnlyList<OrderSummaryResponse>>> ListOrders(CancellationToken cancellationToken) =>
        Ok(await QuerySummaries(db.Orders.AsNoTracking()).Take(500).ToListAsync(cancellationToken));

    [HttpGet("mine")]
    [ProducesResponseType<IReadOnlyList<OrderSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<ActionResult<IReadOnlyList<OrderSummaryResponse>>> ListMyOrders(CancellationToken cancellationToken)
    {
        var (customerId, _) = GetCaller();
        return Ok(await QuerySummaries(db.Orders.AsNoTracking().Where(o => o.CustomerId == customerId))
            .ToListAsync(cancellationToken));
    }

    /// <summary>Customers only see their own orders (another customer's order answers 404); Admins see every order.</summary>
    [HttpGet("{orderId:guid}")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<OrderResponse>> GetOrderById(Guid orderId, CancellationToken cancellationToken)
    {
        var (customerId, _) = GetCaller();
        var order = await db.Orders.AsNoTracking()
            .Include(o => o.Lines)
            .Include(o => o.StatusHistory)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null || (order.CustomerId != customerId && !User.IsInRole(AdminRole)))
        {
            throw new NotFoundException($"Order {orderId} was not found.");
        }

        return Ok(OrderResponse.From(order));
    }

    private static IQueryable<OrderSummaryResponse> QuerySummaries(IQueryable<Domain.Order> orders) =>
        orders
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OrderSummaryResponse(
                o.Id,
                o.CustomerEmail,
                o.Status,
                o.TotalAmount,
                o.Lines.Sum(l => l.Quantity),
                o.CancellationReason,
                o.CreatedAt,
                o.UpdatedAt));

    private (Guid CustomerId, string Email) GetCaller()
    {
        if (!Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var customerId))
        {
            throw new BadRequestException("The access token has no valid subject (sub) claim.");
        }

        var email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value ?? string.Empty;
        return (customerId, email);
    }
}
