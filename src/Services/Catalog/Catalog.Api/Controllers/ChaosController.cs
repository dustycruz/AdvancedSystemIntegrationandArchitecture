using Asp.Versioning;
using Catalog.Api.Chaos;
using Catalog.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.Observability.ProblemDetails;

namespace Catalog.Api.Controllers;

/// <summary>
/// Development-only fault injection (tag Chaos in contracts/catalog.v1.yaml). Outside the Development
/// environment every action answers 404, so the switch cannot be used in production.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/chaos")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public sealed class ChaosController(ChaosState state, IWebHostEnvironment environment, ILogger<ChaosController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ChaosSettings>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public ActionResult<ChaosSettings> GetChaosSettings()
    {
        EnsureDevelopment();
        return Ok(state.Current);
    }

    [HttpPut]
    [Consumes("application/json")]
    [ProducesResponseType<ChaosSettings>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public ActionResult<ChaosSettings> UpdateChaosSettings(ChaosSettings settings)
    {
        EnsureDevelopment();
        state.Update(settings);
        logger.LogWarning("Chaos settings changed by {User}: failure rate {FailureRate:P0}, delay {DelayMs} ms",
            User.Identity?.Name, settings.FailureRate, settings.DelayMs);
        return Ok(state.Current);
    }

    private void EnsureDevelopment()
    {
        if (!environment.IsDevelopment())
        {
            throw new NotFoundException("Chaos settings are only available in the Development environment.");
        }
    }
}
