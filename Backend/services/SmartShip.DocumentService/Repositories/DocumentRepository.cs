using Microsoft.EntityFrameworkCore;
using SmartShip.DocumentService.Data;
using SmartShip.DocumentService.Models;

namespace SmartShip.DocumentService.Repositories;

public class DocumentRepository(DocumentDbContext dbContext) : IDocumentRepository
{
    public async Task AddDocumentAsync(Document document) => await dbContext.Documents.AddAsync(document);

    public Task<List<Document>> GetDocumentsByCustomerAsync(Guid customerId) => dbContext.Documents
        .Where(x => x.CustomerId == customerId)
        .OrderByDescending(x => x.UploadedAt)
        .ToListAsync();

    public Task<List<Document>> GetDocumentsByShipmentAsync(Guid shipmentId) => dbContext.Documents
        .Where(x => x.ShipmentId == shipmentId)
        .OrderByDescending(x => x.UploadedAt)
        .ToListAsync();

    public Task<int> GetDocumentsCountByShipmentAsync(Guid shipmentId) =>
        dbContext.Documents.CountAsync(x => x.ShipmentId == shipmentId);

    public Task<List<Document>> GetDocumentsByShipmentPagedAsync(Guid shipmentId, int pageNumber, int pageSize) =>
        dbContext.Documents
            .Where(x => x.ShipmentId == shipmentId)
            .OrderByDescending(x => x.UploadedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

    public Task<Document?> GetDocumentAsync(Guid id) => dbContext.Documents.FirstOrDefaultAsync(x => x.DocumentId == id);

    public Task DeleteDocumentAsync(Document document)
    {
        dbContext.Documents.Remove(document);
        return Task.CompletedTask;
    }

    public async Task AddDeliveryProofAsync(DeliveryProof proof) => await dbContext.DeliveryProofs.AddAsync(proof);

    public Task<List<DeliveryProof>> GetDeliveryProofsByShipmentAsync(Guid shipmentId) => dbContext.DeliveryProofs
        .Where(x => x.ShipmentId == shipmentId)
        .OrderByDescending(x => x.Timestamp)
        .ToListAsync();

    public Task SaveChangesAsync() => dbContext.SaveChangesAsync();
}