using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Store.Contracts.Events;
using Store.Messaging.Configuration;
using Store.Messaging.Connection;
using Store.Messaging.Correlation;
using Store.Messaging.Consuming;
using Store.Messaging.Publishing;

namespace Store.Messaging;

public static class MessagingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the RabbitMQ connection, the confirming event publisher and correlation id propagation,
    /// bound to the "RabbitMq" section.
    /// </summary>
    public static IServiceCollection AddRabbitMqMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IRabbitMqConnection, RabbitMqConnection>();
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

        // X-Correlation-ID travels in a message header and is restored (with a logging scope) when consumed.
        services.AddSingleton<IPublishPropertiesEnricher, CorrelationIdPublishEnricher>();
        services.AddSingleton<IConsumeScopeProvider, CorrelationIdConsumeScopeProvider>();
        return services;
    }

    /// <summary>
    /// Registers a background consumer reading <paramref name="queueName"/> and dispatching each
    /// <typeparamref name="TEvent"/> to a scoped <typeparamref name="THandler"/>.
    /// </summary>
    public static IServiceCollection AddEventConsumer<TEvent, THandler>(this IServiceCollection services, string queueName)
        where TEvent : IntegrationEvent
        where THandler : class, IEventHandler<TEvent>
    {
        services.AddScoped<THandler>();
        services.AddHostedService(provider => new ScopedEventConsumer<TEvent, THandler>(
            queueName,
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<IRabbitMqConnection>(),
            provider.GetRequiredService<IOptions<RabbitMqOptions>>(),
            provider.GetServices<IConsumeScopeProvider>(),
            provider.GetRequiredService<ILogger<ScopedEventConsumer<TEvent, THandler>>>()));
        return services;
    }
}
