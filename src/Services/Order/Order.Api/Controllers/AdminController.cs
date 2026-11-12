using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Order.Api.Contracts;
using Order.Api.Data;
using Order.Api.Domain;

namespace Order.Api.Controllers;

/// <summary>
/// Failure mode switch (tag Admin in contracts/order.v1.yaml). Stored in the database, so it applies
/// to the next charged order without code changes or restarts (FailAfterPayment triggers refund + release).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/failure-mode")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public sealed class AdminController(OrderDbContext db, ILogger<AdminController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<OrderFailureModeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    public async Task<ActionResult<OrderFailureModeResponse>> GetOrderFailureMode(CancellationToken cancellationToken)
    {
        var settings = await GetSettingsAsync(cancellationToken);
        return Ok(OrderFailureModeResponse.From(settings));
    }

    [HttpPut]
    [Consumes("application/json")]
    [ProducesResponseType<OrderFailureModeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    public async Task<ActionResult<OrderFailureModeResponse>> SetOrderFailureMode(
        OrderFailureModeRequest request,
        CancellationToken cancellationToken)
    {
        var settings = await GetSettingsAsync(cancellationToken);
        var previous = settings.FailureMode;
        settings.FailureMode = request.Mode!.Value;
        settings.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        logger.LogWarning("Order failure mode changed from {Previous} to {Mode} by {User}",
            previous, settings.FailureMode, User.Identity?.Name);
        return Ok(OrderFailureModeResponse.From(settings));
    }

    private async Task<OrderSettings> GetSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await db.Settings.FirstOrDefaultAsync(s => s.Id == OrderSettings.SingletonId, cancellationToken);
        if (settings is null)
        {
            settings = new OrderSettings { Id = OrderSettings.SingletonId, UpdatedAt = DateTimeOffset.UtcNow };
            db.Settings.Add(settings);
            await db.SaveChangesAsync(cancellationToken);
        }

        return settings;
    }
}
