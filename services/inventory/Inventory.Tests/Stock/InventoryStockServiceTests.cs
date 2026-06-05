using Inventory.Application.Stock;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Stock;
using Microsoft.EntityFrameworkCore;
using Inventory.Domain.Stock;

namespace Inventory.Tests.Stock;

public sealed class InventoryStockServiceTests
{
    [Fact]
    public async Task CreateWarehouseAsync_ShouldCreateActiveWarehouse()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfInventoryStockService(dbContext);

        var result = await service.CreateWarehouseAsync(new CreateWarehouseRequest(
            Code: "main",
            Name: "Main Warehouse"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("MAIN", result.Value.Code);
        Assert.Equal("Main Warehouse", result.Value.Name);
        Assert.True(result.Value.IsActive);

        var warehouseExists = await dbContext.Warehouses
            .AnyAsync(warehouse => warehouse.Id == result.Value.Id);

        Assert.True(warehouseExists);
    }

    [Fact]
    public async Task CreateWarehouseAsync_ShouldRejectDuplicateCode()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfInventoryStockService(dbContext);

        var firstResult = await service.CreateWarehouseAsync(new CreateWarehouseRequest(
            Code: "MAIN",
            Name: "Main Warehouse"));

        var secondResult = await service.CreateWarehouseAsync(new CreateWarehouseRequest(
            Code: "main",
            Name: "Duplicate Main Warehouse"));

        Assert.True(firstResult.IsSuccess);
        Assert.False(secondResult.IsSuccess);
        Assert.Equal("warehouse_code_already_exists", secondResult.ErrorCode);
    }

    [Fact]
    public async Task GetWarehousesAsync_ShouldReturnWarehousesOrderedByCode()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfInventoryStockService(dbContext);

        await service.CreateWarehouseAsync(new CreateWarehouseRequest(
            Code: "Z-WH",
            Name: "Z Warehouse"));

        await service.CreateWarehouseAsync(new CreateWarehouseRequest(
            Code: "A-WH",
            Name: "A Warehouse"));

        var warehouses = await service.GetWarehousesAsync();

        Assert.Equal(2, warehouses.Count);
        Assert.Equal("A-WH", warehouses[0].Code);
        Assert.Equal("Z-WH", warehouses[1].Code);
    }

    [Fact]
    public async Task CreateLocationAsync_ShouldCreateActiveLocation()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfInventoryStockService(dbContext);

        var warehouseResult = await service.CreateWarehouseAsync(new CreateWarehouseRequest(
            Code: "MAIN",
            Name: "Main Warehouse"));

        var locationResult = await service.CreateLocationAsync(new CreateLocationRequest(
            WarehouseId: warehouseResult.Value!.Id,
            Code: "a-01-01"));

        Assert.True(locationResult.IsSuccess);
        Assert.NotNull(locationResult.Value);
        Assert.Equal(warehouseResult.Value.Id, locationResult.Value.WarehouseId);
        Assert.Equal("A-01-01", locationResult.Value.Code);
        Assert.True(locationResult.Value.IsActive);

        var locationExists = await dbContext.StorageLocations
            .AnyAsync(location => location.Id == locationResult.Value.Id);

        Assert.True(locationExists);
    }

    [Fact]
    public async Task CreateLocationAsync_ShouldRejectMissingWarehouse()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfInventoryStockService(dbContext);

        var result = await service.CreateLocationAsync(new CreateLocationRequest(
            WarehouseId: Guid.NewGuid(),
            Code: "A-01-01"));

        Assert.False(result.IsSuccess);
        Assert.Equal("warehouse_not_found", result.ErrorCode);
    }

    [Fact]
    public async Task CreateLocationAsync_ShouldRejectDuplicateCodeInsideSameWarehouse()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfInventoryStockService(dbContext);

        var warehouseResult = await service.CreateWarehouseAsync(new CreateWarehouseRequest(
            Code: "MAIN",
            Name: "Main Warehouse"));

        var firstResult = await service.CreateLocationAsync(new CreateLocationRequest(
            WarehouseId: warehouseResult.Value!.Id,
            Code: "A-01-01"));

        var secondResult = await service.CreateLocationAsync(new CreateLocationRequest(
            WarehouseId: warehouseResult.Value.Id,
            Code: "a-01-01"));

        Assert.True(firstResult.IsSuccess);
        Assert.False(secondResult.IsSuccess);
        Assert.Equal("location_code_already_exists", secondResult.ErrorCode);
    }

    [Fact]
    public async Task GetLocationsAsync_ShouldFilterByWarehouseId()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfInventoryStockService(dbContext);

        var mainWarehouse = await service.CreateWarehouseAsync(new CreateWarehouseRequest(
            Code: "MAIN",
            Name: "Main Warehouse"));

        var backupWarehouse = await service.CreateWarehouseAsync(new CreateWarehouseRequest(
            Code: "BACKUP",
            Name: "Backup Warehouse"));

        await service.CreateLocationAsync(new CreateLocationRequest(
            WarehouseId: mainWarehouse.Value!.Id,
            Code: "A-01-01"));

        await service.CreateLocationAsync(new CreateLocationRequest(
            WarehouseId: backupWarehouse.Value!.Id,
            Code: "B-01-01"));

        var locations = await service.GetLocationsAsync(mainWarehouse.Value.Id);

        Assert.Single(locations);
        Assert.Equal(mainWarehouse.Value.Id, locations[0].WarehouseId);
        Assert.Equal("A-01-01", locations[0].Code);
    }

    [Fact]
    public async Task ReceiveStockAsync_ShouldCreateStockItemAndMovement()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfInventoryStockService(dbContext);

        var setup = await CreateWarehouseAndLocationAsync(service);

        var result = await service.ReceiveStockAsync(new ReceiveStockRequest(
            Sku: "arm-blk",
            WarehouseId: setup.WarehouseId,
            LocationId: setup.LocationId,
            Quantity: 50,
            Reason: "Initial stock"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("ARM-BLK", result.Value.Sku);
        Assert.Equal(50, result.Value.ReceivedQuantity);
        Assert.Equal(50, result.Value.OnHandQuantity);
        Assert.Equal(0, result.Value.ReservedQuantity);
        Assert.Equal(50, result.Value.AvailableQuantity);

        var stockItemCount = await dbContext.StockItems.CountAsync();
        var movementCount = await dbContext.StockMovements.CountAsync();

        Assert.Equal(1, stockItemCount);
        Assert.Equal(1, movementCount);
    }

    [Fact]
    public async Task ReceiveStockAsync_ShouldAccumulateQuantityForSameSkuWarehouseAndLocation()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfInventoryStockService(dbContext);

        var setup = await CreateWarehouseAndLocationAsync(service);

        await service.ReceiveStockAsync(new ReceiveStockRequest(
            Sku: "ARM-BLK",
            WarehouseId: setup.WarehouseId,
            LocationId: setup.LocationId,
            Quantity: 50,
            Reason: "Initial stock"));

        var secondReceipt = await service.ReceiveStockAsync(new ReceiveStockRequest(
            Sku: "ARM-BLK",
            WarehouseId: setup.WarehouseId,
            LocationId: setup.LocationId,
            Quantity: 25,
            Reason: "Second receipt"));

        Assert.True(secondReceipt.IsSuccess);
        Assert.NotNull(secondReceipt.Value);
        Assert.Equal(75, secondReceipt.Value.OnHandQuantity);
        Assert.Equal(75, secondReceipt.Value.AvailableQuantity);

        var stockItemCount = await dbContext.StockItems.CountAsync();
        var movementCount = await dbContext.StockMovements.CountAsync();

        Assert.Equal(1, stockItemCount);
        Assert.Equal(2, movementCount);
    }

    [Fact]
    public async Task ReceiveStockAsync_ShouldRejectInvalidQuantity()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfInventoryStockService(dbContext);

        var setup = await CreateWarehouseAndLocationAsync(service);

        var result = await service.ReceiveStockAsync(new ReceiveStockRequest(
            Sku: "ARM-BLK",
            WarehouseId: setup.WarehouseId,
            LocationId: setup.LocationId,
            Quantity: 0,
            Reason: "Invalid receipt"));

        Assert.False(result.IsSuccess);
        Assert.Equal("receipt_quantity_invalid", result.ErrorCode);
    }

    [Fact]
    public async Task ReceiveStockAsync_ShouldRejectLocationWarehouseMismatch()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfInventoryStockService(dbContext);

        var warehouseA = await service.CreateWarehouseAsync(new CreateWarehouseRequest(
            Code: "A",
            Name: "Warehouse A"));

        var warehouseB = await service.CreateWarehouseAsync(new CreateWarehouseRequest(
            Code: "B",
            Name: "Warehouse B"));

        var locationB = await service.CreateLocationAsync(new CreateLocationRequest(
            WarehouseId: warehouseB.Value!.Id,
            Code: "B-01-01"));

        var result = await service.ReceiveStockAsync(new ReceiveStockRequest(
            Sku: "ARM-BLK",
            WarehouseId: warehouseA.Value!.Id,
            LocationId: locationB.Value!.Id,
            Quantity: 10,
            Reason: "Mismatch test"));

        Assert.False(result.IsSuccess);
        Assert.Equal("location_warehouse_mismatch", result.ErrorCode);
    }

    [Fact]
    public async Task GetStockBySkuAsync_ShouldReturnStockSummary()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfInventoryStockService(dbContext);

        var setup = await CreateWarehouseAndLocationAsync(service);

        await service.ReceiveStockAsync(new ReceiveStockRequest(
            Sku: "ARM-BLK",
            WarehouseId: setup.WarehouseId,
            LocationId: setup.LocationId,
            Quantity: 75,
            Reason: "Initial stock"));

        var stock = await service.GetStockBySkuAsync("arm-blk");

        Assert.NotNull(stock);
        Assert.Equal("ARM-BLK", stock.Sku);
        Assert.Equal(75, stock.TotalOnHandQuantity);
        Assert.Equal(0, stock.TotalReservedQuantity);
        Assert.Equal(75, stock.TotalAvailableQuantity);
        Assert.Single(stock.Locations);
        Assert.Equal("MAIN", stock.Locations[0].WarehouseCode);
        Assert.Equal("A-01-01", stock.Locations[0].LocationCode);
    }

    [Fact]
    public async Task GetStockBySkuAsync_ShouldReturnNull_WhenSkuDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfInventoryStockService(dbContext);

        var stock = await service.GetStockBySkuAsync("MISSING-SKU");

        Assert.Null(stock);
    }

    [Fact]
    public async Task GetStockMovementsAsync_ShouldReturnReceiptHistory()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfInventoryStockService(dbContext);

        var setup = await CreateWarehouseAndLocationAsync(service);

        await service.ReceiveStockAsync(new ReceiveStockRequest(
            Sku: "ARM-BLK",
            WarehouseId: setup.WarehouseId,
            LocationId: setup.LocationId,
            Quantity: 50,
            Reason: "Initial stock"));

        await service.ReceiveStockAsync(new ReceiveStockRequest(
            Sku: "ARM-BLK",
            WarehouseId: setup.WarehouseId,
            LocationId: setup.LocationId,
            Quantity: 25,
            Reason: "Second receipt"));

        var movements = await service.GetStockMovementsAsync("arm-blk");

        Assert.Equal(2, movements.Count);
        Assert.All(movements, movement =>
        {
            Assert.Equal("ARM-BLK", movement.Sku);
            Assert.Equal("Receipt", movement.Type);
        });
    }

    [Fact]
    public async Task ReserveStockAsync_ShouldIncreaseReservedQuantity()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfInventoryStockService(dbContext);

        var setup = await CreateWarehouseAndLocationAsync(service);

        await service.ReceiveStockAsync(new ReceiveStockRequest(
            Sku: "ARM-BLK",
            WarehouseId: setup.WarehouseId,
            LocationId: setup.LocationId,
            Quantity: 75,
            Reason: "Initial stock"));

        var reservationResult = await service.ReserveStockAsync(new CreateStockReservationRequest(
            Sku: "arm-blk",
            WarehouseId: setup.WarehouseId,
            LocationId: setup.LocationId,
            Quantity: 10,
            Reference: "ORDER-1001"));

        Assert.True(reservationResult.IsSuccess);
        Assert.NotNull(reservationResult.Value);
        Assert.Equal("ARM-BLK", reservationResult.Value.Sku);
        Assert.Equal(10, reservationResult.Value.Quantity);
        Assert.Equal("Active", reservationResult.Value.Status);
        Assert.Equal("ORDER-1001", reservationResult.Value.Reference);

        var stock = await service.GetStockBySkuAsync("ARM-BLK");

        Assert.NotNull(stock);
        Assert.Equal(75, stock.TotalOnHandQuantity);
        Assert.Equal(10, stock.TotalReservedQuantity);
        Assert.Equal(65, stock.TotalAvailableQuantity);
    }

    [Fact]
    public async Task ReserveStockAsync_ShouldRejectInsufficientStock()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfInventoryStockService(dbContext);

        var setup = await CreateWarehouseAndLocationAsync(service);

        await service.ReceiveStockAsync(new ReceiveStockRequest(
            Sku: "ARM-BLK",
            WarehouseId: setup.WarehouseId,
            LocationId: setup.LocationId,
            Quantity: 5,
            Reason: "Initial stock"));

        var reservationResult = await service.ReserveStockAsync(new CreateStockReservationRequest(
            Sku: "ARM-BLK",
            WarehouseId: setup.WarehouseId,
            LocationId: setup.LocationId,
            Quantity: 10,
            Reference: "ORDER-1001"));

        Assert.False(reservationResult.IsSuccess);
        Assert.Equal("insufficient_stock", reservationResult.ErrorCode);

        var stock = await service.GetStockBySkuAsync("ARM-BLK");

        Assert.NotNull(stock);
        Assert.Equal(5, stock.TotalOnHandQuantity);
        Assert.Equal(0, stock.TotalReservedQuantity);
        Assert.Equal(5, stock.TotalAvailableQuantity);
    }

    [Fact]
    public async Task ReleaseReservationAsync_ShouldReleaseActiveReservation()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfInventoryStockService(dbContext);

        var setup = await CreateWarehouseAndLocationAsync(service);

        await service.ReceiveStockAsync(new ReceiveStockRequest(
            Sku: "ARM-BLK",
            WarehouseId: setup.WarehouseId,
            LocationId: setup.LocationId,
            Quantity: 75,
            Reason: "Initial stock"));

        var reservationResult = await service.ReserveStockAsync(new CreateStockReservationRequest(
            Sku: "ARM-BLK",
            WarehouseId: setup.WarehouseId,
            LocationId: setup.LocationId,
            Quantity: 10,
            Reference: "ORDER-1001"));

        var releaseResult = await service.ReleaseReservationAsync(reservationResult.Value!.Id);

        Assert.True(releaseResult.IsSuccess);
        Assert.NotNull(releaseResult.Value);
        Assert.Equal("Released", releaseResult.Value.Status);
        Assert.NotNull(releaseResult.Value.ReleasedAt);

        var stock = await service.GetStockBySkuAsync("ARM-BLK");

        Assert.NotNull(stock);
        Assert.Equal(75, stock.TotalOnHandQuantity);
        Assert.Equal(0, stock.TotalReservedQuantity);
        Assert.Equal(75, stock.TotalAvailableQuantity);
    }

    [Fact]
    public async Task CommitReservationAsync_ShouldCommitActiveReservationAndDecreaseStock()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfInventoryStockService(dbContext);

        var setup = await CreateWarehouseAndLocationAsync(service);

        await service.ReceiveStockAsync(new ReceiveStockRequest(
            Sku: "ARM-BLK",
            WarehouseId: setup.WarehouseId,
            LocationId: setup.LocationId,
            Quantity: 75,
            Reason: "Initial stock"));

        var reservationResult = await service.ReserveStockAsync(new CreateStockReservationRequest(
            Sku: "ARM-BLK",
            WarehouseId: setup.WarehouseId,
            LocationId: setup.LocationId,
            Quantity: 15,
            Reference: "ORDER-1002"));

        var commitResult = await service.CommitReservationAsync(reservationResult.Value!.Id);

        Assert.True(commitResult.IsSuccess);
        Assert.NotNull(commitResult.Value);
        Assert.Equal("Committed", commitResult.Value.Status);
        Assert.NotNull(commitResult.Value.CommittedAt);

        var stock = await service.GetStockBySkuAsync("ARM-BLK");

        Assert.NotNull(stock);
        Assert.Equal(60, stock.TotalOnHandQuantity);
        Assert.Equal(0, stock.TotalReservedQuantity);
        Assert.Equal(60, stock.TotalAvailableQuantity);
    }

    [Fact]
    public async Task ReleaseReservationAsync_ShouldRejectAlreadyReleasedReservation()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfInventoryStockService(dbContext);

        var setup = await CreateWarehouseAndLocationAsync(service);

        await service.ReceiveStockAsync(new ReceiveStockRequest(
            Sku: "ARM-BLK",
            WarehouseId: setup.WarehouseId,
            LocationId: setup.LocationId,
            Quantity: 75,
            Reason: "Initial stock"));

        var reservationResult = await service.ReserveStockAsync(new CreateStockReservationRequest(
            Sku: "ARM-BLK",
            WarehouseId: setup.WarehouseId,
            LocationId: setup.LocationId,
            Quantity: 10,
            Reference: "ORDER-1001"));

        var firstRelease = await service.ReleaseReservationAsync(reservationResult.Value!.Id);
        var secondRelease = await service.ReleaseReservationAsync(reservationResult.Value.Id);

        Assert.True(firstRelease.IsSuccess);
        Assert.False(secondRelease.IsSuccess);
        Assert.Equal("reservation_not_active", secondRelease.ErrorCode);
    }

    [Fact]
    public async Task CommitReservationAsync_ShouldRejectReleasedReservation()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfInventoryStockService(dbContext);

        var setup = await CreateWarehouseAndLocationAsync(service);

        await service.ReceiveStockAsync(new ReceiveStockRequest(
            Sku: "ARM-BLK",
            WarehouseId: setup.WarehouseId,
            LocationId: setup.LocationId,
            Quantity: 75,
            Reason: "Initial stock"));

        var reservationResult = await service.ReserveStockAsync(new CreateStockReservationRequest(
            Sku: "ARM-BLK",
            WarehouseId: setup.WarehouseId,
            LocationId: setup.LocationId,
            Quantity: 10,
            Reference: "ORDER-1001"));

        var releaseResult = await service.ReleaseReservationAsync(reservationResult.Value!.Id);
        var commitResult = await service.CommitReservationAsync(reservationResult.Value.Id);

        Assert.True(releaseResult.IsSuccess);
        Assert.False(commitResult.IsSuccess);
        Assert.Equal("reservation_not_active", commitResult.ErrorCode);
    }

    [Fact]
    public async Task GetStockMovementsAsync_ShouldIncludeReservationReleaseAndShipmentMovements()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfInventoryStockService(dbContext);

        var setup = await CreateWarehouseAndLocationAsync(service);

        await service.ReceiveStockAsync(new ReceiveStockRequest(
            Sku: "ARM-BLK",
            WarehouseId: setup.WarehouseId,
            LocationId: setup.LocationId,
            Quantity: 75,
            Reason: "Initial stock"));

        var reservationToRelease = await service.ReserveStockAsync(new CreateStockReservationRequest(
            Sku: "ARM-BLK",
            WarehouseId: setup.WarehouseId,
            LocationId: setup.LocationId,
            Quantity: 10,
            Reference: "ORDER-1001"));

        await service.ReleaseReservationAsync(reservationToRelease.Value!.Id);

        var reservationToCommit = await service.ReserveStockAsync(new CreateStockReservationRequest(
            Sku: "ARM-BLK",
            WarehouseId: setup.WarehouseId,
            LocationId: setup.LocationId,
            Quantity: 15,
            Reference: "ORDER-1002"));

        await service.CommitReservationAsync(reservationToCommit.Value!.Id);

        var movements = await service.GetStockMovementsAsync("ARM-BLK");

        Assert.Equal(5, movements.Count);
        Assert.Equal(1, movements.Count(movement => movement.Type == "Receipt"));
        Assert.Equal(2, movements.Count(movement => movement.Type == "Reservation"));
        Assert.Equal(1, movements.Count(movement => movement.Type == "Release"));
        Assert.Equal(1, movements.Count(movement => movement.Type == "Shipment"));

        Assert.Contains(movements, movement =>
            movement.Type == "Reservation" &&
            movement.Quantity == 10 &&
            movement.Reason == "ORDER-1001");

        Assert.Contains(movements, movement =>
            movement.Type == "Release" &&
            movement.Quantity == -10 &&
            movement.Reason == "ORDER-1001");

        Assert.Contains(movements, movement =>
            movement.Type == "Shipment" &&
            movement.Quantity == -15 &&
            movement.Reason == "ORDER-1002");
    }

    private static async Task<(Guid WarehouseId, Guid LocationId)> CreateWarehouseAndLocationAsync(
        EfInventoryStockService service)
    {
        var warehouseResult = await service.CreateWarehouseAsync(new CreateWarehouseRequest(
            Code: "MAIN",
            Name: "Main Warehouse"));

        var locationResult = await service.CreateLocationAsync(new CreateLocationRequest(
            WarehouseId: warehouseResult.Value!.Id,
            Code: "A-01-01"));

        return (warehouseResult.Value.Id, locationResult.Value!.Id);
    }

    private static InventoryDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new InventoryDbContext(options);
    }

    [Fact]
    public async Task AllocateStockAsync_ShouldReserveFromAvailableLocation()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfInventoryStockService(dbContext);

        var setup = await CreateWarehouseAndLocationAsync(service);

        await service.ReceiveStockAsync(new ReceiveStockRequest(
            Sku: "arm-blk",
            WarehouseId: setup.WarehouseId,
            LocationId: setup.LocationId,
            Quantity: 10,
            Reason: "Initial stock"));

        var result = await service.AllocateStockAsync(new AllocateStockReservationRequest(
            Sku: "arm-blk",
            Quantity: 3,
            Reference: "order:test-1"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("ARM-BLK", result.Value.Sku);
        Assert.Equal(setup.WarehouseId, result.Value.WarehouseId);
        Assert.Equal(setup.LocationId, result.Value.LocationId);
        Assert.Equal(3, result.Value.Quantity);
        Assert.Equal("Active", result.Value.Status);
        Assert.Equal("order:test-1", result.Value.Reference);

        var stockItem = await dbContext.StockItems.SingleAsync();
        Assert.Equal(10, stockItem.OnHandQuantity);
        Assert.Equal(3, stockItem.ReservedQuantity);
        Assert.Equal(7, stockItem.AvailableQuantity);

        var reservationCount = await dbContext.StockReservations.CountAsync();
        var movementCount = await dbContext.StockMovements.CountAsync();

        Assert.Equal(1, reservationCount);
        Assert.Equal(2, movementCount);
    }

    [Fact]
    public async Task AllocateStockAsync_ShouldChooseLocationWithEnoughAvailableStock()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfInventoryStockService(dbContext);

        var warehouse = await service.CreateWarehouseAsync(new CreateWarehouseRequest(
            Code: "MAIN",
            Name: "Main Warehouse"));

        var smallLocation = await service.CreateLocationAsync(new CreateLocationRequest(
            WarehouseId: warehouse.Value!.Id,
            Code: "A-01-01"));

        var largeLocation = await service.CreateLocationAsync(new CreateLocationRequest(
            WarehouseId: warehouse.Value.Id,
            Code: "A-01-02"));

        await service.ReceiveStockAsync(new ReceiveStockRequest(
            Sku: "ARM-BLK",
            WarehouseId: warehouse.Value.Id,
            LocationId: smallLocation.Value!.Id,
            Quantity: 2,
            Reason: "Small stock"));

        await service.ReceiveStockAsync(new ReceiveStockRequest(
            Sku: "ARM-BLK",
            WarehouseId: warehouse.Value.Id,
            LocationId: largeLocation.Value!.Id,
            Quantity: 10,
            Reason: "Large stock"));

        var result = await service.AllocateStockAsync(new AllocateStockReservationRequest(
            Sku: "ARM-BLK",
            Quantity: 5,
            Reference: "order:test-2"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(largeLocation.Value.Id, result.Value.LocationId);
    }

    [Fact]
    public async Task AllocateStockAsync_ShouldRejectMissingSku()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfInventoryStockService(dbContext);

        var result = await service.AllocateStockAsync(new AllocateStockReservationRequest(
            Sku: "MISSING-SKU",
            Quantity: 1,
            Reference: "order:test-missing"));

        Assert.False(result.IsSuccess);
        Assert.Equal("stock_item_not_found", result.ErrorCode);
    }

    [Fact]
    public async Task AllocateStockAsync_ShouldRejectInsufficientStock()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfInventoryStockService(dbContext);

        var setup = await CreateWarehouseAndLocationAsync(service);

        await service.ReceiveStockAsync(new ReceiveStockRequest(
            Sku: "ARM-BLK",
            WarehouseId: setup.WarehouseId,
            LocationId: setup.LocationId,
            Quantity: 2,
            Reason: "Initial stock"));

        var result = await service.AllocateStockAsync(new AllocateStockReservationRequest(
            Sku: "ARM-BLK",
            Quantity: 3,
            Reference: "order:test-insufficient"));

        Assert.False(result.IsSuccess);
        Assert.Equal("insufficient_stock", result.ErrorCode);

        var stockItem = await dbContext.StockItems.SingleAsync();
        Assert.Equal(2, stockItem.OnHandQuantity);
        Assert.Equal(0, stockItem.ReservedQuantity);
    }
}