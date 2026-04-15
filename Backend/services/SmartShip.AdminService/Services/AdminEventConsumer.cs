using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SmartShip.AdminService.Models;
using SmartShip.AdminService.Repositories;
using SmartShip.Contracts.Events;

namespace SmartShip.AdminService.Services;

public class AdminEventConsumer : BackgroundService
{
    private static readonly string[] ExceptionStatuses = ["DELAYED", "FAILED", "RETURNED"];

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AdminEventConsumer> _logger;
    private readonly ConnectionFactory _factory;
    private readonly string _exchangeName;
    private readonly string[] _queues;
    private IConnection? _connection;
    private IModel? _channel;
    private bool _isConsumerAttached;

    public AdminEventConsumer(IServiceProvider serviceProvider, IConfiguration config, ILogger<AdminEventConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _exchangeName = config["RabbitMQ:Exchange"] ?? "smartship_events";

        var shipmentBookedQueue = config["RabbitMQ:Queues:ShipmentBooked"] ?? "admin-shipment-booked-queue";
        var shipmentDeliveredQueue = config["RabbitMQ:Queues:ShipmentDelivered"] ?? "admin-shipment-delivered-queue";
        var trackingUpdatedQueue = config["RabbitMQ:Queues:TrackingUpdated"] ?? "admin-tracking-updated-queue";
        var shipmentIssueReportedQueue = config["RabbitMQ:Queues:ShipmentIssueReported"] ?? "admin-shipment-issue-reported-queue";
        _queues = [shipmentBookedQueue, shipmentDeliveredQueue, trackingUpdatedQueue, shipmentIssueReportedQueue];

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
                _logger.LogWarning(ex, "Admin RabbitMQ consumer could not connect. Retrying in 5 seconds.");
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
            _logger.LogWarning("Admin RabbitMQ connection closed: {ReplyText}", args.ReplyText);
            _isConsumerAttached = false;
        };

        _channel.ExchangeDeclare(_exchangeName, ExchangeType.Direct, durable: true, autoDelete: false);
        _channel.BasicQos(0, 20, false);

        var shipmentBookedQueue = _queues[0];
        var shipmentDeliveredQueue = _queues[1];
        var trackingUpdatedQueue = _queues[2];
        var shipmentIssueReportedQueue = _queues[3];

        _channel.QueueDeclare(shipmentBookedQueue, true, false, false);
        _channel.QueueBind(shipmentBookedQueue, _exchangeName, nameof(ShipmentBookedEvent));

        _channel.QueueDeclare(shipmentDeliveredQueue, true, false, false);
        _channel.QueueBind(shipmentDeliveredQueue, _exchangeName, nameof(ShipmentDeliveredEvent));

        _channel.QueueDeclare(trackingUpdatedQueue, true, false, false);
        _channel.QueueBind(trackingUpdatedQueue, _exchangeName, nameof(TrackingUpdatedEvent));

        _channel.QueueDeclare(shipmentIssueReportedQueue, true, false, false);
        _channel.QueueBind(shipmentIssueReportedQueue, _exchangeName, nameof(ShipmentIssueReportedEvent));

        _logger.LogInformation("Admin event consumer connected to RabbitMQ host {Host}.", _factory.HostName);
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
                var repository = scope.ServiceProvider.GetRequiredService<IAdminRepository>();

                if (routingKey == nameof(ShipmentBookedEvent))
                {
                    var evt = JsonSerializer.Deserialize<ShipmentBookedEvent>(message);
                    if (evt is not null)
                    {
                        _logger.LogInformation("Admin received ShipmentBookedEvent for shipment {ShipmentId} tracking {TrackingNumber}", evt.ShipmentId, evt.TrackingNumber);
                    }
                }
                else if (routingKey == nameof(ShipmentDeliveredEvent))
                {
                    var evt = JsonSerializer.Deserialize<ShipmentDeliveredEvent>(message);
                    if (evt is not null)
                    {
                        await ResolveOpenExceptionsAsync(repository, evt.ShipmentId, "Shipment delivered; exception auto-resolved from event stream.");
                    }
                }
                else if (routingKey == nameof(TrackingUpdatedEvent))
                {
                    var evt = JsonSerializer.Deserialize<TrackingUpdatedEvent>(message);
                    if (evt is not null)
                    {
                        await HandleTrackingUpdateAsync(repository, evt);
                    }
                }
                else if (routingKey == nameof(ShipmentIssueReportedEvent))
                {
                    var evt = JsonSerializer.Deserialize<ShipmentIssueReportedEvent>(message);
                    if (evt is not null)
                    {
                        await HandleIssueReportedAsync(repository, evt);
                    }
                }

                await repository.SaveChangesAsync();
                channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin consumer failed to process message with routing key {RoutingKey}", ea.RoutingKey);
                channel.BasicNack(ea.DeliveryTag, false, requeue: false);
            }
        };

        foreach (var queue in _queues.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            channel.BasicConsume(queue, false, consumer);
        }

        _isConsumerAttached = true;
        _logger.LogInformation("Admin event consumer subscriptions attached for {QueueCount} queues.", _queues.Length);
    }

    private static async Task HandleTrackingUpdateAsync(IAdminRepository repository, TrackingUpdatedEvent evt)
    {
        var normalizedStatus = evt.Status.Trim().ToUpperInvariant();
        if (!ExceptionStatuses.Contains(normalizedStatus))
        {
            return;
        }

        var openExceptions = await repository.GetExceptionsByShipmentAsync(evt.ShipmentId);
        var existingOpen = openExceptions.FirstOrDefault(x => string.Equals(x.Status, "Open", StringComparison.OrdinalIgnoreCase));

        if (existingOpen is not null)
        {
            existingOpen.ExceptionType = normalizedStatus;
            existingOpen.Description = string.IsNullOrWhiteSpace(evt.Remarks)
                ? $"Shipment event flagged as {normalizedStatus}."
                : evt.Remarks.Trim();
            existingOpen.CreatedAt = DateTime.UtcNow;
            existingOpen.ResolvedAt = null;
            return;
        }

        await repository.AddExceptionAsync(new ExceptionRecord
        {
            ExceptionId = Guid.NewGuid(),
            ShipmentId = evt.ShipmentId,
            ExceptionType = normalizedStatus,
            Description = string.IsNullOrWhiteSpace(evt.Remarks)
                ? $"Shipment event flagged as {normalizedStatus}."
                : evt.Remarks.Trim(),
            Status = "Open",
            CreatedAt = DateTime.UtcNow,
            ResolvedAt = null
        });
    }

    private static async Task ResolveOpenExceptionsAsync(IAdminRepository repository, Guid shipmentId, string resolutionMessage)
    {
        var exceptions = await repository.GetExceptionsByShipmentAsync(shipmentId);
        foreach (var record in exceptions.Where(x => string.Equals(x.Status, "Open", StringComparison.OrdinalIgnoreCase)))
        {
            record.Status = "Resolved";
            record.ResolvedAt = DateTime.UtcNow;
            record.Description = string.IsNullOrWhiteSpace(record.Description)
                ? resolutionMessage
                : $"{record.Description} | Resolution: {resolutionMessage}";
        }
    }

    private static async Task HandleIssueReportedAsync(IAdminRepository repository, ShipmentIssueReportedEvent evt)
    {
        var existing = await repository.GetExceptionsByShipmentAsync(evt.ShipmentId);
        var duplicate = existing.FirstOrDefault(x =>
            string.Equals(x.ExceptionType, evt.IssueType, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.Description, evt.Description, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.Status, "Pending", StringComparison.OrdinalIgnoreCase));

        if (duplicate is not null)
        {
            return;
        }

        await repository.AddExceptionAsync(new ExceptionRecord
        {
            ExceptionId = Guid.NewGuid(),
            ShipmentId = evt.ShipmentId,
            ExceptionType = evt.IssueType.Trim(),
            Description = evt.Description.Trim(),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            ResolvedAt = null
        });
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