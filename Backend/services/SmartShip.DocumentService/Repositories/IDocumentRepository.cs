using SmartShip.DocumentService.Models;

namespace SmartShip.DocumentService.Repositories;

public interface IDocumentRepository
{
    Task AddDocumentAsync(Document document);
    Task<List<Document>> GetDocumentsByCustomerAsync(Guid customerId);
    Task<List<Document>> GetDocumentsByShipmentAsync(Guid shipmentId);
    Task<int> GetDocumentsCountByShipmentAsync(Guid shipmentId);
    Task<int> GetDocumentsCountByShipmentAndCustomerAsync(Guid shipmentId, Guid customerId);
    Task<List<Document>> GetDocumentsByShipmentPagedAsync(Guid shipmentId, int pageNumber, int pageSize);
    Task<List<Document>> GetDocumentsByShipmentAndCustomerPagedAsync(Guid shipmentId, Guid customerId, int pageNumber, int pageSize);
    Task<Document?> GetDocumentAsync(Guid id);
    Task DeleteDocumentAsync(Document document);
    Task AddDeliveryProofAsync(DeliveryProof proof);
    Task<List<DeliveryProof>> GetDeliveryProofsByShipmentAsync(Guid shipmentId);
    Task SaveChangesAsync();
}