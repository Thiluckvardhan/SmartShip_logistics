namespace SmartShip.DocumentService.Models;

public class DeliveryProof
{
    public Guid ProofId { get; set; }
    public Guid ShipmentId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string SignerName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime Timestamp { get; set; }
}
