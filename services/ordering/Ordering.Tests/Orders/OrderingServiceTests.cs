using Microsoft.EntityFrameworkCore;
using Ordering.Application.Inventory;
using Ordering.Application.Orders;
using Ordering.Infrastructure.Orders;
using Ordering.Infrastructure.Persistence;
using Ordering.Application.Catalog;


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
        var service = CreateService(dbContext, out _, out var catalogClient);

        var armVariantId = Guid.NewGuid();
        var lampVariantId = Guid.NewGuid();

        catalogClient.QueueSnapshot(
            productVariantId: armVariantId,
            sku: "ARM-BLK",
            productName: "Monitor Arm",
            variantName: "Black",
            priceAmountMinor: 6900,
            currency: "USD");

        catalogClient.QueueSnapshot(
            productVariantId: lampVariantId,
            sku: "LAMP-BLK",
            productName: "Desk Lamp",
            variantName: "Black",
            priceAmountMinor: 4900,
            currency: "USD");

        var result = await service.CreateOrderAsync(new CreateOrderRequest(
            CustomerName: "Test Customer",
            CustomerEmail: "test@example.com",
            Items:
            [
                CreateOrderItem(productVariantId: armVariantId, quantity: 2),
                CreateOrderItem(productVariantId: lampVariantId, quantity: 3)
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
    public async Task CreateOrderAsync_ShouldNormalizeCatalogSnapshotValues()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, out _, out var catalogClient);

        var variantId = Guid.NewGuid();

        catalogClient.QueueSnapshot(
            productVariantId: variantId,
            sku: " arm-blk ",
            productName: " Monitor Arm ",
            variantName: " Black ",
            priceAmountMinor: 6900,
            currency: " usd ");

        var result = await service.CreateOrderAsync(new CreateOrderRequest(
            CustomerName: "Test Customer",
            CustomerEmail: "test@example.com",
            Items:
            [
                CreateOrderItem(productVariantId: variantId)
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
    public async Task CreateOrderAsync_ShouldRejectNegativeUnitPriceFromCatalogSnapshot()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, out _, out var catalogClient);

        catalogClient.QueueSnapshot(priceAmountMinor: -1);

        var request = new CreateOrderRequest(
            CustomerName: "Test Customer",
            CustomerEmail: "test@example.com",
            Items:
            [
                CreateOrderItem()
            ]);

        var result = await service.CreateOrderAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("unit_price_invalid", result.ErrorCode);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldRejectMixedCurrenciesFromCatalogSnapshots()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, out _, out var catalogClient);

        var armVariantId = Guid.NewGuid();
        var lampVariantId = Guid.NewGuid();

        catalogClient.QueueSnapshot(
            productVariantId: armVariantId,
            sku: "ARM-BLK",
            currency: "USD");

        catalogClient.QueueSnapshot(
            productVariantId: lampVariantId,
            sku: "LAMP-BLK",
            currency: "EUR");

        var request = new CreateOrderRequest(
            CustomerName: "Test Customer",
            CustomerEmail: "test@example.com",
            Items:
            [
                CreateOrderItem(productVariantId: armVariantId),
                CreateOrderItem(productVariantId: lampVariantId)
            ]);

        var result = await service.CreateOrderAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("mixed_currencies_not_supported", result.ErrorCode);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldRejectOrder_WhenCatalogSnapshotFails()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, out var inventoryClient, out var catalogClient);

        catalogClient.QueueFailure(
            "catalog_variant_snapshot_not_found",
            "Active product variant snapshot was not found.");

        var result = await service.CreateOrderAsync(CreateValidOrderRequest());

        Assert.False(result.IsSuccess);
        Assert.Equal("catalog_variant_snapshot_not_found", result.ErrorCode);
        Assert.Empty(inventoryClient.AllocateRequests);

        var orderCount = await dbContext.Orders.CountAsync();

        Assert.Equal(0, orderCount);
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
        return CreateService(dbContext, out inventoryClient, out _);
    }

    private static EfOrderingService CreateService(
        OrderingDbContext dbContext,
        out FakeInventoryClient inventoryClient,
        out FakeCatalogClient catalogClient)
    {
        inventoryClient = new FakeInventoryClient();
        catalogClient = new FakeCatalogClient();

        return new EfOrderingService(dbContext, inventoryClient, catalogClient);
    }

    private static CreateOrderRequest CreateValidOrderRequest()
    {
        return new CreateOrderRequest(
            CustomerName: "Test Customer",
            CustomerEmail: "test@example.com",
            Items:
            [
                CreateOrderItem(quantity: 2)
            ]);
    }

    private static CreateOrderItemRequest CreateOrderItem(
        Guid? productVariantId = null,
        int quantity = 1)
    {
        return new CreateOrderItemRequest(
            ProductVariantId: productVariantId ?? Guid.NewGuid(),
            Quantity: quantity);
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

    private sealed class FakeCatalogClient : ICatalogClient
    {
        private readonly Queue<Func<Guid, CatalogClientResult<ProductVariantSnapshotDto>>> _snapshotResults = [];

        public List<Guid> SnapshotRequests { get; } = [];

        public void QueueSnapshot(
            Guid? productId = null,
            Guid? productVariantId = null,
            string sku = "ARM-BLK",
            string productName = "Monitor Arm",
            string variantName = "Black",
            long priceAmountMinor = 6900,
            string currency = "USD")
        {
            _snapshotResults.Enqueue(requestedProductVariantId =>
                CatalogClientResult<ProductVariantSnapshotDto>.Success(
                    new ProductVariantSnapshotDto(
                        ProductId: productId ?? Guid.NewGuid(),
                        ProductVariantId: productVariantId ?? requestedProductVariantId,
                        Sku: sku,
                        ProductName: productName,
                        VariantName: variantName,
                        PriceAmountMinor: priceAmountMinor,
                        Currency: currency)));
        }

        public void QueueFailure(string errorCode, string errorMessage)
        {
            _snapshotResults.Enqueue(_ =>
                CatalogClientResult<ProductVariantSnapshotDto>.Failure(
                    errorCode,
                    errorMessage));
        }

        public Task<CatalogClientResult<ProductVariantSnapshotDto>> GetProductVariantSnapshotAsync(
            Guid productVariantId,
            CancellationToken cancellationToken = default)
        {
            SnapshotRequests.Add(productVariantId);

            if (_snapshotResults.Count > 0)
            {
                return Task.FromResult(_snapshotResults.Dequeue()(productVariantId));
            }

            var snapshot = new ProductVariantSnapshotDto(
                ProductId: Guid.NewGuid(),
                ProductVariantId: productVariantId,
                Sku: "ARM-BLK",
                ProductName: "Monitor Arm",
                VariantName: "Black",
                PriceAmountMinor: 6900,
                Currency: "USD");

            return Task.FromResult(
                CatalogClientResult<ProductVariantSnapshotDto>.Success(snapshot));
        }
    }
}