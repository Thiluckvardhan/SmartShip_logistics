namespace SmartShip.ShipmentService.Models;

public enum ShipmentStatus
{
    Draft,
    Booked,
    PickedUp,
    InTransit,
    OutForDelivery,
    Delivered,
    Delayed,
    Failed,
    Returned
}
