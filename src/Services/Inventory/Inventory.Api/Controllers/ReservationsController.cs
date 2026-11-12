using Asp.Versioning;
using Inventory.Api.Contracts;
using Inventory.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Controllers;

/// <summary>Reservations held for orders (tag Reservations in contracts/inventory.v1.yaml).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reservations")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public sealed class ReservationsController(InventoryDbContext db) : ControllerBase
{
    [HttpGet("{orderId:guid}")]
    [ProducesResponseType<IReadOnlyList<ReservationResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    public async Task<ActionResult<IReadOnlyList<ReservationResponse>>> GetReservationsByOrderId(Guid orderId, CancellationToken cancellationToken)
    {
        var reservations = await db.Reservations.AsNoTracking()
            .Where(r => r.OrderId == orderId)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
        return Ok(reservations.Select(ReservationResponse.From).ToList());
    }
}
