using RabbitMQ.Client;

namespace Store.Messaging.Connection;

/// <summary>One long-lived broker connection per service process; channels are created from it.</summary>
public interface IRabbitMqConnection
{
    /// <summary>True when the connection exists and is open.</summary>
    bool IsConnected { get; }

    /// <summary>Opens the connection on first use, retrying until the broker accepts it or <paramref name="cancellationToken"/> fires.</summary>
    Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken);

    /// <summary>Creates a channel; set <paramref name="publisherConfirms"/> for channels used to publish.</summary>
    Task<IChannel> CreateChannelAsync(bool publisherConfirms, CancellationToken cancellationToken);
}
