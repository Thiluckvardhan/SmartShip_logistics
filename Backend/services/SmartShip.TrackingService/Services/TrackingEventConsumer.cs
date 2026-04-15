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
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _exchangeName;
    private readonly string[] _queues;

    public TrackingEventConsumer(IServiceProvider serviceProvider, IConfiguration config)
    {
        _serviceProvider = serviceProvider;
        _exchangeName = config["RabbitMQ:Exchange"] ?? "smartship_events";

        var shipmentCreatedQueue = config["RabbitMQ:Queues:ShipmentCreated"] ?? "shipment-created-queue";
        var shipmentPickedUpQueue = config["RabbitMQ:Queues:ShipmentPickedUp"] ?? "shipment-pickedup-queue";
        var shipmentDeliveredQueue = config["RabbitMQ:Queues:ShipmentDelivered"] ?? "shipment-delivered-queue";
        var shipmentExceptionQueue = config["RabbitMQ:Queues:ShipmentException"] ?? "shipment-exception-queue";

        _queues = [shipmentCreatedQueue, shipmentPickedUpQueue, shipmentDeliveredQueue, shipmentExceptionQueue];

        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:Host"] ?? "localhost",
            UserName = config["RabbitMQ:Username"] ?? "guest",
            Password = config["RabbitMQ:Password"] ?? "guest"
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(_exchangeName, ExchangeType.Direct, durable: true, autoDelete: false);
        _channel.BasicQos(0, 20, false);

        _channel.QueueDeclare(shipmentCreatedQueue, true, false, false);
        _channel.QueueBind(shipmentCreatedQueue, _exchangeName, nameof(ShipmentCreatedEvent));

        _channel.QueueDeclare(shipmentPickedUpQueue, true, false, false);
        _channel.QueueBind(shipmentPickedUpQueue, _exchangeName, nameof(ShipmentBookedEvent));

        _channel.QueueDeclare(shipmentDeliveredQueue, true, false, false);
        _channel.QueueBind(shipmentDeliveredQueue, _exchangeName, nameof(ShipmentDeliveredEvent));

        _channel.QueueDeclare(shipmentExceptionQueue, true, false, false);
        _channel.QueueBind(shipmentExceptionQueue, _exchangeName, nameof(TrackingUpdatedEvent));
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(_channel);

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
                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Tracking consumer failed: {ex.Message}");
                _channel.BasicNack(ea.DeliveryTag, false, requeue: false);
            }
        };

        foreach (var queue in _queues.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            _channel.BasicConsume(queue, false, consumer);
        }

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
        base.Dispose();
    }
}