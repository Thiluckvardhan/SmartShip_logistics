using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SmartShip.Contracts.Events;
using SmartShip.TrackingService.Models;
using SmartShip.TrackingService.Repositories;

namespace SmartShip.TrackingService.Services;

public class TrackingEventConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TrackingEventConsumer> _logger;
    private readonly ConnectionFactory _factory;
    private readonly string _exchangeName;
    private readonly string[] _queues;
    private IConnection? _connection;
    private IModel? _channel;
    private bool _isConsumerAttached;

    public TrackingEventConsumer(IServiceProvider serviceProvider, IConfiguration config, ILogger<TrackingEventConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _exchangeName = config["RabbitMQ:Exchange"] ?? "smartship_events";

        var shipmentCreatedQueue = config["RabbitMQ:Queues:ShipmentCreated"] ?? "shipment-created-queue";
        var shipmentPickedUpQueue = config["RabbitMQ:Queues:ShipmentPickedUp"] ?? "shipment-pickedup-queue";
        var shipmentDeliveredQueue = config["RabbitMQ:Queues:ShipmentDelivered"] ?? "shipment-delivered-queue";
        var shipmentExceptionQueue = config["RabbitMQ:Queues:ShipmentException"] ?? "shipment-exception-queue";

        _queues = [shipmentCreatedQueue, shipmentPickedUpQueue, shipmentDeliveredQueue, shipmentExceptionQueue];

        _factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:Host"] ?? "localhost",
            UserName = config["RabbitMQ:Username"] ?? "guest",
            Password = config["RabbitMQ:Password"] ?? "guest"
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                EnsureConnected();
                AttachConsumerIfNeeded();

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                CleanupConnection();
                _logger.LogWarning(ex, "Tracking consumer could not connect to RabbitMQ. Retrying in 5 seconds.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private void EnsureConnected()
    {
        if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
        {
            return;
        }

        CleanupConnection();

        _connection = _factory.CreateConnection();
        _channel = _connection.CreateModel();
        _isConsumerAttached = false;

        _connection.ConnectionShutdown += (_, args) =>
        {
            _logger.LogWarning("RabbitMQ connection closed: {ReplyText}", args.ReplyText);
            _isConsumerAttached = false;
        };

        _channel.ExchangeDeclare(_exchangeName, ExchangeType.Direct, durable: true, autoDelete: false);
        _channel.BasicQos(0, 20, false);

        var shipmentCreatedQueue = _queues[0];
        var shipmentPickedUpQueue = _queues[1];
        var shipmentDeliveredQueue = _queues[2];
        var shipmentExceptionQueue = _queues[3];

        _channel.QueueDeclare(shipmentCreatedQueue, true, false, false);
        _channel.QueueBind(shipmentCreatedQueue, _exchangeName, nameof(ShipmentCreatedEvent));

        _channel.QueueDeclare(shipmentPickedUpQueue, true, false, false);
        _channel.QueueBind(shipmentPickedUpQueue, _exchangeName, nameof(ShipmentBookedEvent));

        _channel.QueueDeclare(shipmentDeliveredQueue, true, false, false);
        _channel.QueueBind(shipmentDeliveredQueue, _exchangeName, nameof(ShipmentDeliveredEvent));

        _channel.QueueDeclare(shipmentExceptionQueue, true, false, false);
        _channel.QueueBind(shipmentExceptionQueue, _exchangeName, nameof(TrackingUpdatedEvent));

        _logger.LogInformation("Tracking consumer connected to RabbitMQ host {Host}.", _factory.HostName);
    }

    private void AttachConsumerIfNeeded()
    {
        if (_isConsumerAttached || _channel is not { IsOpen: true })
        {
            return;
        }

        var channel = _channel;
        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += async (_, ea) =>
        {
            try
            {
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                var routingKey = ea.RoutingKey;

                using var scope = _serviceProvider.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<ITrackingRepository>();

                if (routingKey == nameof(ShipmentCreatedEvent))
                {
                    var evt = JsonSerializer.Deserialize<ShipmentCreatedEvent>(message);
                    if (evt != null)
                    {
                        await repo.AddEventAsync(new TrackingEvent
                        {
                            EventId = Guid.NewGuid(),
                            TrackingNumber = evt.TrackingNumber,
                            Status = "CREATED",
                            Location = "System",
                            Description = "Shipment Created",
                            Timestamp = DateTime.UtcNow
                        });
                    }
                }
                else if (routingKey == nameof(TrackingUpdatedEvent))
                {
                    var evt = JsonSerializer.Deserialize<TrackingUpdatedEvent>(message);
                    if (evt != null)
                    {
                        await repo.AddEventAsync(new TrackingEvent
                        {
                            EventId = Guid.NewGuid(),
                            TrackingNumber = evt.TrackingNumber,
                            Status = evt.Status,
                            Location = evt.Location,
                            Description = evt.Remarks,
                            Timestamp = DateTime.UtcNow
                        });
                    }
                }
                else if (routingKey == nameof(ShipmentBookedEvent))
                {
                    var evt = JsonSerializer.Deserialize<ShipmentBookedEvent>(message);
                    if (evt != null)
                    {
                        var status = string.IsNullOrWhiteSpace(evt.HubId) ? "PICKED_UP" : evt.HubId.Trim();
                        await repo.AddEventAsync(new TrackingEvent
                        {
                            EventId = Guid.NewGuid(),
                            TrackingNumber = evt.TrackingNumber,
                            Status = status,
                            Location = "Hub",
                            Description = $"Shipment moved to {status}",
                            Timestamp = DateTime.UtcNow
                        });
                    }
                }
                else if (routingKey == nameof(ShipmentDeliveredEvent))
                {
                    var evt = JsonSerializer.Deserialize<ShipmentDeliveredEvent>(message);
                    if (evt != null)
                    {
                        await repo.AddEventAsync(new TrackingEvent
                        {
                            EventId = Guid.NewGuid(),
                            TrackingNumber = evt.TrackingNumber,
                            Status = "DELIVERED",
                            Location = "Destination",
                            Description = "Shipment Delivered",
                            Timestamp = DateTime.UtcNow
                        });
                    }
                }

                await repo.SaveChangesAsync();
                channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tracking consumer failed to process message with routing key {RoutingKey}", ea.RoutingKey);
                channel.BasicNack(ea.DeliveryTag, false, requeue: false);
            }
        };

        foreach (var queue in _queues.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            channel.BasicConsume(queue, false, consumer);
        }

        _isConsumerAttached = true;
        _logger.LogInformation("Tracking consumer subscriptions attached for {QueueCount} queues.", _queues.Length);
    }

    private void CleanupConnection()
    {
        try
        {
            _channel?.Dispose();
        }
        catch
        {
            // Ignore cleanup exceptions.
        }

        try
        {
            _connection?.Dispose();
        }
        catch
        {
            // Ignore cleanup exceptions.
        }

        _channel = null;
        _connection = null;
        _isConsumerAttached = false;
    }

    public override void Dispose()
    {
        CleanupConnection();
        base.Dispose();
    }
}