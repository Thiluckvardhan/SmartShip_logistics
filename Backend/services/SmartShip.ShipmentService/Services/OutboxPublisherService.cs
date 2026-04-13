using System.Text.Json;
using SmartShip.Contracts.Events;
using SmartShip.Core.Messaging;
using SmartShip.ShipmentService.Repositories;

namespace SmartShip.ShipmentService.Services;

public class OutboxPublisherService(IServiceProvider serviceProvider, ILogger<OutboxPublisherService> logger) : BackgroundService
{
    private const int BatchSize = 50;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IShipmentRepository>();
                var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

                var pendingMessages = await repository.GetPendingOutboxMessagesAsync(BatchSize);
                if (pendingMessages.Count == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    continue;
                }

                foreach (var outboxMessage in pendingMessages)
                {
                    try
                    {
                        logger.LogInformation("Publishing outbox message {OutboxMessageId} of type {EventType}", outboxMessage.Id, outboxMessage.EventType);

                        var eventType = Type.GetType(outboxMessage.EventType);
                        if (eventType is null)
                        {
                            throw new InvalidOperationException($"Unable to resolve outbox event type '{outboxMessage.EventType}'.");
                        }

                        var deserialized = JsonSerializer.Deserialize(outboxMessage.Payload, eventType) as IntegrationEvent;
                        if (deserialized is null)
                        {
                            throw new InvalidOperationException($"Unable to deserialize outbox payload as IntegrationEvent for type '{outboxMessage.EventType}'.");
                        }

                        await eventBus.PublishAsync(deserialized, stoppingToken);
                        outboxMessage.ProcessedAt = DateTime.UtcNow;
                        outboxMessage.Status = "Published";
                        outboxMessage.LastError = null;
                        logger.LogInformation("Outbox message {OutboxMessageId} published successfully with status {Status}", outboxMessage.Id, outboxMessage.Status);
                    }
                    catch (Exception ex)
                    {
                        outboxMessage.AttemptCount += 1;
                        outboxMessage.Status = "Failed";
                        outboxMessage.LastError = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
                        logger.LogWarning(ex, "Failed processing outbox message {OutboxMessageId} (attempt {AttemptCount})", outboxMessage.Id, outboxMessage.AttemptCount);
                    }
                }

                await repository.SaveChangesAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error in outbox publisher loop.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
