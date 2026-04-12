namespace SmartShip.DocumentService.Models;

public class Document
{
    public Guid DocumentId { get; set; }
    public Guid ShipmentId { get; set; }
    public Guid CustomerId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
}
