using Microsoft.EntityFrameworkCore;
using Ordering.Application.Inventory;
using Ordering.Application.Orders;
using Ordering.Infrastructure.Orders;
using Ordering.Infrastructure.Persistence;

namespace Ordering.Tests.Orders;

public sealed class OrderingServiceTests
{
    private static readonly Guid WarehouseId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid LocationId = Guid.Parse("20000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task CreateOrderAsync_ShouldCreatePendingPaymentOrder()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, out var inventoryClient);

        var result = await service.CreateOrderAsync(CreateValidOrderRequest());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("PendingPayment", result.Value.Status);
        Assert.Equal("Test Customer", result.Value.CustomerName);
        Assert.Equal("test@example.com", result.Value.CustomerEmail);
        Assert.Equal("USD", result.Value.Currency);
        Assert.Single(result.Value.Items);
        Assert.NotNull(result.Value.Items[0].InventoryReservationId);

        Assert.Single(inventoryClient.AllocateRequests);
        Assert.Equal("ARM-BLK", inventoryClient.AllocateRequests[0].Sku);
        Assert.Equal(2, inventoryClient.AllocateRequests[0].Quantity);
        Assert.StartsWith("ORDER-", inventoryClient.AllocateRequests[0].Reference);

        var orderExists = await dbContext.Orders
            .AnyAsync(order => order.Id == result.Value.Id);

        Assert.True(orderExists);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldCalculateLineTotalAndOrderTotal()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, out _);

        var result = await service.CreateOrderAsync(new CreateOrderRequest(
            CustomerName: "Test Customer",
            CustomerEmail: "test@example.com",
            Items:
            [
                CreateOrderItem(
                    sku: "ARM-BLK",
                    productName: "Monitor Arm",
                    variantName: "Black",
                    unitPriceAmountMinor: 6900,
                    quantity: 2),
                CreateOrderItem(
                    sku: "LAMP-BLK",
                    productName: "Desk Lamp",
                    variantName: "Black",
                    unitPriceAmountMinor: 4900,
                    quantity: 3)
            ]));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(28500, result.Value.TotalAmountMinor);
        Assert.Equal(2, result.Value.Items.Count);

        var armItem = result.Value.Items.Single(item => item.Sku == "ARM-BLK");
        var lampItem = result.Value.Items.Single(item => item.Sku == "LAMP-BLK");

        Assert.Equal(13800, armItem.LineTotalAmountMinor);
        Assert.Equal(14700, lampItem.LineTotalAmountMinor);
        Assert.NotNull(armItem.InventoryReservationId);
        Assert.NotNull(lampItem.InventoryReservationId);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldNormalizeSkuAndCurrency()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, out _);

        var result = await service.CreateOrderAsync(new CreateOrderRequest(
            CustomerName: "Test Customer",
            CustomerEmail: "test@example.com",
            Items:
            [
                new CreateOrderItemRequest(
                    ProductId: Guid.NewGuid(),
                    ProductVariantId: Guid.NewGuid(),
                    Sku: " arm-blk ",
                    ProductName: " Monitor Arm ",
                    VariantName: " Black ",
                    UnitPriceAmountMinor: 6900,
                    Currency: " usd ",
                    Quantity: 1,
                    WarehouseId: WarehouseId,
                    LocationId: LocationId)
            ]));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("USD", result.Value.Currency);
        Assert.Equal("ARM-BLK", result.Value.Items[0].Sku);
        Assert.Equal("Monitor Arm", result.Value.Items[0].ProductName);
        Assert.Equal("Black", result.Value.Items[0].VariantName);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldRejectMissingCustomerName()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, out _);

        var request = CreateValidOrderRequest() with
        {
            CustomerName = " "
        };

        var result = await service.CreateOrderAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("customer_name_required", result.ErrorCode);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldRejectMissingCustomerEmail()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, out _);

        var request = CreateValidOrderRequest() with
        {
            CustomerEmail = " "
        };

        var result = await service.CreateOrderAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("customer_email_required", result.ErrorCode);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldRejectEmptyItems()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, out _);

        var result = await service.CreateOrderAsync(new CreateOrderRequest(
            CustomerName: "Test Customer",
            CustomerEmail: "test@example.com",
            Items: Array.Empty<CreateOrderItemRequest>()));

        Assert.False(result.IsSuccess);
        Assert.Equal("order_items_required", result.ErrorCode);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldRejectInvalidQuantity()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, out _);

        var request = new CreateOrderRequest(
            CustomerName: "Test Customer",
            CustomerEmail: "test@example.com",
            Items:
            [
                CreateOrderItem(quantity: 0)
            ]);

        var result = await service.CreateOrderAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("quantity_invalid", result.ErrorCode);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldRejectNegativeUnitPrice()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, out _);

        var request = new CreateOrderRequest(
            CustomerName: "Test Customer",
            CustomerEmail: "test@example.com",
            Items:
            [
                CreateOrderItem(unitPriceAmountMinor: -1)
            ]);

        var result = await service.CreateOrderAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("unit_price_invalid", result.ErrorCode);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldRejectMixedCurrencies()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, out _);

        var request = new CreateOrderRequest(
            CustomerName: "Test Customer",
            CustomerEmail: "test@example.com",
            Items:
            [
                CreateOrderItem(sku: "ARM-BLK", currency: "USD"),
                CreateOrderItem(sku: "LAMP-BLK", currency: "EUR")
            ]);

        var result = await service.CreateOrderAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("mixed_currencies_not_supported", result.ErrorCode);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldNotRequireWarehouseOrLocation()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, out var inventoryClient);

        var result = await service.CreateOrderAsync(new CreateOrderRequest(
            CustomerName: "Test Customer",
            CustomerEmail: "test@example.com",
            Items:
            [
                CreateOrderItem(
                    warehouseId: Guid.Empty,
                    locationId: Guid.Empty)
            ]));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        Assert.Single(inventoryClient.AllocateRequests);
        Assert.Equal("ARM-BLK", inventoryClient.AllocateRequests[0].Sku);
        Assert.Equal(1, inventoryClient.AllocateRequests[0].Quantity);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldRejectOrder_WhenInventoryReservationFails()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, out var inventoryClient);

        inventoryClient.NextAllocateResult =
            InventoryClientResult<InventoryReservationDto>.Failure(
                "insufficient_stock",
                "Not enough available stock to reserve.");

        var result = await service.CreateOrderAsync(CreateValidOrderRequest());

        Assert.False(result.IsSuccess);
        Assert.Equal("insufficient_stock", result.ErrorCode);

        var orderCount = await dbContext.Orders.CountAsync();

        Assert.Equal(0, orderCount);
    }

    [Fact]
    public async Task GetOrdersAsync_ShouldReturnOrders()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, out _);

        var createdOrder = await service.CreateOrderAsync(CreateValidOrderRequest());

        var orders = await service.GetOrdersAsync();

        Assert.Single(orders);
        Assert.Equal(createdOrder.Value!.Id, orders[0].Id);
        Assert.Equal("Test Customer", orders[0].CustomerName);
        Assert.Equal("PendingPayment", orders[0].Status);
        Assert.Equal(1, orders[0].ItemCount);
        Assert.Equal(13800, orders[0].TotalAmountMinor);
    }

    [Fact]
    public async Task GetOrderByIdAsync_ShouldReturnOrderDetails()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, out _);

        var createdOrder = await service.CreateOrderAsync(CreateValidOrderRequest());

        var order = await service.GetOrderByIdAsync(createdOrder.Value!.Id);

        Assert.NotNull(order);
        Assert.Equal(createdOrder.Value.Id, order.Id);
        Assert.Equal("Test Customer", order.CustomerName);
        Assert.Equal("PendingPayment", order.Status);
        Assert.Single(order.Items);
        Assert.Equal("ARM-BLK", order.Items[0].Sku);
        Assert.NotNull(order.Items[0].InventoryReservationId);
    }

    [Fact]
    public async Task GetOrderByIdAsync_ShouldReturnNull_WhenOrderDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, out _);

        var order = await service.GetOrderByIdAsync(Guid.NewGuid());

        Assert.Null(order);
    }

    [Fact]
    public async Task CancelOrderAsync_ShouldCancelPendingPaymentOrderAndReleaseReservation()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, out var inventoryClient);

        var createdOrder = await service.CreateOrderAsync(CreateValidOrderRequest());

        var result = await service.CancelOrderAsync(createdOrder.Value!.Id);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Cancelled", result.Value.Status);

        Assert.Single(inventoryClient.ReleaseRequests);
        Assert.Equal(
            createdOrder.Value.Items[0].InventoryReservationId,
            inventoryClient.ReleaseRequests[0]);

        var order = await service.GetOrderByIdAsync(createdOrder.Value.Id);

        Assert.NotNull(order);
        Assert.Equal("Cancelled", order.Status);
    }

    [Fact]
    public async Task CancelOrderAsync_ShouldRejectMissingOrder()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, out _);

        var result = await service.CancelOrderAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal("order_not_found", result.ErrorCode);
    }

    [Fact]
    public async Task CancelOrderAsync_ShouldRejectAlreadyCancelledOrder()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, out _);

        var createdOrder = await service.CreateOrderAsync(CreateValidOrderRequest());

        var firstCancel = await service.CancelOrderAsync(createdOrder.Value!.Id);
        var secondCancel = await service.CancelOrderAsync(createdOrder.Value.Id);

        Assert.True(firstCancel.IsSuccess);
        Assert.False(secondCancel.IsSuccess);
        Assert.Equal("order_cannot_be_cancelled", secondCancel.ErrorCode);
    }

    [Fact]
    public async Task MarkOrderPaidAsync_ShouldMarkPendingPaymentOrderAsPaidAndCommitReservation()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, out var inventoryClient);

        var createdOrder = await service.CreateOrderAsync(CreateValidOrderRequest());

        var result = await service.MarkOrderPaidAsync(createdOrder.Value!.Id);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Paid", result.Value.Status);

        Assert.Single(inventoryClient.CommitRequests);
        Assert.Equal(
            createdOrder.Value.Items[0].InventoryReservationId,
            inventoryClient.CommitRequests[0]);

        var order = await service.GetOrderByIdAsync(createdOrder.Value.Id);

        Assert.NotNull(order);
        Assert.Equal("Paid", order.Status);
    }

    [Fact]
    public async Task MarkOrderPaidAsync_ShouldRejectMissingOrder()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, out _);

        var result = await service.MarkOrderPaidAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal("order_not_found", result.ErrorCode);
    }

    [Fact]
    public async Task MarkOrderPaidAsync_ShouldRejectCancelledOrder()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, out _);

        var createdOrder = await service.CreateOrderAsync(CreateValidOrderRequest());

        var cancelResult = await service.CancelOrderAsync(createdOrder.Value!.Id);
        var paidResult = await service.MarkOrderPaidAsync(createdOrder.Value.Id);

        Assert.True(cancelResult.IsSuccess);
        Assert.False(paidResult.IsSuccess);
        Assert.Equal("order_cannot_be_marked_paid", paidResult.ErrorCode);
    }

    [Fact]
    public async Task MarkOrderPaidAsync_ShouldRejectAlreadyPaidOrder()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, out _);

        var createdOrder = await service.CreateOrderAsync(CreateValidOrderRequest());

        var firstResult = await service.MarkOrderPaidAsync(createdOrder.Value!.Id);
        var secondResult = await service.MarkOrderPaidAsync(createdOrder.Value.Id);

        Assert.True(firstResult.IsSuccess);
        Assert.False(secondResult.IsSuccess);
        Assert.Equal("order_cannot_be_marked_paid", secondResult.ErrorCode);
    }

    private static EfOrderingService CreateService(
        OrderingDbContext dbContext,
        out FakeInventoryClient inventoryClient)
    {
        inventoryClient = new FakeInventoryClient();

        return new EfOrderingService(dbContext, inventoryClient);
    }

    private static CreateOrderRequest CreateValidOrderRequest()
    {
        return new CreateOrderRequest(
            CustomerName: "Test Customer",
            CustomerEmail: "test@example.com",
            Items:
            [
                CreateOrderItem(
                    sku: "ARM-BLK",
                    productName: "Monitor Arm",
                    variantName: "Black",
                    unitPriceAmountMinor: 6900,
                    currency: "USD",
                    quantity: 2)
            ]);
    }

    private static CreateOrderItemRequest CreateOrderItem(
        string sku = "ARM-BLK",
        string productName = "Monitor Arm",
        string variantName = "Black",
        long unitPriceAmountMinor = 6900,
        string currency = "USD",
        int quantity = 1,
        Guid? warehouseId = null,
        Guid? locationId = null)
    {
        return new CreateOrderItemRequest(
            ProductId: Guid.NewGuid(),
            ProductVariantId: Guid.NewGuid(),
            Sku: sku,
            ProductName: productName,
            VariantName: variantName,
            UnitPriceAmountMinor: unitPriceAmountMinor,
            Currency: currency,
            Quantity: quantity,
            WarehouseId: warehouseId ?? WarehouseId,
            LocationId: locationId ?? LocationId);
    }

    private static OrderingDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrderingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new OrderingDbContext(options);
    }

    private sealed class FakeInventoryClient : IInventoryClient
    {
        public List<AllocateStockRequest> AllocateRequests { get; } = [];

        public List<Guid> ReleaseRequests { get; } = [];

        public List<Guid> CommitRequests { get; } = [];

        public InventoryClientResult<InventoryReservationDto>? NextAllocateResult { get; set; }

        public Task<InventoryClientResult<InventoryReservationDto>> AllocateStockAsync(
            AllocateStockRequest request,
            CancellationToken cancellationToken = default)
        {
            AllocateRequests.Add(request);

            if (NextAllocateResult is not null)
            {
                var result = NextAllocateResult;
                NextAllocateResult = null;

                return Task.FromResult(result);
            }

            var reservation = new InventoryReservationDto(
                Id: Guid.NewGuid(),
                Sku: request.Sku,
                WarehouseId: WarehouseId,
                LocationId: LocationId,
                Quantity: request.Quantity,
                Status: "Active",
                Reference: request.Reference,
                CreatedAt: DateTimeOffset.UtcNow,
                ReleasedAt: null,
                CommittedAt: null);

            return Task.FromResult(
                InventoryClientResult<InventoryReservationDto>.Success(reservation));
        }

        public Task<InventoryClientResult<InventoryReservationDto>> ReleaseReservationAsync(
            Guid reservationId,
            CancellationToken cancellationToken = default)
        {
            ReleaseRequests.Add(reservationId);

            var reservation = new InventoryReservationDto(
                Id: reservationId,
                Sku: "ARM-BLK",
                WarehouseId: WarehouseId,
                LocationId: LocationId,
                Quantity: 1,
                Status: "Released",
                Reference: null,
                CreatedAt: DateTimeOffset.UtcNow,
                ReleasedAt: DateTimeOffset.UtcNow,
                CommittedAt: null);

            return Task.FromResult(
                InventoryClientResult<InventoryReservationDto>.Success(reservation));
        }

        public Task<InventoryClientResult<InventoryReservationDto>> CommitReservationAsync(
            Guid reservationId,
            CancellationToken cancellationToken = default)
        {
            CommitRequests.Add(reservationId);

            var reservation = new InventoryReservationDto(
                Id: reservationId,
                Sku: "ARM-BLK",
                WarehouseId: WarehouseId,
                LocationId: LocationId,
                Quantity: 1,
                Status: "Committed",
                Reference: null,
                CreatedAt: DateTimeOffset.UtcNow,
                ReleasedAt: null,
                CommittedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(
                InventoryClientResult<InventoryReservationDto>.Success(reservation));
        }
    }
}