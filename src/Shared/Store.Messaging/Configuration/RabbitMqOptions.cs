using System.ComponentModel.DataAnnotations;

namespace Store.Messaging.Configuration;

/// <summary>Bound from the "RabbitMq" configuration section (RabbitMq__HostName etc. in docker-compose).</summary>
public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    [Required] public string HostName { get; set; } = "localhost";
    [Range(1, 65535)] public int Port { get; set; } = 5672;
    [Required] public string UserName { get; set; } = "guest";
    [Required] public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";

    /// <summary>Topic exchange every integration event is published to.</summary>
    [Required] public string ExchangeName { get; set; } = "store.events";

    /// <summary>Direct exchange that receives messages rejected from a queue; routes to "&lt;queue&gt;.dlq".</summary>
    [Required] public string DeadLetterExchangeName { get; set; } = "store.dlx";

    /// <summary>Times a consumer tries a message before it is rejected to the dead-letter queue.</summary>
    [Range(1, 20)] public int MaxDeliveryAttempts { get; set; } = 3;

    /// <summary>Base delay between consumer attempts; multiplied by the attempt number.</summary>
    [Range(0, 60000)] public int RetryDelayMilliseconds { get; set; } = 1000;

    /// <summary>Times a publish is retried when the broker does not confirm it.</summary>
    [Range(1, 10)] public int PublishRetryCount { get; set; } = 3;

    /// <summary>Messages a consumer may hold unacknowledged at once.</summary>
    [Range(1, 1000)] public ushort PrefetchCount { get; set; } = 10;

    /// <summary>Name shown for the connection in the RabbitMQ management UI.</summary>
    public string ClientProvidedName { get; set; } = "store-platform";
}
