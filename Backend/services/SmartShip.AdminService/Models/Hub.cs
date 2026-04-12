namespace SmartShip.AdminService.Models;

public class Hub
{
    public Guid HubId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public string ManagerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }

    public ICollection<ServiceLocation> ServiceLocations { get; set; } = new List<ServiceLocation>();
}
