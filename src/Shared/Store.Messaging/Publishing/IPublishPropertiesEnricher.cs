using RabbitMQ.Client;
using Store.Contracts.Events;

namespace Store.Messaging.Publishing;

/// <summary>
/// Extension point that adds headers to every outgoing message (for example tracing headers).
/// All registered enrichers run, in registration order, before each publish.
/// </summary>
public interface IPublishPropertiesEnricher
{
    void Enrich(BasicProperties properties, IntegrationEvent integrationEvent);
}
