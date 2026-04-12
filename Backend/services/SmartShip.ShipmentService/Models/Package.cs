namespace SmartShip.ShipmentService.Models;

public class Package
{
    public Guid PackageId { get; set; }
    public Guid ShipmentId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Weight { get; set; }
    public string? Description { get; set; }

    public Shipment Shipment { get; set; } = null!;
}
