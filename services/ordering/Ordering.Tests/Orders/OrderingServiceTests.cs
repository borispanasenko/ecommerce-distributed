using Microsoft.EntityFrameworkCore;
using Ordering.Application.Orders;
using Ordering.Infrastructure.Orders;
using Ordering.Infrastructure.Persistence;

namespace Ordering.Tests.Orders;

public sealed class OrderingServiceTests
{
    [Fact]
    public async Task CreateOrderAsync_ShouldCreatePendingPaymentOrder()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfOrderingService(dbContext);

        var result = await service.CreateOrderAsync(CreateValidOrderRequest());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("PendingPayment", result.Value.Status);
        Assert.Equal("Test Customer", result.Value.CustomerName);
        Assert.Equal("test@example.com", result.Value.CustomerEmail);
        Assert.Equal("USD", result.Value.Currency);
        Assert.Single(result.Value.Items);

        var orderExists = await dbContext.Orders
            .AnyAsync(order => order.Id == result.Value.Id);

        Assert.True(orderExists);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldCalculateLineTotalAndOrderTotal()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfOrderingService(dbContext);

        var result = await service.CreateOrderAsync(new CreateOrderRequest(
            CustomerName: "Test Customer",
            CustomerEmail: "test@example.com",
            Items:
            [
                new CreateOrderItemRequest(
                    ProductId: Guid.NewGuid(),
                    ProductVariantId: Guid.NewGuid(),
                    Sku: "ARM-BLK",
                    ProductName: "Monitor Arm",
                    VariantName: "Black",
                    UnitPriceAmountMinor: 6900,
                    Currency: "USD",
                    Quantity: 2),
                new CreateOrderItemRequest(
                    ProductId: Guid.NewGuid(),
                    ProductVariantId: Guid.NewGuid(),
                    Sku: "LAMP-BLK",
                    ProductName: "Desk Lamp",
                    VariantName: "Black",
                    UnitPriceAmountMinor: 4900,
                    Currency: "USD",
                    Quantity: 3)
            ]));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(28500, result.Value.TotalAmountMinor);
        Assert.Equal(2, result.Value.Items.Count);

        var armItem = result.Value.Items.Single(item => item.Sku == "ARM-BLK");
        var lampItem = result.Value.Items.Single(item => item.Sku == "LAMP-BLK");

        Assert.Equal(13800, armItem.LineTotalAmountMinor);
        Assert.Equal(14700, lampItem.LineTotalAmountMinor);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldNormalizeSkuAndCurrency()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfOrderingService(dbContext);

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
                    Quantity: 1)
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
        var service = new EfOrderingService(dbContext);

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
        var service = new EfOrderingService(dbContext);

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
        var service = new EfOrderingService(dbContext);

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
        var service = new EfOrderingService(dbContext);

        var request = new CreateOrderRequest(
            CustomerName: "Test Customer",
            CustomerEmail: "test@example.com",
            Items:
            [
                new CreateOrderItemRequest(
                    ProductId: Guid.NewGuid(),
                    ProductVariantId: Guid.NewGuid(),
                    Sku: "ARM-BLK",
                    ProductName: "Monitor Arm",
                    VariantName: "Black",
                    UnitPriceAmountMinor: 6900,
                    Currency: "USD",
                    Quantity: 0)
            ]);

        var result = await service.CreateOrderAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("quantity_invalid", result.ErrorCode);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldRejectNegativeUnitPrice()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfOrderingService(dbContext);

        var request = new CreateOrderRequest(
            CustomerName: "Test Customer",
            CustomerEmail: "test@example.com",
            Items:
            [
                new CreateOrderItemRequest(
                    ProductId: Guid.NewGuid(),
                    ProductVariantId: Guid.NewGuid(),
                    Sku: "ARM-BLK",
                    ProductName: "Monitor Arm",
                    VariantName: "Black",
                    UnitPriceAmountMinor: -1,
                    Currency: "USD",
                    Quantity: 1)
            ]);

        var result = await service.CreateOrderAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("unit_price_invalid", result.ErrorCode);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldRejectMixedCurrencies()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfOrderingService(dbContext);

        var request = new CreateOrderRequest(
            CustomerName: "Test Customer",
            CustomerEmail: "test@example.com",
            Items:
            [
                new CreateOrderItemRequest(
                    ProductId: Guid.NewGuid(),
                    ProductVariantId: Guid.NewGuid(),
                    Sku: "ARM-BLK",
                    ProductName: "Monitor Arm",
                    VariantName: "Black",
                    UnitPriceAmountMinor: 6900,
                    Currency: "USD",
                    Quantity: 1),
                new CreateOrderItemRequest(
                    ProductId: Guid.NewGuid(),
                    ProductVariantId: Guid.NewGuid(),
                    Sku: "LAMP-BLK",
                    ProductName: "Desk Lamp",
                    VariantName: "Black",
                    UnitPriceAmountMinor: 4900,
                    Currency: "EUR",
                    Quantity: 1)
            ]);

        var result = await service.CreateOrderAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("mixed_currencies_not_supported", result.ErrorCode);
    }

    [Fact]
    public async Task GetOrdersAsync_ShouldReturnOrders()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfOrderingService(dbContext);

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
        var service = new EfOrderingService(dbContext);

        var createdOrder = await service.CreateOrderAsync(CreateValidOrderRequest());

        var order = await service.GetOrderByIdAsync(createdOrder.Value!.Id);

        Assert.NotNull(order);
        Assert.Equal(createdOrder.Value.Id, order.Id);
        Assert.Equal("Test Customer", order.CustomerName);
        Assert.Equal("PendingPayment", order.Status);
        Assert.Single(order.Items);
        Assert.Equal("ARM-BLK", order.Items[0].Sku);
    }

    [Fact]
    public async Task GetOrderByIdAsync_ShouldReturnNull_WhenOrderDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfOrderingService(dbContext);

        var order = await service.GetOrderByIdAsync(Guid.NewGuid());

        Assert.Null(order);
    }

    private static CreateOrderRequest CreateValidOrderRequest()
    {
        return new CreateOrderRequest(
            CustomerName: "Test Customer",
            CustomerEmail: "test@example.com",
            Items:
            [
                new CreateOrderItemRequest(
                    ProductId: Guid.NewGuid(),
                    ProductVariantId: Guid.NewGuid(),
                    Sku: "ARM-BLK",
                    ProductName: "Monitor Arm",
                    VariantName: "Black",
                    UnitPriceAmountMinor: 6900,
                    Currency: "USD",
                    Quantity: 2)
            ]);
    }

    private static OrderingDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrderingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new OrderingDbContext(options);
    }
}