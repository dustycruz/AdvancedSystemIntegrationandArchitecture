using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Store.Contracts.Events;
using Store.Messaging.Configuration;
using Store.Messaging.Connection;

namespace Store.Messaging.Consuming;

/// <summary>
/// Consumer that resolves <typeparamref name="THandler"/> from a fresh DI scope for every message.
/// Registered with <c>AddEventConsumer&lt;TEvent, THandler&gt;("queue-name")</c>.
/// </summary>
public sealed class ScopedEventConsumer<TEvent, THandler>(
    string queueName,
    IServiceScopeFactory scopeFactory,
    IRabbitMqConnection connection,
    IOptions<RabbitMqOptions> options,
    IEnumerable<IConsumeScopeProvider> scopeProviders,
    ILogger<ScopedEventConsumer<TEvent, THandler>> logger)
    : RabbitMqConsumerBase<TEvent>(connection, options, scopeProviders, logger)
    where TEvent : IntegrationEvent
    where THandler : class, IEventHandler<TEvent>
{
    public override string QueueName { get; } = queueName;

    protected override async Task HandleAsync(TEvent message, MessageContext context, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<THandler>();
        await handler.HandleAsync(message, context, cancellationToken);
    }
}
