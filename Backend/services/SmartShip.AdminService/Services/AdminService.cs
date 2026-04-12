using SmartShip.AdminService.DTOs;
using SmartShip.AdminService.Models;
using SmartShip.AdminService.Repositories;

namespace SmartShip.AdminService.Services;

public class AdminService(IAdminRepository repository, ILogger<AdminService> logger) : IAdminService
{
    // ── Dashboard & Statistics ──────────────────────────────

    public async Task<object> GetDashboardAsync()
    {
        var totalExceptions = await repository.GetExceptionCountAsync();
        var openExceptions = await repository.GetExceptionCountByStatusAsync("Open");
        var resolvedExceptions = await repository.GetExceptionCountByStatusAsync("Resolved");
        var activeHubs = await repository.GetHubCountAsync();
        var totalLocations = await repository.GetTotalLocationCountAsync();

        return new
        {
            TotalExceptions = totalExceptions,
            OpenExceptions = openExceptions,
            ResolvedExceptions = resolvedExceptions,
            ActiveHubs = activeHubs,
            TotalLocations = totalLocations,
            GeneratedAt = DateTime.UtcNow
        };
    }

    public async Task<object> GetStatisticsAsync()
    {
        var totalHubs = await repository.GetTotalHubCountAsync();
        var activeHubs = await repository.GetHubCountAsync();
        var totalLocations = await repository.GetTotalLocationCountAsync();
        var totalExceptions = await repository.GetExceptionCountAsync();
        var openExceptions = await repository.GetExceptionCountByStatusAsync("Open");

        return new
        {
            TotalHubs = totalHubs,
            ActiveHubs = activeHubs,
            TotalLocations = totalLocations,
            TotalExceptions = totalExceptions,
            OpenExceptions = openExceptions,
            GeneratedAt = DateTime.UtcNow
        };
    }

    public Task<object> LoggingDemoAsync(bool fail)
    {
        logger.LogInformation("Logging demo endpoint called with fail={Fail}", fail);

        if (fail)
        {
            logger.LogError("Logging demo: simulated error");
            throw new InvalidOperationException("Simulated error for logging demo.");
        }

        logger.LogInformation("Logging demo: success");
        return Task.FromResult<object>(new
        {
            Message = "Logging demo executed successfully.",
            Timestamp = DateTime.UtcNow
        });
    }

    // ── Hubs ────────────────────────────────────────────────

    public async Task<object> CreateHubAsync(CreateHubDto request)
    {
        var hub = new Hub
        {
            HubId = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Address = request.Address.Trim(),
            ContactNumber = request.ContactNumber.Trim(),
            ManagerName = request.ManagerName.Trim(),
            Email = request.Email.Trim(),
            CreatedAt = DateTime.UtcNow,
            IsActive = request.IsActive
        };

        await repository.AddHubAsync(hub);
        await repository.SaveChangesAsync();
        return MapHub(hub);
    }

    public async Task<object> GetHubsPagedAsync(int pageNumber, int pageSize)
    {
        var totalCount = await repository.GetTotalHubCountAsync();
        var items = await repository.GetHubsPagedAsync(pageNumber, pageSize);

        return new
        {
            Items = items.Select(MapHub).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<object?> GetHubAsync(Guid id)
    {
        var hub = await repository.GetHubAsync(id);
        return hub is null ? null : MapHub(hub);
    }

    public async Task<object?> UpdateHubAsync(Guid id, UpdateHubDto request)
    {
        var hub = await repository.GetHubAsync(id);
        if (hub is null) return null;

        if (!string.IsNullOrWhiteSpace(request.Name)) hub.Name = request.Name.Trim();
        if (!string.IsNullOrWhiteSpace(request.Address)) hub.Address = request.Address.Trim();
        if (!string.IsNullOrWhiteSpace(request.ContactNumber)) hub.ContactNumber = request.ContactNumber.Trim();
        if (!string.IsNullOrWhiteSpace(request.ManagerName)) hub.ManagerName = request.ManagerName.Trim();
        if (!string.IsNullOrWhiteSpace(request.Email)) hub.Email = request.Email.Trim();
        if (request.IsActive.HasValue) hub.IsActive = request.IsActive.Value;

        await repository.SaveChangesAsync();
        return MapHub(hub);
    }

    public async Task<bool> DeleteHubAsync(Guid id)
    {
        var hub = await repository.GetHubAsync(id);
        if (hub is null) return false;
        await repository.DeleteHubAsync(hub);
        await repository.SaveChangesAsync();
        return true;
    }

    // ── Locations ───────────────────────────────────────────

    public async Task<object> GetLocationsPagedAsync(int pageNumber, int pageSize)
    {
        var totalCount = await repository.GetTotalLocationCountAsync();
        var items = await repository.GetAllLocationsPagedAsync(pageNumber, pageSize);

        return new
        {
            Items = items.Select(x => new
            {
                x.LocationId,
                x.HubId,
                x.Name,
                x.ZipCode,
                x.IsActive
            }).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<object?> CreateLocationAsync(CreateServiceLocationDto request)
    {
        if (!await repository.HubExistsAsync(request.HubId)) return null;

        var location = new ServiceLocation
        {
            LocationId = Guid.NewGuid(),
            HubId = request.HubId,
            Name = request.Name.Trim(),
            ZipCode = request.ZipCode.Trim(),
            IsActive = request.IsActive
        };

        await repository.AddLocationAsync(location);
        await repository.SaveChangesAsync();
        return new
        {
            location.LocationId,
            location.HubId,
            location.Name,
            location.ZipCode,
            location.IsActive
        };
    }

    public async Task<object?> UpdateLocationAsync(Guid id, UpdateServiceLocationDto request)
    {
        var loc = await repository.GetLocationAsync(id);
        if (loc is null) return null;

        if (request.HubId != Guid.Empty && await repository.HubExistsAsync(request.HubId))
            loc.HubId = request.HubId;

        loc.Name = request.Name.Trim();
        loc.ZipCode = request.ZipCode.Trim();
        loc.IsActive = request.IsActive;

        await repository.SaveChangesAsync();
        return new
        {
            loc.LocationId,
            loc.HubId,
            loc.Name,
            loc.ZipCode,
            loc.IsActive
        };
    }

    public async Task<bool> DeleteLocationAsync(Guid id)
    {
        var loc = await repository.GetLocationAsync(id);
        if (loc is null) return false;
        await repository.DeleteLocationAsync(loc);
        await repository.SaveChangesAsync();
        return true;
    }

    // ── Exceptions ──────────────────────────────────────────

    public async Task<object> GetExceptionsPagedAsync(int pageNumber, int pageSize)
    {
        var totalCount = await repository.GetExceptionCountAsync();
        var items = await repository.GetExceptionsPagedAsync(pageNumber, pageSize);

        return new
        {
            Items = items.Select(x => new
            {
                x.ExceptionId,
                x.ShipmentId,
                x.ExceptionType,
                x.Description,
                x.Status,
                x.CreatedAt,
                x.ResolvedAt
            }).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    // ── Shipments (proxy stubs — data comes from ShipmentService via gateway) ──

    public Task<object?> ResolveShipmentAsync(Guid id, ResolveShipmentDto request)
    {
        return Task.FromResult<object?>(new
        {
            ShipmentId = id,
            Status = "Resolved",
            request.ResolutionNotes,
            ResolvedAt = DateTime.UtcNow
        });
    }

    public Task<object?> DelayShipmentAsync(Guid id, DelayShipmentDto request)
    {
        return Task.FromResult<object?>(new
        {
            ShipmentId = id,
            Status = "Delayed",
            request.Reason,
            UpdatedAt = DateTime.UtcNow
        });
    }

    public Task<object?> ReturnShipmentAsync(Guid id, ReturnShipmentDto request)
    {
        return Task.FromResult<object?>(new
        {
            ShipmentId = id,
            Status = "Returned",
            request.Reason,
            UpdatedAt = DateTime.UtcNow
        });
    }

    public Task<object> GetShipmentsPagedAsync(int pageNumber, int pageSize)
    {
        return Task.FromResult<object>(new
        {
            Items = new List<object>(),
            TotalCount = 0,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = 0,
            Message = "Shipment data is served via the ShipmentService. Use the gateway to access shipment details."
        });
    }

    public Task<object?> GetShipmentAsync(Guid id)
    {
        return Task.FromResult<object?>(new
        {
            ShipmentId = id,
            Message = "Shipment data is served via the ShipmentService. Use the gateway to access shipment details."
        });
    }

    public Task<object> GetShipmentsByHubPagedAsync(Guid hubId, int pageNumber, int pageSize)
    {
        return Task.FromResult<object>(new
        {
            HubId = hubId,
            Items = new List<object>(),
            TotalCount = 0,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = 0,
            Message = "Shipment data is served via the ShipmentService. Use the gateway to access shipment details."
        });
    }

    // ── Reports ─────────────────────────────────────────────

    public async Task<object> GetReportsAsync()
    {
        var exceptions = await repository.GetAllExceptionsAsync();

        var byType = exceptions.GroupBy(x => x.ExceptionType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        var byStatus = exceptions.GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToList();

        var recentExceptions = exceptions.Take(10).Select(x => new
        {
            x.ExceptionId,
            x.ShipmentId,
            x.ExceptionType,
            x.Status,
            x.CreatedAt,
            x.ResolvedAt
        }).ToList();

        return new
        {
            ExceptionsByType = byType,
            ExceptionsByStatus = byStatus,
            RecentExceptions = recentExceptions,
            GeneratedAt = DateTime.UtcNow
        };
    }

    public Task<object> GetShipmentPerformanceReportAsync()
    {
        return Task.FromResult<object>(new
        {
            Report = "Shipment Performance",
            Message = "Aggregated from ShipmentService data.",
            GeneratedAt = DateTime.UtcNow
        });
    }

    public Task<object> GetDeliverySlaReportAsync()
    {
        return Task.FromResult<object>(new
        {
            Report = "Delivery SLA",
            Message = "Aggregated from TrackingService and ShipmentService data.",
            GeneratedAt = DateTime.UtcNow
        });
    }

    public Task<object> GetRevenueReportAsync()
    {
        return Task.FromResult<object>(new
        {
            Report = "Revenue",
            Message = "Aggregated from ShipmentService data.",
            GeneratedAt = DateTime.UtcNow
        });
    }

    public async Task<object> GetHubPerformanceReportAsync()
    {
        var hubs = await repository.GetHubsAsync();

        return new
        {
            Report = "Hub Performance",
            Hubs = hubs.Select(h => new
            {
                h.HubId,
                h.Name,
                h.IsActive,
                LocationCount = h.ServiceLocations.Count
            }).ToList(),
            GeneratedAt = DateTime.UtcNow
        };
    }

    // ── Helpers ─────────────────────────────────────────────

    private static object MapHub(Hub hub) => new
    {
        hub.HubId,
        hub.Name,
        hub.Address,
        hub.ContactNumber,
        hub.ManagerName,
        hub.Email,
        hub.CreatedAt,
        hub.IsActive,
        ServiceLocations = hub.ServiceLocations.Select(l => new
        {
            l.LocationId,
            l.Name,
            l.ZipCode,
            l.IsActive
        }).ToList()
    };
}