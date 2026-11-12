using Microsoft.Extensions.Diagnostics.HealthChecks;
using Store.Messaging.Connection;

namespace Store.Messaging;

/// <summary>Reports Degraded (not Unhealthy) while the broker connection is not open, so the database
/// remains the deciding health signal and the service keeps serving HTTP while RabbitMQ restarts.</summary>
public sealed class RabbitMqHealthCheck(IRabbitMqConnection connection) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(connection.IsConnected
            ? HealthCheckResult.Healthy("RabbitMQ connection is open.")
            : HealthCheckResult.Degraded("RabbitMQ connection is not open yet."));
}
