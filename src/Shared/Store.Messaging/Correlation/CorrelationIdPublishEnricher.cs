using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Store.Contracts.Events;
using Store.Messaging.Publishing;
using Store.Observability.Correlation;

namespace Store.Messaging.Correlation;

/// <summary>
/// Copies the correlation id into every outgoing message: the X-Correlation-ID header (the same name as the
/// HTTP header) and the AMQP correlation_id property. The event's own CorrelationId wins; when it is empty the
/// id of the current request or message is used, so a flow started by one HTTP request keeps one id end to end.
/// </summary>
public sealed class CorrelationIdPublishEnricher(
    ICorrelationIdAccessor accessor,
    ILogger<CorrelationIdPublishEnricher> logger) : IPublishPropertiesEnricher
{
    public void Enrich(BasicProperties properties, IntegrationEvent integrationEvent)
    {
        var correlationId = string.IsNullOrWhiteSpace(integrationEvent.CorrelationId)
            ? accessor.GetOrCreate()
            : integrationEvent.CorrelationId;

        if (!string.IsNullOrWhiteSpace(accessor.CorrelationId) && accessor.CorrelationId != correlationId)
        {
            logger.LogDebug("Event {MessageId} carries correlation id {EventCorrelationId}; current scope is {CurrentCorrelationId}",
                integrationEvent.MessageId, correlationId, accessor.CorrelationId);
        }

        properties.CorrelationId = correlationId;
        properties.Headers ??= new Dictionary<string, object?>();
        properties.Headers[CorrelationIdConstants.HeaderName] = correlationId;
    }
}
