using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Payment.Api.Contracts;
using Payment.Api.Data;
using Payment.Api.Domain;

namespace Payment.Api.Controllers;

/// <summary>
/// Failure mode switch (tag Admin in contracts/payment.v1.yaml). Stored in the database, so it applies
/// immediately to the next charge without code changes or restarts.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/failure-mode")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public sealed class AdminController(PaymentDbContext db, ILogger<AdminController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PaymentFailureModeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    public async Task<ActionResult<PaymentFailureModeResponse>> GetPaymentFailureMode(CancellationToken cancellationToken)
    {
        var settings = await GetSettingsAsync(cancellationToken);
        return Ok(PaymentFailureModeResponse.From(settings));
    }

    [HttpPut]
    [Consumes("application/json")]
    [ProducesResponseType<PaymentFailureModeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    public async Task<ActionResult<PaymentFailureModeResponse>> SetPaymentFailureMode(
        PaymentFailureModeRequest request,
        CancellationToken cancellationToken)
    {
        var settings = await GetSettingsAsync(cancellationToken);
        var previous = settings.FailureMode;
        settings.FailureMode = request.Mode!.Value;
        settings.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        logger.LogWarning("Payment failure mode changed from {Previous} to {Mode} by {User}",
            previous, settings.FailureMode, User.Identity?.Name);
        return Ok(PaymentFailureModeResponse.From(settings));
    }

    private async Task<PaymentSettings> GetSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await db.Settings.FirstOrDefaultAsync(s => s.Id == PaymentSettings.SingletonId, cancellationToken);
        if (settings is null)
        {
            settings = new PaymentSettings { Id = PaymentSettings.SingletonId, UpdatedAt = DateTimeOffset.UtcNow };
            db.Settings.Add(settings);
            await db.SaveChangesAsync(cancellationToken);
        }

        return settings;
    }
}
