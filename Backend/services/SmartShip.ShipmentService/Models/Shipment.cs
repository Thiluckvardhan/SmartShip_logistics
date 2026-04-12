namespace SmartShip.ShipmentService.Models;

public class Shipment
{
    public Guid ShipmentId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid SenderAddressId { get; set; }
    public Guid ReceiverAddressId { get; set; }
    public decimal TotalWeight { get; set; }
    public decimal EstimatedRate { get; set; }
    public ShipmentStatus Status { get; set; } = ShipmentStatus.Draft;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Address SenderAddress { get; set; } = null!;
    public Address ReceiverAddress { get; set; } = null!;
    public ICollection<Package> Packages { get; set; } = new List<Package>();
    public ICollection<PickupSchedule> PickupSchedules { get; set; } = new List<PickupSchedule>();
}
