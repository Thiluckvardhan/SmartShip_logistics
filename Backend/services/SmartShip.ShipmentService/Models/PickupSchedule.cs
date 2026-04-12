namespace SmartShip.ShipmentService.Models;

public class PickupSchedule
{
    public Guid PickupScheduleId { get; set; }
    public Guid ShipmentId { get; set; }
    public DateTime PickupDate { get; set; }
    public string? Notes { get; set; }

    public Shipment Shipment { get; set; } = null!;
}
