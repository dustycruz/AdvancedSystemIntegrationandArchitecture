using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Payment.Api.Contracts;
using Payment.Api.Data;
using Store.Observability.ProblemDetails;

namespace Payment.Api.Controllers;

/// <summary>Charges and refunds (tag Payments in contracts/payment.v1.yaml). Admin only.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/payments")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public sealed class PaymentsController(PaymentDbContext db) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PaymentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    public async Task<ActionResult<IReadOnlyList<PaymentResponse>>> ListPayments(CancellationToken cancellationToken)
    {
        var payments = await db.Payments.AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .Take(500)
            .ToListAsync(cancellationToken);
        return Ok(payments.Select(PaymentResponse.From).ToList());
    }

    [HttpGet("by-order/{orderId:guid}")]
    [ProducesResponseType<PaymentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<PaymentResponse>> GetPaymentByOrderId(Guid orderId, CancellationToken cancellationToken)
    {
        var payment = await db.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.OrderId == orderId, cancellationToken)
            ?? throw new NotFoundException($"No payment has been recorded for order {orderId}.");
        return Ok(PaymentResponse.From(payment));
    }
}
