namespace SmartShip.AdminService.Models;

public class ServiceLocation
{
    public Guid LocationId { get; set; }
    public Guid HubId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public Hub Hub { get; set; } = null!;
}
