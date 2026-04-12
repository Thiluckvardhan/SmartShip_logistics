namespace SmartShip.AdminService.Models;

public class ExceptionRecord
{
    public Guid ExceptionId { get; set; }
    public Guid ShipmentId { get; set; }
    public string ExceptionType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
