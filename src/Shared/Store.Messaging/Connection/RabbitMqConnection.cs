using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Store.Messaging.Configuration;

namespace Store.Messaging.Connection;

public sealed class RabbitMqConnection(IOptions<RabbitMqOptions> options, ILogger<RabbitMqConnection> logger)
    : IRabbitMqConnection, IAsyncDisposable
{
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(15);

    private readonly RabbitMqOptions _options = options.Value;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;

    public bool IsConnected => _connection is { IsOpen: true };

    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true } open)
        {
            return open;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true } existing)
            {
                return existing;
            }

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
                ClientProvidedName = _options.ClientProvidedName,
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
            };

            var delay = TimeSpan.FromSeconds(1);
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    _connection = await factory.CreateConnectionAsync(cancellationToken);
                    logger.LogInformation("Connected to RabbitMQ at {Host}:{Port} as {ClientName}",
                        _options.HostName, _options.Port, _options.ClientProvidedName);
                    return _connection;
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    logger.LogWarning("RabbitMQ at {Host}:{Port} is not reachable (attempt {Attempt}): {Error}. Retrying in {Delay}",
                        _options.HostName, _options.Port, attempt, ex.Message, delay);
                    await Task.Delay(delay, cancellationToken);
                    delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, MaxRetryDelay.Ticks));
                }
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IChannel> CreateChannelAsync(bool publisherConfirms, CancellationToken cancellationToken)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: publisherConfirms,
            publisherConfirmationTrackingEnabled: publisherConfirms);
        return await connection.CreateChannelAsync(channelOptions, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }

        _gate.Dispose();
    }
}
