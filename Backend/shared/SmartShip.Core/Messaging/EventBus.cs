using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SmartShip.Contracts.Events;

namespace SmartShip.Core.Messaging;

public interface IEventBus
{
    Task PublishAsync(IntegrationEvent @event, CancellationToken cancellationToken = default);
}

public class RabbitMQService : IEventBus, IDisposable
{
    private readonly ConnectionFactory _factory;
    private readonly ILogger<RabbitMQService> _logger;
    private readonly string _exchangeName;
    private readonly int _retryCount;
    private readonly int _retryDelayMs;
    private readonly object _connectionLock = new();
    private IConnection? _connection;

    public RabbitMQService(IConfiguration configuration, ILogger<RabbitMQService> logger)
    {
        _logger = logger;
        _exchangeName = configuration["RabbitMQ:Exchange"] ?? "smartship_events";
        _retryCount = Math.Max(1, configuration.GetValue<int?>("RabbitMQ:RetryCount") ?? 3);
        _retryDelayMs = Math.Max(100, configuration.GetValue<int?>("RabbitMQ:RetryDelayMs") ?? 500);

        _factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:Host"] ?? "localhost",
            UserName = configuration["RabbitMQ:Username"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest"
        };
    }

    public async Task PublishAsync(IntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        var eventName = @event.GetType().Name;
        var message = JsonSerializer.Serialize((object)@event);
        var body = Encoding.UTF8.GetBytes(message);

        Exception? lastError = null;
        for (var attempt = 1; attempt <= _retryCount; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var channel = GetOrCreateConnection().CreateModel();
                channel.ExchangeDeclare(exchange: _exchangeName, type: ExchangeType.Direct, durable: true, autoDelete: false);

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;

                channel.BasicPublish(exchange: _exchangeName,
                    routingKey: eventName,
                    basicProperties: properties,
                    body: body);

                return;
            }
            catch (Exception ex)
            {
                lastError = ex;
                _logger.LogWarning(ex, "Failed to publish event {EventName} on attempt {Attempt}/{RetryCount}", eventName, attempt, _retryCount);

                if (attempt == _retryCount)
                {
                    break;
                }

                await Task.Delay(_retryDelayMs * attempt, cancellationToken);
            }
        }

        throw new InvalidOperationException($"Failed to publish event '{eventName}' after {_retryCount} attempts.", lastError);
    }

    private IConnection GetOrCreateConnection()
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        lock (_connectionLock)
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            _connection?.Dispose();
            _connection = _factory.CreateConnection();
            return _connection;
        }
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}