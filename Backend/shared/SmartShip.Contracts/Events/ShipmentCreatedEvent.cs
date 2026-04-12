namespace SmartShip.Contracts.Events;

public abstract class IntegrationEvent
{
    public Guid Id { get; }
    public DateTime CreationDate { get; }

    protected IntegrationEvent()
    {
        Id = Guid.NewGuid();
        CreationDate = DateTime.UtcNow;
    }
}

public class ShipmentCreatedEvent : IntegrationEvent
{
    public Guid ShipmentId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public decimal Weight { get; set; }
}

public class ShipmentBookedEvent : IntegrationEvent
{
    public Guid ShipmentId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public string HubId { get; set; } = string.Empty;
}

public class ShipmentDeliveredEvent : IntegrationEvent
{
    public Guid ShipmentId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
}

public class TrackingUpdatedEvent : IntegrationEvent
{
    public Guid ShipmentId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
}