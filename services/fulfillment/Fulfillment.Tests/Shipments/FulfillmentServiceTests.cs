using Fulfillment.Application.Ordering;
using Fulfillment.Application.Shipments;
using Fulfillment.Infrastructure.Persistence;
using Fulfillment.Infrastructure.Shipments;
using Microsoft.EntityFrameworkCore;

namespace Fulfillment.Tests.Shipments;

public sealed class FulfillmentServiceTests
{
    [Fact]
    public async Task CreateShipmentAsync_ShouldCreatePendingShipment()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, out _);

        var result = await service.CreateShipmentAsync(new CreateShipmentRequest(
            OrderId: Guid.NewGuid(),
            Carrier: "Manual",
            TrackingNumber: "TRACK-001"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Pending", result.Value.Status);
        Assert.Equal("Manual", result.Value.Carrier);
        Assert.Equal("TRACK-001", result.Value.TrackingNumber);

        var shipmentExists = await dbContext.Shipments
            .AnyAsync(shipment => shipment.Id == result.Value.Id);

        Assert.True(shipmentExists);
    }

    [Fact]
    public async Task CreateShipmentAsync_ShouldRejectMissingOrderId()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, out _);

        var result = await service.CreateShipmentAsync(new CreateShipmentRequest(
            OrderId: Guid.Empty,
            Carrier: "Manual",
            TrackingNumber: "TRACK-001"));

        Assert.False(result.IsSuccess);
        Assert.Equal("order_id_required", result.ErrorCode);
    }

    [Fact]
    public async Task GetShipmentsAsync_ShouldReturnShipments()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, out _);

        var createdShipment = await service.CreateShipmentAsync(new CreateShipmentRequest(
            OrderId: Guid.NewGuid(),
            Carrier: "Manual",
            TrackingNumber: "TRACK-001"));

        var shipments = await service.GetShipmentsAsync();

        Assert.Single(shipments);
        Assert.Equal(createdShipment.Value!.Id, shipments[0].Id);
    }

    [Fact]
    public async Task GetShipmentByIdAsync_ShouldReturnShipment()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, out _);

        var createdShipment = await service.CreateShipmentAsync(new CreateShipmentRequest(
            OrderId: Guid.NewGuid(),
            Carrier: "Manual",
            TrackingNumber: "TRACK-001"));

        var shipment = await service.GetShipmentByIdAsync(createdShipment.Value!.Id);

        Assert.NotNull(shipment);
        Assert.Equal(createdShipment.Value.Id, shipment.Id);
    }

    [Fact]
    public async Task ShipShipmentAsync_ShouldMarkShipmentAsShippedAndCallOrdering()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, out var orderingClient);

        var orderId = Guid.NewGuid();

        var createdShipment = await service.CreateShipmentAsync(new CreateShipmentRequest(
            OrderId: orderId,
            Carrier: null,
            TrackingNumber: null));

        var result = await service.ShipShipmentAsync(
            createdShipment.Value!.Id,
            new ShipShipmentRequest(
                Carrier: "Manual",
                TrackingNumber: "TRACK-001"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Shipped", result.Value.Status);
        Assert.Equal("Manual", result.Value.Carrier);
        Assert.Equal("TRACK-001", result.Value.TrackingNumber);
        Assert.NotNull(result.Value.ShippedAt);

        Assert.Single(orderingClient.MarkShippedRequests);
        Assert.Equal(orderId, orderingClient.MarkShippedRequests[0]);
    }

    [Fact]
    public async Task ShipShipmentAsync_ShouldRejectMissingShipment()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, out _);

        var result = await service.ShipShipmentAsync(
            Guid.NewGuid(),
            new ShipShipmentRequest("Manual", "TRACK-001"));

        Assert.False(result.IsSuccess);
        Assert.Equal("shipment_not_found", result.ErrorCode);
    }

    [Fact]
    public async Task ShipShipmentAsync_ShouldRejectAlreadyShippedShipment()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, out _);

        var createdShipment = await service.CreateShipmentAsync(new CreateShipmentRequest(
            OrderId: Guid.NewGuid(),
            Carrier: null,
            TrackingNumber: null));

        var firstResult = await service.ShipShipmentAsync(
            createdShipment.Value!.Id,
            new ShipShipmentRequest("Manual", "TRACK-001"));

        var secondResult = await service.ShipShipmentAsync(
            createdShipment.Value.Id,
            new ShipShipmentRequest("Manual", "TRACK-002"));

        Assert.True(firstResult.IsSuccess);
        Assert.False(secondResult.IsSuccess);
        Assert.Equal("shipment_cannot_be_shipped", secondResult.ErrorCode);
    }

    [Fact]
    public async Task ShipShipmentAsync_ShouldReject_WhenOrderingRejects()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, out var orderingClient);

        orderingClient.NextResult = OrderingClientResult.Failure(
            "order_cannot_be_marked_shipped",
            "Only paid orders can be marked as shipped.");

        var createdShipment = await service.CreateShipmentAsync(new CreateShipmentRequest(
            OrderId: Guid.NewGuid(),
            Carrier: null,
            TrackingNumber: null));

        var result = await service.ShipShipmentAsync(
            createdShipment.Value!.Id,
            new ShipShipmentRequest("Manual", "TRACK-001"));

        Assert.False(result.IsSuccess);
        Assert.Equal("order_cannot_be_marked_shipped", result.ErrorCode);

        var shipment = await service.GetShipmentByIdAsync(createdShipment.Value.Id);

        Assert.NotNull(shipment);
        Assert.Equal("Pending", shipment.Status);
    }

    [Fact]
    public async Task CancelShipmentAsync_ShouldMarkPendingShipmentAsCancelled()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, out _);

        var createdShipment = await service.CreateShipmentAsync(new CreateShipmentRequest(
            OrderId: Guid.NewGuid(),
            Carrier: null,
            TrackingNumber: null));

        var result = await service.CancelShipmentAsync(createdShipment.Value!.Id);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Cancelled", result.Value.Status);
        Assert.NotNull(result.Value.CancelledAt);
    }

    [Fact]
    public async Task CancelShipmentAsync_ShouldRejectShippedShipment()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, out _);

        var createdShipment = await service.CreateShipmentAsync(new CreateShipmentRequest(
            OrderId: Guid.NewGuid(),
            Carrier: null,
            TrackingNumber: null));

        var shipResult = await service.ShipShipmentAsync(
            createdShipment.Value!.Id,
            new ShipShipmentRequest("Manual", "TRACK-001"));

        var cancelResult = await service.CancelShipmentAsync(createdShipment.Value.Id);

        Assert.True(shipResult.IsSuccess);
        Assert.False(cancelResult.IsSuccess);
        Assert.Equal("shipment_cannot_be_cancelled", cancelResult.ErrorCode);
    }

    private static EfFulfillmentService CreateService(
        FulfillmentDbContext dbContext,
        out FakeOrderingClient orderingClient)
    {
        orderingClient = new FakeOrderingClient();

        return new EfFulfillmentService(dbContext, orderingClient);
    }

    private static FulfillmentDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FulfillmentDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new FulfillmentDbContext(options);
    }

    private sealed class FakeOrderingClient : IOrderingClient
    {
        public List<Guid> MarkShippedRequests { get; } = [];

        public OrderingClientResult? NextResult { get; set; }

        public Task<OrderingClientResult> MarkOrderShippedAsync(
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            MarkShippedRequests.Add(orderId);

            if (NextResult is not null)
            {
                var result = NextResult;
                NextResult = null;

                return Task.FromResult(result);
            }

            return Task.FromResult(OrderingClientResult.Success());
        }
    }
}
