using Microsoft.AspNetCore.Http;

namespace SmartShip.DocumentService.Services;

public interface IDocumentService
{
    Task<object?> UploadDocumentAsync(Guid shipmentId, string documentType, IFormFile file, Guid customerId, string contentRootPath);
    Task<object?> UpdateDocumentAsync(Guid id, Guid shipmentId, IFormFile file, string contentRootPath);
    Task<object?> GetDocumentAsync(Guid id);
    Task<object> GetDocumentsByCustomerAsync(Guid customerId);
    Task<object> GetDocumentsAsync(Guid shipmentId, int pageNumber, int pageSize);
    Task<bool> DeleteDocumentAsync(Guid id);
    Task<object?> CreateDeliveryProofAsync(Guid shipmentId, string signerName, string? notes, IFormFile file, string contentRootPath);
    Task<object?> GetDeliveryProofAsync(Guid shipmentId);
}