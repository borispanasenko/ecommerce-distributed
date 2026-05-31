using Inventory.Application.Stock;
using Inventory.Domain.Stock;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Stock;

public sealed class EfInventoryStockService : IInventoryStockService
{
    private readonly InventoryDbContext _dbContext;

    public EfInventoryStockService(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<WarehouseDto>> GetWarehousesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Warehouses
            .AsNoTracking()
            .OrderBy(warehouse => warehouse.Code)
            .Select(warehouse => new WarehouseDto(
                warehouse.Id,
                warehouse.Code,
                warehouse.Name,
                warehouse.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<InventoryResult<WarehouseDto>> CreateWarehouseAsync(
        CreateWarehouseRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return InventoryResult<WarehouseDto>.Failure(
                "warehouse_code_required",
                "Warehouse code is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return InventoryResult<WarehouseDto>.Failure(
                "warehouse_name_required",
                "Warehouse name is required.");
        }

        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var codeExists = await _dbContext.Warehouses
            .AnyAsync(warehouse => warehouse.Code == normalizedCode, cancellationToken);

        if (codeExists)
        {
            return InventoryResult<WarehouseDto>.Failure(
                "warehouse_code_already_exists",
                "Warehouse code already exists.");
        }

        var now = DateTimeOffset.UtcNow;

        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            Code = normalizedCode,
            Name = request.Name.Trim(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Warehouses.Add(warehouse);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return InventoryResult<WarehouseDto>.Success(
            new WarehouseDto(
                warehouse.Id,
                warehouse.Code,
                warehouse.Name,
                warehouse.IsActive));
    }

    public async Task<IReadOnlyList<StorageLocationDto>> GetLocationsAsync(
        Guid? warehouseId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.StorageLocations
            .AsNoTracking()
            .AsQueryable();

        if (warehouseId is not null)
        {
            query = query.Where(location => location.WarehouseId == warehouseId);
        }

        return await query
            .OrderBy(location => location.Code)
            .Select(location => new StorageLocationDto(
                location.Id,
                location.WarehouseId,
                location.Code,
                location.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<InventoryResult<StorageLocationDto>> CreateLocationAsync(
        CreateLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return InventoryResult<StorageLocationDto>.Failure(
                "location_code_required",
                "Location code is required.");
        }

        var warehouse = await _dbContext.Warehouses
            .FirstOrDefaultAsync(x => x.Id == request.WarehouseId, cancellationToken);

        if (warehouse is null)
        {
            return InventoryResult<StorageLocationDto>.Failure(
                "warehouse_not_found",
                "Warehouse was not found.");
        }

        if (!warehouse.IsActive)
        {
            return InventoryResult<StorageLocationDto>.Failure(
                "warehouse_inactive",
                "Cannot create location for inactive warehouse.");
        }

        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var locationExists = await _dbContext.StorageLocations
            .AnyAsync(location =>
                    location.WarehouseId == request.WarehouseId &&
                    location.Code == normalizedCode,
                cancellationToken);

        if (locationExists)
        {
            return InventoryResult<StorageLocationDto>.Failure(
                "location_code_already_exists",
                "Location code already exists in this warehouse.");
        }

        var now = DateTimeOffset.UtcNow;

        var location = new StorageLocation
        {
            Id = Guid.NewGuid(),
            WarehouseId = warehouse.Id,
            Warehouse = warehouse,
            Code = normalizedCode,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.StorageLocations.Add(location);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return InventoryResult<StorageLocationDto>.Success(
            new StorageLocationDto(
                location.Id,
                location.WarehouseId,
                location.Code,
                location.IsActive));
    }

    public async Task<InventoryResult<StockReceiptDto>> ReceiveStockAsync(
        ReceiveStockRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Sku))
        {
            return InventoryResult<StockReceiptDto>.Failure(
                "sku_required",
                "SKU is required.");
        }

        if (request.Quantity <= 0)
        {
            return InventoryResult<StockReceiptDto>.Failure(
                "receipt_quantity_invalid",
                "Receipt quantity must be greater than zero.");
        }

        var warehouse = await _dbContext.Warehouses
            .FirstOrDefaultAsync(x => x.Id == request.WarehouseId, cancellationToken);

        if (warehouse is null)
        {
            return InventoryResult<StockReceiptDto>.Failure(
                "warehouse_not_found",
                "Warehouse was not found.");
        }

        var location = await _dbContext.StorageLocations
            .FirstOrDefaultAsync(x => x.Id == request.LocationId, cancellationToken);

        if (location is null)
        {
            return InventoryResult<StockReceiptDto>.Failure(
                "location_not_found",
                "Location was not found.");
        }

        if (location.WarehouseId != warehouse.Id)
        {
            return InventoryResult<StockReceiptDto>.Failure(
                "location_warehouse_mismatch",
                "Location does not belong to the specified warehouse.");
        }

        var normalizedSku = request.Sku.Trim().ToUpperInvariant();
        var now = DateTimeOffset.UtcNow;

        var stockItem = await _dbContext.StockItems
            .FirstOrDefaultAsync(
                item =>
                    item.Sku == normalizedSku &&
                    item.WarehouseId == warehouse.Id &&
                    item.LocationId == location.Id,
                cancellationToken);

        if (stockItem is null)
        {
            stockItem = new StockItem
            {
                Id = Guid.NewGuid(),
                Sku = normalizedSku,
                WarehouseId = warehouse.Id,
                Warehouse = warehouse,
                LocationId = location.Id,
                Location = location,
                OnHandQuantity = 0,
                ReservedQuantity = 0,
                CreatedAt = now,
                UpdatedAt = now
            };

            _dbContext.StockItems.Add(stockItem);
        }

        stockItem.OnHandQuantity += request.Quantity;
        stockItem.UpdatedAt = now;

        var movement = new StockMovement
        {
            Id = Guid.NewGuid(),
            Sku = normalizedSku,
            WarehouseId = warehouse.Id,
            Warehouse = warehouse,
            LocationId = location.Id,
            Location = location,
            Type = StockMovementType.Receipt,
            Quantity = request.Quantity,
            Reason = request.Reason?.Trim(),
            CreatedAt = now
        };

        _dbContext.StockMovements.Add(movement);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return InventoryResult<StockReceiptDto>.Success(
            new StockReceiptDto(
                stockItem.Sku,
                stockItem.WarehouseId,
                stockItem.LocationId,
                request.Quantity,
                stockItem.OnHandQuantity,
                stockItem.ReservedQuantity,
                stockItem.AvailableQuantity));
    }

    public async Task<StockSummaryDto?> GetStockBySkuAsync(
        string sku,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            return null;
        }

        var normalizedSku = sku.Trim().ToUpperInvariant();

        var items = await _dbContext.StockItems
            .AsNoTracking()
            .Include(item => item.Warehouse)
            .Include(item => item.Location)
            .Where(item => item.Sku == normalizedSku)
            .OrderBy(item => item.Warehouse.Code)
            .ThenBy(item => item.Location.Code)
            .ToListAsync(cancellationToken);

        if (items.Count == 0)
        {
            return null;
        }

        return new StockSummaryDto(
            Sku: normalizedSku,
            TotalOnHandQuantity: items.Sum(item => item.OnHandQuantity),
            TotalReservedQuantity: items.Sum(item => item.ReservedQuantity),
            TotalAvailableQuantity: items.Sum(item => item.AvailableQuantity),
            Locations: items
                .Select(item => new StockLocationBalanceDto(
                    item.WarehouseId,
                    item.Warehouse.Code,
                    item.LocationId,
                    item.Location.Code,
                    item.OnHandQuantity,
                    item.ReservedQuantity,
                    item.AvailableQuantity))
                .ToList());
    }

    public async Task<IReadOnlyList<StockMovementDto>> GetStockMovementsAsync(
        string? sku = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 500);

        var query = _dbContext.StockMovements
            .AsNoTracking()
            .Include(movement => movement.Warehouse)
            .Include(movement => movement.Location)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(sku))
        {
            var normalizedSku = sku.Trim().ToUpperInvariant();
            query = query.Where(movement => movement.Sku == normalizedSku);
        }

        return await query
            .OrderByDescending(movement => movement.CreatedAt)
            .Take(safeLimit)
            .Select(movement => new StockMovementDto(
                movement.Id,
                movement.Sku,
                movement.WarehouseId,
                movement.Warehouse.Code,
                movement.LocationId,
                movement.Location.Code,
                movement.Type.ToString(),
                movement.Quantity,
                movement.Reason,
                movement.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
