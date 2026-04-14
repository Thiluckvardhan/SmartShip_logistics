using Microsoft.AspNetCore.Http;

namespace SmartShip.DocumentService.Services;

public interface IDocumentService
{
    Task<object?> UploadDocumentAsync(Guid shipmentId, string documentType, IFormFile file, Guid customerId, string contentRootPath);
    Task<object?> UpdateDocumentAsync(Guid id, Guid shipmentId, IFormFile? file, string contentRootPath, Guid requesterId, bool isAdmin);
    Task<object?> GetDocumentAsync(Guid id, Guid requesterId, bool isAdmin);
    Task<(byte[] Content, string ContentType, string FileName)?> DownloadDocumentAsync(Guid id, Guid requesterId, bool isAdmin);
    Task<object> GetDocumentsByCustomerAsync(Guid customerId);
    Task<object> GetDocumentsAsync(Guid shipmentId, int pageNumber, int pageSize, Guid requesterId, bool isAdmin);
    Task<bool> DeleteDocumentAsync(Guid id, Guid requesterId, bool isAdmin);
    Task<object?> CreateDeliveryProofAsync(Guid shipmentId, string signerName, string? notes, IFormFile file, string contentRootPath, Guid requesterId, bool isAdmin);
    Task<object?> GetDeliveryProofAsync(Guid shipmentId, Guid requesterId, bool isAdmin);
}