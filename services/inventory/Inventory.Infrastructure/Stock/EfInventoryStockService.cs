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

    public async Task<InventoryResult<StockReservationDto>> ReserveStockAsync(
        CreateStockReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Sku))
        {
            return InventoryResult<StockReservationDto>.Failure(
                "sku_required",
                "SKU is required.");
        }

        if (request.Quantity <= 0)
        {
            return InventoryResult<StockReservationDto>.Failure(
                "reservation_quantity_invalid",
                "Reservation quantity must be greater than zero.");
        }

        var normalizedSku = request.Sku.Trim().ToUpperInvariant();

        var stockItem = await _dbContext.StockItems
            .Include(item => item.Warehouse)
            .Include(item => item.Location)
            .FirstOrDefaultAsync(
                item =>
                    item.Sku == normalizedSku &&
                    item.WarehouseId == request.WarehouseId &&
                    item.LocationId == request.LocationId,
                cancellationToken);

        if (stockItem is null)
        {
            return InventoryResult<StockReservationDto>.Failure(
                "stock_item_not_found",
                "Stock item was not found for the specified SKU, warehouse and location.");
        }

        if (stockItem.AvailableQuantity < request.Quantity)
        {
            return InventoryResult<StockReservationDto>.Failure(
                "insufficient_stock",
                "Not enough available stock to reserve.");
        }

        var now = DateTimeOffset.UtcNow;

        stockItem.ReservedQuantity += request.Quantity;
        stockItem.UpdatedAt = now;

        var reservation = new StockReservation
        {
            Id = Guid.NewGuid(),
            Sku = normalizedSku,
            WarehouseId = stockItem.WarehouseId,
            Warehouse = stockItem.Warehouse,
            LocationId = stockItem.LocationId,
            Location = stockItem.Location,
            Quantity = request.Quantity,
            Status = StockReservationStatus.Active,
            Reference = request.Reference?.Trim(),
            CreatedAt = now
        };

        var movement = new StockMovement
        {
            Id = Guid.NewGuid(),
            Sku = normalizedSku,
            WarehouseId = stockItem.WarehouseId,
            Warehouse = stockItem.Warehouse,
            LocationId = stockItem.LocationId,
            Location = stockItem.Location,
            Type = StockMovementType.Reservation,
            Quantity = request.Quantity,
            Reason = request.Reference?.Trim(),
            CreatedAt = now
        };

        _dbContext.StockReservations.Add(reservation);
        _dbContext.StockMovements.Add(movement);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return InventoryResult<StockReservationDto>.Success(ToReservationDto(reservation));
    }

    public async Task<InventoryResult<StockReservationDto>> AllocateStockAsync(
    AllocateStockReservationRequest request,
    CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Sku))
        {
            return InventoryResult<StockReservationDto>.Failure(
                "sku_required",
                "SKU is required.");
        }

        if (request.Quantity <= 0)
        {
            return InventoryResult<StockReservationDto>.Failure(
                "reservation_quantity_invalid",
                "Reservation quantity must be greater than zero.");
        }

        var normalizedSku = request.Sku.Trim().ToUpperInvariant();

        var stockItemExists = await _dbContext.StockItems
            .AnyAsync(item => item.Sku == normalizedSku, cancellationToken);

        if (!stockItemExists)
        {
            return InventoryResult<StockReservationDto>.Failure(
                "stock_item_not_found",
                "Stock item was not found for the specified SKU.");
        }

        var stockItem = await _dbContext.StockItems
            .Include(item => item.Warehouse)
            .Include(item => item.Location)
            .Where(item =>
                item.Sku == normalizedSku &&
                item.Warehouse.IsActive &&
                item.Location.IsActive &&
                item.OnHandQuantity - item.ReservedQuantity >= request.Quantity)
            .OrderBy(item => item.Warehouse.Code)
            .ThenBy(item => item.Location.Code)
            .FirstOrDefaultAsync(cancellationToken);

        if (stockItem is null)
        {
            return InventoryResult<StockReservationDto>.Failure(
                "insufficient_stock",
                "Not enough available stock for the specified SKU.");
        }

        var now = DateTimeOffset.UtcNow;
        var reference = request.Reference?.Trim();

        stockItem.ReservedQuantity += request.Quantity;
        stockItem.UpdatedAt = now;

        var reservation = new StockReservation
        {
            Id = Guid.NewGuid(),
            Sku = normalizedSku,
            WarehouseId = stockItem.WarehouseId,
            Warehouse = stockItem.Warehouse,
            LocationId = stockItem.LocationId,
            Location = stockItem.Location,
            Quantity = request.Quantity,
            Status = StockReservationStatus.Active,
            Reference = reference,
            CreatedAt = now,
            ReleasedAt = null,
            CommittedAt = null
        };

        var movement = new StockMovement
        {
            Id = Guid.NewGuid(),
            Sku = normalizedSku,
            WarehouseId = stockItem.WarehouseId,
            Warehouse = stockItem.Warehouse,
            LocationId = stockItem.LocationId,
            Location = stockItem.Location,
            Type = StockMovementType.Reservation,
            Quantity = request.Quantity,
            Reason = reference,
            CreatedAt = now
        };

        _dbContext.StockReservations.Add(reservation);
        _dbContext.StockMovements.Add(movement);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return InventoryResult<StockReservationDto>.Success(
            new StockReservationDto(
                reservation.Id,
                reservation.Sku,
                reservation.WarehouseId,
                reservation.LocationId,
                reservation.Quantity,
                reservation.Status.ToString(),
                reservation.Reference,
                reservation.CreatedAt,
                reservation.ReleasedAt,
                reservation.CommittedAt));
    }

    public async Task<InventoryResult<StockReservationDto>> ReleaseReservationAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        var reservation = await _dbContext.StockReservations
            .Include(x => x.Warehouse)
            .Include(x => x.Location)
            .FirstOrDefaultAsync(x => x.Id == reservationId, cancellationToken);

        if (reservation is null)
        {
            return InventoryResult<StockReservationDto>.Failure(
                "reservation_not_found",
                "Reservation was not found.");
        }

        if (reservation.Status == StockReservationStatus.Released)
        {
            return InventoryResult<StockReservationDto>.Success(ToReservationDto(reservation));
        }

        if (reservation.Status != StockReservationStatus.Active)
        {
            return InventoryResult<StockReservationDto>.Failure(
                "reservation_not_active",
                "Only active reservation can be released.");
        }

        var stockItem = await _dbContext.StockItems
            .FirstOrDefaultAsync(
                item =>
                    item.Sku == reservation.Sku &&
                    item.WarehouseId == reservation.WarehouseId &&
                    item.LocationId == reservation.LocationId,
                cancellationToken);

        if (stockItem is null)
        {
            return InventoryResult<StockReservationDto>.Failure(
                "stock_item_not_found",
                "Stock item was not found for the reservation.");
        }

        if (stockItem.ReservedQuantity < reservation.Quantity)
        {
            return InventoryResult<StockReservationDto>.Failure(
                "reservation_state_invalid",
                "Reserved quantity is lower than reservation quantity.");
        }

        var now = DateTimeOffset.UtcNow;

        stockItem.ReservedQuantity -= reservation.Quantity;
        stockItem.UpdatedAt = now;

        reservation.Status = StockReservationStatus.Released;
        reservation.ReleasedAt = now;

        var movement = new StockMovement
        {
            Id = Guid.NewGuid(),
            Sku = reservation.Sku,
            WarehouseId = reservation.WarehouseId,
            Warehouse = reservation.Warehouse,
            LocationId = reservation.LocationId,
            Location = reservation.Location,
            Type = StockMovementType.Release,
            Quantity = -reservation.Quantity,
            Reason = reservation.Reference,
            CreatedAt = now
        };

        _dbContext.StockMovements.Add(movement);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return InventoryResult<StockReservationDto>.Success(ToReservationDto(reservation));
    }

    public async Task<InventoryResult<StockReservationDto>> CommitReservationAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        var reservation = await _dbContext.StockReservations
            .Include(x => x.Warehouse)
            .Include(x => x.Location)
            .FirstOrDefaultAsync(x => x.Id == reservationId, cancellationToken);

        if (reservation is null)
        {
            return InventoryResult<StockReservationDto>.Failure(
                "reservation_not_found",
                "Reservation was not found.");
        }

        if (reservation.Status == StockReservationStatus.Committed)
        {
            return InventoryResult<StockReservationDto>.Success(ToReservationDto(reservation));
        }

        if (reservation.Status != StockReservationStatus.Active)
        {
            return InventoryResult<StockReservationDto>.Failure(
                "reservation_not_active",
                "Only active reservation can be committed.");
        }

        var stockItem = await _dbContext.StockItems
            .FirstOrDefaultAsync(
                item =>
                    item.Sku == reservation.Sku &&
                    item.WarehouseId == reservation.WarehouseId &&
                    item.LocationId == reservation.LocationId,
                cancellationToken);

        if (stockItem is null)
        {
            return InventoryResult<StockReservationDto>.Failure(
                "stock_item_not_found",
                "Stock item was not found for the reservation.");
        }

        if (stockItem.ReservedQuantity < reservation.Quantity)
        {
            return InventoryResult<StockReservationDto>.Failure(
                "reservation_state_invalid",
                "Reserved quantity is lower than reservation quantity.");
        }

        if (stockItem.OnHandQuantity < reservation.Quantity)
        {
            return InventoryResult<StockReservationDto>.Failure(
                "insufficient_on_hand_stock",
                "On-hand quantity is lower than reservation quantity.");
        }

        var now = DateTimeOffset.UtcNow;

        stockItem.ReservedQuantity -= reservation.Quantity;
        stockItem.OnHandQuantity -= reservation.Quantity;
        stockItem.UpdatedAt = now;

        reservation.Status = StockReservationStatus.Committed;
        reservation.CommittedAt = now;

        var movement = new StockMovement
        {
            Id = Guid.NewGuid(),
            Sku = reservation.Sku,
            WarehouseId = reservation.WarehouseId,
            Warehouse = reservation.Warehouse,
            LocationId = reservation.LocationId,
            Location = reservation.Location,
            Type = StockMovementType.Shipment,
            Quantity = -reservation.Quantity,
            Reason = reservation.Reference,
            CreatedAt = now
        };

        _dbContext.StockMovements.Add(movement);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return InventoryResult<StockReservationDto>.Success(ToReservationDto(reservation));
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

    private static StockReservationDto ToReservationDto(StockReservation reservation)
    {
        return new StockReservationDto(
            reservation.Id,
            reservation.Sku,
            reservation.WarehouseId,
            reservation.LocationId,
            reservation.Quantity,
            reservation.Status.ToString(),
            reservation.Reference,
            reservation.CreatedAt,
            reservation.ReleasedAt,
            reservation.CommittedAt);
    }
}
