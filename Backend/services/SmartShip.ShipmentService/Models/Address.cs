namespace SmartShip.ShipmentService.Models;

public class Address
{
    public Guid AddressId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;

    public ICollection<Shipment> SenderShipments { get; set; } = new List<Shipment>();
    public ICollection<Shipment> ReceiverShipments { get; set; } = new List<Shipment>();
}
