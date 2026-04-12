using SmartShip.DocumentService.Models;

namespace SmartShip.DocumentService.Repositories;

public interface IDocumentRepository
{
    Task AddDocumentAsync(Document document);
    Task<List<Document>> GetDocumentsByCustomerAsync(Guid customerId);
    Task<List<Document>> GetDocumentsByShipmentAsync(Guid shipmentId);
    Task<int> GetDocumentsCountByShipmentAsync(Guid shipmentId);
    Task<List<Document>> GetDocumentsByShipmentPagedAsync(Guid shipmentId, int pageNumber, int pageSize);
    Task<Document?> GetDocumentAsync(Guid id);
    Task DeleteDocumentAsync(Document document);
    Task AddDeliveryProofAsync(DeliveryProof proof);
    Task<List<DeliveryProof>> GetDeliveryProofsByShipmentAsync(Guid shipmentId);
    Task SaveChangesAsync();
}