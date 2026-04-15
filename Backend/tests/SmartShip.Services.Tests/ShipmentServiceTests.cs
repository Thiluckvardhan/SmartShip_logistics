using Microsoft.Extensions.Logging;
using Moq;
using SmartShip.Core.Email;
using SmartShip.ShipmentService.Models;
using SmartShip.ShipmentService.Repositories;
using SmartShip.ShipmentService.Services;
using ShipmentServiceImpl = SmartShip.ShipmentService.Services.ShipmentService;
using Xunit;

namespace SmartShip.Services.Tests;

public class ShipmentServiceTests
{
    [Fact]
    public async Task UpdateShipmentStatusAsync_WhenTransitionIsNotAllowed_ReturnsError()
    {
        var repository = new Mock<IShipmentRepository>();
        var shipment = new Shipment
        {
            ShipmentId = Guid.NewGuid(),
            TrackingNumber = "SS-1234",
            Status = ShipmentStatus.Draft,
            CustomerId = Guid.NewGuid()
        };

        repository
            .Setup(r => r.GetShipmentAsync(shipment.ShipmentId))
            .ReturnsAsync(shipment);

        var service = CreateShipmentService(repository.Object);

        var result = await service.UpdateShipmentStatusAsync(shipment.ShipmentId, ShipmentStatus.Delivered);

        Assert.False(result.Ok);
        Assert.Contains("Cannot transition", result.Message);
        Assert.Null(result.Data);

        repository.Verify(r => r.SaveChangesAsync(), Times.Never);
        repository.Verify(r => r.AddOutboxMessageAsync(It.IsAny<OutboxMessage>()), Times.Never);
    }

    [Fact]
    public async Task BookShipmentAsync_WhenRequesterIsNotOwner_ReturnsError()
    {
        var repository = new Mock<IShipmentRepository>();
        var shipment = new Shipment
        {
            ShipmentId = Guid.NewGuid(),
            TrackingNumber = "SS-1234",
            Status = ShipmentStatus.Draft,
            CustomerId = Guid.NewGuid()
        };

        repository
            .Setup(r => r.GetShipmentAsync(shipment.ShipmentId))
            .ReturnsAsync(shipment);

        var service = CreateShipmentService(repository.Object);

        var result = await service.BookShipmentAsync(shipment.ShipmentId, Guid.NewGuid());

        Assert.False(result.Ok);
        Assert.Equal("You can book only your own shipments.", result.Message);
        Assert.Null(result.Data);
        repository.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task ReportShipmentIssueAsync_WhenDamagedProductBeforeDelivery_ReturnsValidationError()
    {
        var repository = new Mock<IShipmentRepository>();
        var customerId = Guid.NewGuid();
        var shipment = new Shipment
        {
            ShipmentId = Guid.NewGuid(),
            TrackingNumber = "SS-1234",
            Status = ShipmentStatus.InTransit,
            CustomerId = customerId
        };

        repository
            .Setup(r => r.GetShipmentAsync(shipment.ShipmentId))
            .ReturnsAsync(shipment);

        var service = CreateShipmentService(repository.Object);

        var result = await service.ReportShipmentIssueAsync(
            shipment.ShipmentId,
            customerId,
            "Damaged Product",
            "Package looked crushed");

        Assert.False(result.Ok);
        Assert.Equal("Damaged Product issues can be reported only after the shipment is delivered.", result.Message);
        Assert.Null(result.Data);

        repository.Verify(r => r.AddOutboxMessageAsync(It.IsAny<OutboxMessage>()), Times.Never);
        repository.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    private static ShipmentServiceImpl CreateShipmentService(IShipmentRepository repository)
    {
        var emailService = Mock.Of<IEmailService>();
        var serviceTokenGenerator = Mock.Of<IServiceTokenGenerator>();
        var logger = Mock.Of<ILogger<ShipmentServiceImpl>>();

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(new StubHttpMessageHandler()));

        return new ShipmentServiceImpl(repository, httpClientFactory.Object, emailService, serviceTokenGenerator, logger);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            });
        }
    }
}
