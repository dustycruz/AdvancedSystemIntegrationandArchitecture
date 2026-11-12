using Microsoft.AspNetCore.Http;

namespace Store.Observability.ProblemDetails;

/// <summary>
/// Base class for expected failures. <see cref="StoreExceptionHandler"/> turns each one into a
/// Problem Details response with the exception's status code, title and message.
/// </summary>
public abstract class StoreException(string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public abstract int StatusCode { get; }
    public abstract string Title { get; }
}

/// <summary>400 — the request is well-formed JSON but breaks a business rule.</summary>
public sealed class BadRequestException(string message) : StoreException(message)
{
    public override int StatusCode => StatusCodes.Status400BadRequest;
    public override string Title => "The request is not valid.";
}

/// <summary>403 — the caller is authenticated but may not perform this action.</summary>
public sealed class ForbiddenException(string message) : StoreException(message)
{
    public override int StatusCode => StatusCodes.Status403Forbidden;
    public override string Title => "You do not have permission to do this.";
}

/// <summary>404 — the resource does not exist (or is not visible to the caller).</summary>
public sealed class NotFoundException(string message) : StoreException(message)
{
    public override int StatusCode => StatusCodes.Status404NotFound;
    public override string Title => "The resource was not found.";
}

/// <summary>409 — the request conflicts with the current state (duplicate key, insufficient stock, inactive product).</summary>
public sealed class ConflictException(string message) : StoreException(message)
{
    public override int StatusCode => StatusCodes.Status409Conflict;
    public override string Title => "The request conflicts with the current state.";
}

/// <summary>
/// 503 — a downstream dependency (another service over HTTP, the message broker) failed after
/// the resilience pipeline gave up. Adapters translate transport failures into this exception.
/// </summary>
public sealed class DownstreamServiceException(string serviceName, string message, Exception? innerException = null)
    : StoreException(message, innerException)
{
    public string ServiceName { get; } = serviceName;
    public override int StatusCode => StatusCodes.Status503ServiceUnavailable;
    public override string Title => $"The {ServiceName} service is temporarily unavailable.";
}
