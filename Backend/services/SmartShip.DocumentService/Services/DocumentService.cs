using SmartShip.DocumentService.Models;
using SmartShip.DocumentService.Repositories;

namespace SmartShip.DocumentService.Services;

public class DocumentService(IDocumentRepository repository) : IDocumentService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/png",
        "image/jpeg",
        "image/jpg"
    };

    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

    public async Task<object?> UploadDocumentAsync(Guid shipmentId, string documentType, IFormFile file, Guid customerId, string contentRootPath)
    {
        if (file.Length == 0 || file.Length > MaxFileSize) return null;
        if (!AllowedContentTypes.Contains(file.ContentType)) return null;

        var uploadsRoot = Path.Combine(contentRootPath, "uploads", "documents");
        Directory.CreateDirectory(uploadsRoot);

        var fileName = $"{Guid.NewGuid():N}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(uploadsRoot, fileName);

        await using (var stream = File.Create(filePath))
        {
            await file.CopyToAsync(stream);
        }

        var document = new Document
        {
            DocumentId = Guid.NewGuid(),
            ShipmentId = shipmentId,
            CustomerId = customerId,
            FileName = file.FileName,
            FilePath = filePath,
            DocumentType = documentType.Trim(),
            ContentType = file.ContentType,
            UploadedAt = DateTime.UtcNow
        };

        await repository.AddDocumentAsync(document);
        await repository.SaveChangesAsync();
        return new
        {
            document.DocumentId,
            document.ShipmentId,
            document.FileName,
            document.DocumentType,
            document.ContentType,
            document.UploadedAt
        };
    }

    public async Task<object> GetDocumentsAsync(Guid shipmentId, int pageNumber, int pageSize, Guid requesterId, bool isAdmin)
    {
        var totalCount = isAdmin
            ? await repository.GetDocumentsCountByShipmentAsync(shipmentId)
            : await repository.GetDocumentsCountByShipmentAndCustomerAsync(shipmentId, requesterId);

        var items = isAdmin
            ? await repository.GetDocumentsByShipmentPagedAsync(shipmentId, pageNumber, pageSize)
            : await repository.GetDocumentsByShipmentAndCustomerPagedAsync(shipmentId, requesterId, pageNumber, pageSize);

        return new
        {
            Items = items.Select(x => new
            {
                x.DocumentId,
                x.ShipmentId,
                x.FileName,
                x.DocumentType,
                x.ContentType,
                x.UploadedAt
            }).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<object> GetDocumentsByCustomerAsync(Guid customerId)
    {
        var items = await repository.GetDocumentsByCustomerAsync(customerId);
        return items.Select(x => new
        {
            x.DocumentId,
            x.ShipmentId,
            x.CustomerId,
            x.FileName,
            x.DocumentType,
            x.ContentType,
            x.UploadedAt
        }).ToList();
    }

    public async Task<object?> GetDocumentAsync(Guid id, Guid requesterId, bool isAdmin)
    {
        var x = await repository.GetDocumentAsync(id);
        if (x is not null && !isAdmin && x.CustomerId != requesterId)
        {
            return null;
        }

        return x is null ? null : new
        {
            x.DocumentId,
            x.ShipmentId,
            x.CustomerId,
            x.FileName,
            x.DocumentType,
            x.ContentType,
            x.UploadedAt
        };
    }

    public async Task<object?> UpdateDocumentAsync(Guid id, Guid shipmentId, IFormFile? file, string contentRootPath, Guid requesterId, bool isAdmin)
    {
        var doc = await repository.GetDocumentAsync(id);
        if (doc is null) return null;
        if (!isAdmin && doc.CustomerId != requesterId) return null;

        doc.ShipmentId = shipmentId;

        if (file is not null)
        {
            if (file.Length == 0 || file.Length > MaxFileSize) return null;
            if (!AllowedContentTypes.Contains(file.ContentType)) return null;

            var uploadsRoot = Path.Combine(contentRootPath, "uploads", "documents");
            Directory.CreateDirectory(uploadsRoot);

            var fileName = $"{Guid.NewGuid():N}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadsRoot, fileName);

            await using (var stream = File.Create(filePath))
            {
                await file.CopyToAsync(stream);
            }

            if (File.Exists(doc.FilePath))
                File.Delete(doc.FilePath);

            doc.FileName = file.FileName;
            doc.FilePath = filePath;
            doc.ContentType = file.ContentType;
        }

        await repository.SaveChangesAsync();
        return new
        {
            doc.DocumentId,
            doc.ShipmentId,
            doc.CustomerId,
            doc.FileName,
            doc.DocumentType,
            doc.ContentType,
            doc.UploadedAt
        };
    }

    public async Task<bool> DeleteDocumentAsync(Guid id, Guid requesterId, bool isAdmin)
    {
        var doc = await repository.GetDocumentAsync(id);
        if (doc is null) return false;
        if (!isAdmin && doc.CustomerId != requesterId) return false;

        if (File.Exists(doc.FilePath)) File.Delete(doc.FilePath);
        await repository.DeleteDocumentAsync(doc);
        await repository.SaveChangesAsync();
        return true;
    }

    public async Task<object?> CreateDeliveryProofAsync(Guid shipmentId, string signerName, string? notes, IFormFile file, string contentRootPath, Guid requesterId, bool isAdmin)
    {
        if (file.Length == 0 || file.Length > MaxFileSize) return null;

        if (!isAdmin)
        {
            var customerDocs = await repository.GetDocumentsByShipmentAndCustomerPagedAsync(shipmentId, requesterId, 1, 1);
            if (customerDocs.Count == 0) return null;
        }

        var uploadsRoot = Path.Combine(contentRootPath, "uploads", "delivery-proof");
        Directory.CreateDirectory(uploadsRoot);

        var fileName = $"{Guid.NewGuid():N}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(uploadsRoot, fileName);

        await using (var stream = File.Create(filePath))
        {
            await file.CopyToAsync(stream);
        }

        var proof = new DeliveryProof
        {
            ProofId = Guid.NewGuid(),
            ShipmentId = shipmentId,
            FilePath = filePath,
            SignerName = signerName.Trim(),
            Notes = notes?.Trim(),
            Timestamp = DateTime.UtcNow
        };

        await repository.AddDeliveryProofAsync(proof);
        await repository.SaveChangesAsync();
        return new
        {
            proof.ProofId,
            proof.ShipmentId,
            proof.SignerName,
            proof.Notes,
            proof.Timestamp
        };
    }

    public async Task<object?> GetDeliveryProofAsync(Guid shipmentId, Guid requesterId, bool isAdmin)
    {
        if (!isAdmin)
        {
            var customerDocs = await repository.GetDocumentsByShipmentAndCustomerPagedAsync(shipmentId, requesterId, 1, 1);
            if (customerDocs.Count == 0) return new List<object>();
        }

        var items = await repository.GetDeliveryProofsByShipmentAsync(shipmentId);
        return items.Select(x => new
        {
            x.ProofId,
            x.ShipmentId,
            x.SignerName,
            x.Notes,
            x.Timestamp
        }).ToList();
    }
}