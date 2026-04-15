using SmartShip.ShipmentService.DTOs;
using SmartShip.ShipmentService.Models;
using SmartShip.ShipmentService.Repositories;
using SmartShip.Contracts.Events;
using SmartShip.Core.Email;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using System.Text.Json;

namespace SmartShip.ShipmentService.Services;

public class ShipmentService(
    IShipmentRepository repository,
    IHttpClientFactory httpClientFactory,
    IEmailService emailService,
    IServiceTokenGenerator serviceTokenGenerator,
    ILogger<ShipmentService> logger) : IShipmentService
{
    private const decimal DomesticRateMultiplier = 10.0m;
    private readonly HttpClient _identityClient = httpClientFactory.CreateClient("IdentityService");

    private static readonly Dictionary<ShipmentStatus, ShipmentStatus[]> AllowedTransitions = new()
    {
        [ShipmentStatus.Draft] = [ShipmentStatus.Booked],
        [ShipmentStatus.Booked] = [ShipmentStatus.PickedUp, ShipmentStatus.Delayed, ShipmentStatus.Failed],
        [ShipmentStatus.PickedUp] = [ShipmentStatus.InTransit, ShipmentStatus.Delayed, ShipmentStatus.Failed],
        [ShipmentStatus.InTransit] = [ShipmentStatus.InTransit, ShipmentStatus.OutForDelivery, ShipmentStatus.Delayed, ShipmentStatus.Failed],
        [ShipmentStatus.OutForDelivery] = [ShipmentStatus.Delivered, ShipmentStatus.Delayed, ShipmentStatus.Failed, ShipmentStatus.Returned],
        [ShipmentStatus.Delayed] = [ShipmentStatus.InTransit, ShipmentStatus.OutForDelivery, ShipmentStatus.Failed, ShipmentStatus.Returned],
        [ShipmentStatus.Failed] = [ShipmentStatus.Returned],
        [ShipmentStatus.Returned] = [],
        [ShipmentStatus.Delivered] = []
    };

    public async Task<object?> CreateShipmentAsync(CreateShipmentDto request, Guid customerId)
    {
        var items = request.Items ?? [];
        var totalWeight = items.Sum(x => x.Weight * (x.Quantity <= 0 ? 1 : x.Quantity));
        var rate = CalculateRate(totalWeight);

        var sender = new Address
        {
            AddressId = Guid.NewGuid(),
            Name = request.SenderAddress.Name.Trim(),
            Phone = request.SenderAddress.Phone.Trim(),
            Street = request.SenderAddress.Street.Trim(),
            City = request.SenderAddress.City.Trim(),
            State = request.SenderAddress.State.Trim(),
            Country = request.SenderAddress.Country.Trim(),
            PostalCode = request.SenderAddress.Pincode.Trim()
        };

        var receiver = new Address
        {
            AddressId = Guid.NewGuid(),
            Name = request.ReceiverAddress.Name.Trim(),
            Phone = request.ReceiverAddress.Phone.Trim(),
            Street = request.ReceiverAddress.Street.Trim(),
            City = request.ReceiverAddress.City.Trim(),
            State = request.ReceiverAddress.State.Trim(),
            Country = request.ReceiverAddress.Country.Trim(),
            PostalCode = request.ReceiverAddress.Pincode.Trim()
        };

        await repository.AddAddressAsync(sender);
        await repository.AddAddressAsync(receiver);

        var shipment = new Shipment
        {
            ShipmentId = Guid.NewGuid(),
            TrackingNumber = GenerateTrackingNumber(),
            CustomerId = customerId,
            SenderAddressId = sender.AddressId,
            ReceiverAddressId = receiver.AddressId,
            TotalWeight = totalWeight,
            EstimatedRate = rate,
            Status = ShipmentStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await repository.AddShipmentAsync(shipment);

        foreach (var item in items)
        {
            var quantity = item.Quantity <= 0 ? 1 : item.Quantity;

            await repository.AddPackageAsync(new Package
            {
                PackageId = Guid.NewGuid(),
                ShipmentId = shipment.ShipmentId,
                ItemName = item.ItemName.Trim(),
                Quantity = quantity,
                Weight = item.Weight,
                Description = item.Description?.Trim() ?? item.ItemName.Trim()
            });
        }

        await QueueOutboxEventAsync(new ShipmentCreatedEvent
        {
            ShipmentId = shipment.ShipmentId,
            TrackingNumber = shipment.TrackingNumber,
            CustomerId = shipment.CustomerId,
            Weight = shipment.TotalWeight
        });

        await repository.SaveChangesAsync();

        return new
        {
            shipment.ShipmentId,
            shipment.TrackingNumber,
            shipment.CustomerId,
            shipment.TotalWeight,
            shipment.EstimatedRate,
            shipment.Status,
            shipment.CreatedAt,
            SenderAddress = new { sender.Name, sender.Phone, sender.Street, sender.City, sender.State, sender.Country, sender.PostalCode },
            ReceiverAddress = new { receiver.Name, receiver.Phone, receiver.Street, receiver.City, receiver.State, receiver.Country, receiver.PostalCode },
            Packages = items.Select(i => new { i.ItemName, i.Quantity, i.Weight }).ToList()
        };
    }

    public async Task<object?> GetShipmentAsync(Guid id, Guid requesterId, bool isAdmin)
    {
        var s = await repository.GetShipmentAsync(id);
        if (s is null) return null;
        if (!CanAccessShipment(s, requesterId, isAdmin)) return null;

        return MapShipment(s);
    }

    public async Task<object?> GetShipmentByTrackingNumberAsync(string trackingNumber, Guid requesterId, bool isAdmin)
    {
        var s = await repository.GetShipmentByTrackingNumberAsync(trackingNumber);
        if (s is null) return null;
        if (!CanAccessShipment(s, requesterId, isAdmin)) return null;

        return MapShipment(s);
    }

    public async Task<List<object>> GetMyShipmentsAsync(Guid customerId)
    {
        var items = await repository.GetShipmentsByCustomerAsync(customerId);
        return items.Select(MapShipment).ToList();
    }

    public async Task<List<object>> GetAllShipmentsAsync()
    {
        var items = await repository.GetAllShipmentsAsync();
        return items
            .Where(x => x.Status != ShipmentStatus.Draft)
            .Select(MapShipment)
            .ToList();
    }

    public async Task<(bool Ok, string? Message, object? Data)> BookShipmentAsync(Guid id, Guid customerId)
    {
        var shipment = await repository.GetShipmentAsync(id);
        if (shipment is null) return (false, "Shipment not found.", null);

        if (shipment.CustomerId != customerId)
            return (false, "You can book only your own shipments.", null);

        return await UpdateShipmentStatusAsync(id, ShipmentStatus.Booked);
    }

    public async Task<object?> UpdateShipmentAsync(Guid id, UpdateShipmentDto request, Guid requesterId, bool isAdmin)
    {
        var shipment = await repository.GetShipmentAsync(id);
        if (shipment is null) return null;
        if (!CanAccessShipment(shipment, requesterId, isAdmin)) return null;

        var items = request.Items ?? [];
        if (items.Count == 0) return MapShipment(shipment);

        decimal addedWeight = 0;

        foreach (var item in items)
        {
            var quantity = item.Quantity <= 0 ? 1 : item.Quantity;
            var itemName = string.IsNullOrWhiteSpace(item.ItemName)
                ? (item.Description?.Trim() ?? "Package")
                : item.ItemName.Trim();

            await repository.AddPackageAsync(new Package
            {
                PackageId = Guid.NewGuid(),
                ShipmentId = shipment.ShipmentId,
                ItemName = itemName,
                Quantity = quantity,
                Weight = item.Weight,
                Description = item.Description?.Trim()
            });

            addedWeight += item.Weight * quantity;
        }

        shipment.TotalWeight += addedWeight;
        shipment.EstimatedRate = CalculateRate(shipment.TotalWeight);
        shipment.UpdatedAt = DateTime.UtcNow;

        await repository.SaveChangesAsync();
        return MapShipment(shipment);
    }

    public async Task<(bool Ok, string? Message, object? Data)> UpdateShipmentStatusWithJourneyAsync(Guid id, AdminUpdateShipmentJourneyDto request)
    {
        var rawStatus = request.Status.Trim();
        var normalizedStatus = rawStatus.Replace("-", string.Empty).Replace(" ", string.Empty);
        if (!Enum.TryParse<ShipmentStatus>(normalizedStatus, true, out var parsedStatus))
        {
            return (false, $"Unsupported status '{rawStatus}'.", null);
        }

        var reasonRequired = parsedStatus is ShipmentStatus.Delayed or ShipmentStatus.Returned or ShipmentStatus.Failed;
        if (reasonRequired && string.IsNullOrWhiteSpace(request.Reason))
        {
            return (false, $"Reason is required when setting status to '{parsedStatus}'.", null);
        }

        var hub = request.HubName?.Trim();
        var location = request.ServiceLocationName?.Trim();
        var locationLabel = BuildLocationLabel(hub, location);
        var remarks = BuildStatusRemarks(parsedStatus, request.Reason?.Trim(), locationLabel);

        return await UpdateShipmentStatusAsync(id, parsedStatus, locationLabel, remarks);
    }

    public async Task<(bool Ok, string? Message, object? Data)> UpdateShipmentStatusAsync(Guid id, ShipmentStatus newStatus, string? location = null, string? remarks = null)
    {
        var shipment = await repository.GetShipmentAsync(id);
        if (shipment is null) return (false, "Shipment not found.", null);

        if (AllowedTransitions.TryGetValue(shipment.Status, out var allowed))
        {
            if (!allowed.Contains(newStatus))
                return (false, $"Cannot transition from '{shipment.Status}' to '{newStatus}'. Allowed: {string.Join(", ", allowed)}", null);
        }

        shipment.Status = newStatus;
        shipment.UpdatedAt = DateTime.UtcNow;
        await repository.SaveChangesAsync();

        if (newStatus is ShipmentStatus.PickedUp or ShipmentStatus.OutForDelivery or ShipmentStatus.Delivered)
        {
            await SendShipmentStatusEmailAsync(shipment, newStatus);
        }

        if (newStatus is ShipmentStatus.Booked or ShipmentStatus.PickedUp)
        {
            await QueueOutboxEventAsync(new ShipmentBookedEvent
            {
                ShipmentId = shipment.ShipmentId,
                TrackingNumber = shipment.TrackingNumber,
                HubId = newStatus.ToString().ToUpperInvariant()
            });
        }

        if (newStatus == ShipmentStatus.Delivered)
        {
            await QueueOutboxEventAsync(new ShipmentDeliveredEvent
            {
                ShipmentId = shipment.ShipmentId,
                TrackingNumber = shipment.TrackingNumber
            });
        }

        if (newStatus is ShipmentStatus.PickedUp or ShipmentStatus.InTransit or ShipmentStatus.OutForDelivery or ShipmentStatus.Delayed or ShipmentStatus.Failed or ShipmentStatus.Returned)
        {
            await QueueOutboxEventAsync(new TrackingUpdatedEvent
            {
                ShipmentId = shipment.ShipmentId,
                TrackingNumber = shipment.TrackingNumber,
                Status = newStatus.ToString().ToUpperInvariant(),
                Location = string.IsNullOrWhiteSpace(location) ? "System" : location.Trim(),
                Remarks = string.IsNullOrWhiteSpace(remarks) ? $"Shipment status changed to {newStatus}." : remarks.Trim()
            });
        }

        return (true, null, MapShipment(shipment));
    }

    public async Task<(bool Ok, string? Message)> DeleteShipmentAsync(Guid id, Guid requesterId, bool isAdmin)
    {
        var shipment = await repository.GetShipmentAsync(id);
        if (shipment is null) return (false, "Shipment not found.");

        if (!isAdmin)
        {
            if (shipment.CustomerId != requesterId)
                return (false, "You can delete only your own shipments.");

            if (shipment.Status is not ShipmentStatus.Draft and not ShipmentStatus.Booked)
                return (false, "Cannot delete shipment now because the shipment is beyond Booked.");
        }

        var senderAddressId = shipment.SenderAddressId;
        var receiverAddressId = shipment.ReceiverAddressId;

        await repository.DeleteShipmentAsync(shipment);

        if (!await repository.IsAddressUsedByOtherShipmentsAsync(senderAddressId, shipment.ShipmentId))
        {
            var senderAddress = await repository.GetAddressAsync(senderAddressId);
            if (senderAddress is not null)
                await repository.DeleteAddressAsync(senderAddress);
        }

        if (receiverAddressId != senderAddressId && !await repository.IsAddressUsedByOtherShipmentsAsync(receiverAddressId, shipment.ShipmentId))
        {
            var receiverAddress = await repository.GetAddressAsync(receiverAddressId);
            if (receiverAddress is not null)
                await repository.DeleteAddressAsync(receiverAddress);
        }

        await repository.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Message, object? Data)> ReportShipmentIssueAsync(Guid id, Guid customerId, string issueType, string description)
    {
        var shipment = await repository.GetShipmentAsync(id);
        if (shipment is null) return (false, "Shipment not found.", null);
        if (shipment.CustomerId != customerId)
            return (false, "You can report issues only for your own shipments.", null);

        var normalizedIssueType = issueType.Trim();
        if (shipment.Status == ShipmentStatus.Draft)
        {
            return (false, "Book your shipment before reporting an issue to support.", null);
        }

        if (normalizedIssueType.Equals("Damaged Product", StringComparison.OrdinalIgnoreCase) && shipment.Status != ShipmentStatus.Delivered)
        {
            return (false, "Damaged Product issues can be reported only after the shipment is delivered.", null);
        }

        await QueueOutboxEventAsync(new ShipmentIssueReportedEvent
        {
            ShipmentId = shipment.ShipmentId,
            TrackingNumber = shipment.TrackingNumber,
            CustomerId = customerId,
            IssueType = normalizedIssueType,
            Description = description.Trim()
        });

        await repository.SaveChangesAsync();

        return (true, null, new
        {
            ShipmentId = shipment.ShipmentId,
            TrackingNumber = shipment.TrackingNumber,
            ExceptionType = normalizedIssueType,
            Status = "Pending",
            Description = description.Trim(),
            Message = "Issue submitted successfully and queued for admin review."
        });
    }

    public async Task<(bool Ok, string? Message, object? Data)> GetShipmentIssuesAsync(Guid id, Guid requesterId, bool isAdmin)
    {
        var shipment = await repository.GetShipmentAsync(id);
        if (shipment is null) return (false, "Shipment not found.", null);

        if (!isAdmin && shipment.CustomerId != requesterId)
        {
            return (false, "You can view only your own shipment issues.", null);
        }

        var adminClient = httpClientFactory.CreateClient("AdminService");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/admin/exceptions/shipment/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", serviceTokenGenerator.GenerateToken());

        try
        {
            using var response = await adminClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Admin exception lookup failed for shipment {ShipmentId}: {StatusCode}",
                    id,
                    response.StatusCode);

                return (true, null, new
                {
                    Items = Array.Empty<object>(),
                    TotalCount = 0,
                    ShipmentId = id
                });
            }

            var payload = await response.Content.ReadFromJsonAsync<object>() ?? new
            {
                Items = Array.Empty<object>(),
                TotalCount = 0,
                ShipmentId = id
            };
            return (true, null, payload);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Admin exception lookup request failed for shipment {ShipmentId}", id);
            return (true, null, new
            {
                Items = Array.Empty<object>(),
                TotalCount = 0,
                ShipmentId = id
            });
        }
    }

    public Task<object> CalculateRateAsync(CalculateRateDto request)
    {
        var rate = CalculateRate(request.TotalWeight);
        return Task.FromResult<object>(new
        {
            request.TotalWeight,
            EstimatedRate = rate,
            Currency = "INR"
        });
    }

    public async Task<object> CreateAddressAsync(CreateAddressDto request)
    {
        var address = new Address
        {
            AddressId = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Phone = request.Phone.Trim(),
            Street = request.Street.Trim(),
            City = request.City.Trim(),
            State = request.State.Trim(),
            Country = request.Country.Trim(),
            PostalCode = request.Pincode.Trim()
        };

        await repository.AddAddressAsync(address);
        await repository.SaveChangesAsync();
        return address;
    }

    public Task<object?> GetAddressAsync(Guid id) => repository.GetAddressAsync(id).ContinueWith(t => (object?)t.Result);

    public async Task<object?> CreatePackageAsync(Guid shipmentId, CreatePackageDto request, Guid requesterId, bool isAdmin)
    {
        var shipment = await repository.GetShipmentAsync(shipmentId);
        if (shipment is null) return null;
        if (!CanAccessShipment(shipment, requesterId, isAdmin)) return null;

        var quantity = request.Quantity.GetValueOrDefault(1) <= 0 ? 1 : request.Quantity.GetValueOrDefault(1);

        var package = new Package
        {
            PackageId = Guid.NewGuid(),
            ShipmentId = shipmentId,
            ItemName = string.IsNullOrWhiteSpace(request.ItemName)
                ? (request.Description?.Trim() ?? "Package")
                : request.ItemName.Trim(),
            Quantity = quantity,
            Weight = request.Weight,
            Description = request.Description?.Trim()
        };

        await repository.AddPackageAsync(package);

        shipment.TotalWeight += package.Weight * quantity;
        shipment.EstimatedRate = CalculateRate(shipment.TotalWeight);
        shipment.UpdatedAt = DateTime.UtcNow;

        await repository.SaveChangesAsync();
        return package;
    }

    public async Task<object?> UpdatePackageAsync(Guid shipmentId, Guid packageId, UpdatePackageDto request, Guid requesterId, bool isAdmin)
    {
        var shipment = await repository.GetShipmentAsync(shipmentId);
        if (shipment is null) return null;
        if (!CanAccessShipment(shipment, requesterId, isAdmin)) return null;

        var package = await repository.GetPackageAsync(shipmentId, packageId);
        if (package is null) return null;

        var oldTotalWeight = package.Weight * package.Quantity;

        if (!string.IsNullOrWhiteSpace(request.ItemName))
            package.ItemName = request.ItemName.Trim();

        if (request.Quantity.HasValue)
            package.Quantity = request.Quantity.Value <= 0 ? 1 : request.Quantity.Value;

        if (request.Weight.HasValue)
            package.Weight = request.Weight.Value;

        if (request.Description is not null)
            package.Description = request.Description.Trim();

        var newTotalWeight = package.Weight * package.Quantity;
        shipment.TotalWeight += (newTotalWeight - oldTotalWeight);
        shipment.EstimatedRate = CalculateRate(shipment.TotalWeight);
        shipment.UpdatedAt = DateTime.UtcNow;

        await repository.SaveChangesAsync();
        return package;
    }

    public async Task<(bool Ok, string? Message)> DeletePackageAsync(Guid shipmentId, Guid packageId, Guid requesterId, bool isAdmin)
    {
        var shipment = await repository.GetShipmentAsync(shipmentId);
        if (shipment is null) return (false, "Shipment not found.");
        if (!CanAccessShipment(shipment, requesterId, isAdmin)) return (false, "You can modify only your own shipments.");

        var package = await repository.GetPackageAsync(shipmentId, packageId);
        if (package is null) return (false, "Package not found.");

        shipment.TotalWeight -= package.Weight * package.Quantity;
        if (shipment.TotalWeight < 0) shipment.TotalWeight = 0;
        shipment.EstimatedRate = CalculateRate(shipment.TotalWeight);
        shipment.UpdatedAt = DateTime.UtcNow;

        await repository.DeletePackageAsync(package);
        await repository.SaveChangesAsync();
        return (true, null);
    }

    public async Task<List<object>?> GetPackagesByShipmentAsync(Guid shipmentId, Guid requesterId, bool isAdmin)
    {
        var shipment = await repository.GetShipmentAsync(shipmentId);
        if (shipment is null) return null;
        if (!CanAccessShipment(shipment, requesterId, isAdmin)) return null;

        var items = await repository.GetPackagesByShipmentAsync(shipmentId);
        return items.Select(x => (object)x).ToList();
    }

    public async Task<object?> CreatePickupAsync(CreatePickupDto request)
    {
        var shipment = await repository.GetShipmentAsync(request.ShipmentId);
        if (shipment is null) return null;

        var pickup = new PickupSchedule
        {
            PickupScheduleId = Guid.NewGuid(),
            ShipmentId = request.ShipmentId,
            PickupDate = request.PickupDate,
            Notes = request.Notes?.Trim()
        };

        await repository.AddPickupAsync(pickup);
        await repository.SaveChangesAsync();

        await SendPickupScheduledEmailAsync(shipment, pickup);
        return pickup;
    }

    public async Task<List<object>> GetAllPickupsAsync()
    {
        var items = await repository.GetAllPickupsAsync();
        return items.Select(x => (object)x).ToList();
    }

    public async Task<List<object>> GetPickupsAsync(Guid shipmentId)
    {
        var items = await repository.GetPickupsByShipmentAsync(shipmentId);
        return items.Select(x => (object)x).ToList();
    }

    public async Task<object?> UpdatePickupAsync(Guid shipmentId, UpdatePickupDto request)
    {
        var pickup = await repository.GetLatestPickupByShipmentAsync(shipmentId);
        if (pickup is null) return null;

        var shipment = await repository.GetShipmentAsync(shipmentId);
        if (shipment is null) return null;

        if (request.PickupDate.HasValue) pickup.PickupDate = request.PickupDate.Value;
        if (request.Notes is not null) pickup.Notes = request.Notes.Trim();

        await repository.SaveChangesAsync();

        await SendPickupUpdatedEmailAsync(shipment, pickup);
        return pickup;
    }

    public async Task<object> GetShipmentStatsAsync()
    {
        var total = await repository.GetShipmentCountAsync();
        var draft = await repository.GetShipmentCountByStatusAsync(ShipmentStatus.Draft);
        var booked = await repository.GetShipmentCountByStatusAsync(ShipmentStatus.Booked);
        var inTransit = await repository.GetShipmentCountByStatusAsync(ShipmentStatus.InTransit);
        var delivered = await repository.GetShipmentCountByStatusAsync(ShipmentStatus.Delivered);
        var delayed = await repository.GetShipmentCountByStatusAsync(ShipmentStatus.Delayed);
        var failed = await repository.GetShipmentCountByStatusAsync(ShipmentStatus.Failed);

        return new
        {
            TotalShipments = total,
            Draft = draft,
            Booked = booked,
            InTransit = inTransit,
            Delivered = delivered,
            Delayed = delayed,
            Failed = failed
        };
    }

    private static decimal CalculateRate(decimal totalWeight)
    {
        return Math.Round(totalWeight * DomesticRateMultiplier, 2);
    }

    private static string GenerateTrackingNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = Random.Shared.Next(1000, 9999);
        return $"SS-{timestamp}-{random}";
    }

    private static object MapShipment(Shipment s) => new
    {
        s.ShipmentId,
        s.TrackingNumber,
        s.CustomerId,
        s.TotalWeight,
        s.EstimatedRate,
        s.Status,
        s.CreatedAt,
        s.UpdatedAt,
        SenderAddress = s.SenderAddress is null ? null : new
        {
            s.SenderAddress.Name,
            s.SenderAddress.Phone,
            s.SenderAddress.Street,
            s.SenderAddress.City,
            s.SenderAddress.State,
            s.SenderAddress.Country,
            s.SenderAddress.PostalCode
        },
        ReceiverAddress = s.ReceiverAddress is null ? null : new
        {
            s.ReceiverAddress.Name,
            s.ReceiverAddress.Phone,
            s.ReceiverAddress.Street,
            s.ReceiverAddress.City,
            s.ReceiverAddress.State,
            s.ReceiverAddress.Country,
            s.ReceiverAddress.PostalCode
        },
        Packages = s.Packages.Select(p => new
        {
            p.PackageId,
            p.ItemName,
            p.Quantity,
            p.Weight,
            p.Description
        }).ToList(),
        PickupSchedules = s.PickupSchedules.Select(ps => new
        {
            ps.PickupScheduleId,
            ps.PickupDate,
            ps.Notes
        }).ToList()
    };

    private async Task SendPickupScheduledEmailAsync(Shipment shipment, PickupSchedule pickup)
    {
        try
        {
            var recipientEmail = await GetCustomerEmailAsync(shipment.CustomerId);
            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                logger.LogWarning("Pickup scheduled for shipment {ShipmentId} but customer email could not be resolved.", shipment.ShipmentId);
                return;
            }

            var sent = await emailService.SendPickupScheduledEmailAsync(
                recipientEmail,
                shipment.TrackingNumber,
                pickup.PickupDate,
                pickup.Notes);

            if (!sent)
            {
                logger.LogWarning("Pickup scheduled email could not be sent for shipment {ShipmentId} to {Email}.", shipment.ShipmentId, recipientEmail);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send pickup scheduled email for shipment {ShipmentId}.", shipment.ShipmentId);
        }
    }

    private async Task SendPickupUpdatedEmailAsync(Shipment shipment, PickupSchedule pickup)
    {
        try
        {
            var recipientEmail = await GetCustomerEmailAsync(shipment.CustomerId);
            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                logger.LogWarning("Pickup update email skipped for shipment {ShipmentId} because customer email could not be resolved.", shipment.ShipmentId);
                return;
            }

            var sent = await emailService.SendPickupUpdatedEmailAsync(
                recipientEmail,
                shipment.TrackingNumber,
                pickup.PickupDate,
                pickup.Notes);

            if (!sent)
            {
                logger.LogWarning("Pickup update email could not be sent for shipment {ShipmentId} to {Email}.", shipment.ShipmentId, recipientEmail);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send pickup update email for shipment {ShipmentId}.", shipment.ShipmentId);
        }
    }

    private async Task SendShipmentStatusEmailAsync(Shipment shipment, ShipmentStatus status)
    {
        try
        {
            var recipientEmail = await GetCustomerEmailAsync(shipment.CustomerId);
            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                logger.LogWarning("Status update email skipped for shipment {ShipmentId} because customer email could not be resolved.", shipment.ShipmentId);
                return;
            }

            var message = status switch
            {
                ShipmentStatus.PickedUp => "Your package has been picked up by our logistics partner.",
                ShipmentStatus.OutForDelivery => "Your package is out for delivery and will arrive soon.",
                ShipmentStatus.Delivered => "Your package has been delivered successfully.",
                _ => $"Your shipment status was updated to {status}."
            };

            var sent = await emailService.SendShipmentStatusEmailAsync(
                recipientEmail,
                shipment.TrackingNumber,
                status.ToString(),
                message);

            if (!sent)
            {
                logger.LogWarning("Status update email could not be sent for shipment {ShipmentId} to {Email}. Status: {Status}", shipment.ShipmentId, recipientEmail, status);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send status update email for shipment {ShipmentId}. Status: {Status}", shipment.ShipmentId, status);
        }
    }

    private async Task<string?> GetCustomerEmailAsync(Guid customerId)
    {
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/users/{customerId}");
                request.Version = HttpVersion.Version11;
                request.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", serviceTokenGenerator.GenerateToken());

                using var response = await _identityClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning(
                        "Identity lookup failed for customer {CustomerId} with status code {StatusCode}. Attempt {Attempt}/{MaxAttempts}.",
                        customerId,
                        response.StatusCode,
                        attempt,
                        maxAttempts);

                    if ((int)response.StatusCode >= 500 && attempt < maxAttempts)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt));
                        continue;
                    }

                    return null;
                }

                var user = await response.Content.ReadFromJsonAsync<JsonElement>();
                return user.TryGetProperty("email", out var email) ? email.GetString() :
                       user.TryGetProperty("Email", out var emailUpper) ? emailUpper.GetString() : null;
            }
            catch (HttpRequestException ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(
                    ex,
                    "Transient identity lookup error for customer {CustomerId}. Attempt {Attempt}/{MaxAttempts}.",
                    customerId,
                    attempt,
                    maxAttempts);
                await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt));
            }
            catch (TaskCanceledException ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(
                    ex,
                    "Identity lookup timeout for customer {CustomerId}. Attempt {Attempt}/{MaxAttempts}.",
                    customerId,
                    attempt,
                    maxAttempts);
                await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt));
            }
        }

        logger.LogWarning("Identity lookup exhausted retries for customer {CustomerId}.", customerId);
        return null;
    }

    private static bool CanAccessShipment(Shipment shipment, Guid requesterId, bool isAdmin)
    {
        if (isAdmin)
        {
            return shipment.Status != ShipmentStatus.Draft;
        }

        return shipment.CustomerId == requesterId;
    }

    private async Task QueueOutboxEventAsync(IntegrationEvent @event)
    {
        await repository.AddOutboxMessageAsync(new OutboxMessage
        {
            EventType = @event.GetType().AssemblyQualifiedName ?? @event.GetType().FullName ?? @event.GetType().Name,
            Payload = JsonSerializer.Serialize((object)@event, @event.GetType()),
            CreatedAt = DateTime.UtcNow,
            Status = "Pending",
            AttemptCount = 0
        });
    }

    private static string BuildLocationLabel(string? hubName, string? locationName)
    {
        var parts = new[] { locationName, hubName }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .ToArray();

        return parts.Length == 0 ? "System" : string.Join(" · ", parts);
    }

    private static string BuildStatusRemarks(ShipmentStatus status, string? reason, string locationLabel)
    {
        var movement = status switch
        {
            ShipmentStatus.PickedUp => "Shipment picked up by carrier.",
            ShipmentStatus.InTransit => $"Shipment is in transit via {locationLabel}.",
            ShipmentStatus.OutForDelivery => $"Shipment left {locationLabel} and is out for delivery.",
            ShipmentStatus.Delayed => $"Shipment delayed at {locationLabel}.",
            ShipmentStatus.Returned => $"Shipment returned from {locationLabel}.",
            ShipmentStatus.Failed => $"Shipment delivery failed at {locationLabel}.",
            ShipmentStatus.Delivered => "Shipment delivered.",
            _ => $"Shipment status changed to {status}."
        };

        if (string.IsNullOrWhiteSpace(reason))
        {
            return movement;
        }

        return $"{movement} Reason: {reason.Trim()}";
    }
}