using System.Text.Json;
using Storefront.Web.Clients;

namespace Storefront.Web.Infrastructure;

/// <summary>A user-facing explanation of a failed gateway call.</summary>
public sealed record ApiError(int StatusCode, string Title, string Message, IReadOnlyDictionary<string, string[]> FieldErrors);

/// <summary>Turns <see cref="GatewayApiException"/> into friendly messages for 400/401/403/404/409/429/5xx.</summary>
public static class ApiErrors
{
    public static ApiError From(GatewayApiException exception)
    {
        var detail = ReadDetail(exception, out var fieldErrors);
        var (title, fallback) = exception.StatusCode switch
        {
            400 => ("Please check the form", "Some of the information you entered is not valid."),
            401 => ("Please sign in", "Your session has ended or you are not signed in."),
            403 => ("Not allowed", "Your account does not have permission to do that."),
            404 => ("Not found", "We could not find what you were looking for."),
            409 => ("That did not work", "This conflicts with the current state - please refresh and try again."),
            429 => ("Slow down a little", "Too many attempts. Please wait a minute and try again."),
            503 => ("Service busy", "A part of the store is temporarily unavailable. Please try again in a moment."),
            >= 500 => ("Something went wrong", "The store could not complete your request. Please try again."),
            _ => ("Request failed", "The request could not be completed.")
        };

        return new ApiError(exception.StatusCode, title, string.IsNullOrWhiteSpace(detail) ? fallback : detail, fieldErrors);
    }

    private static string? ReadDetail(GatewayApiException exception, out IReadOnlyDictionary<string, string[]> fieldErrors)
    {
        fieldErrors = new Dictionary<string, string[]>();

        if (exception is GatewayApiException<ProblemDetails> { Result: { } problem })
        {
            if (problem.Errors is { Count: > 0 } errors)
            {
                fieldErrors = errors.ToDictionary(e => e.Key, e => e.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
            }

            return problem.Detail ?? (fieldErrors.Count > 0 ? string.Join(" ", fieldErrors.Values.SelectMany(v => v)) : null);
        }

        var body = exception.Response;
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        if (!body.TrimStart().StartsWith('{'))
        {
            // The gateway's rate limiter answers with plain text.
            return body.Length <= 300 ? body.Trim() : null;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.TryGetProperty("detail", out var d) && d.ValueKind == JsonValueKind.String
                ? d.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
