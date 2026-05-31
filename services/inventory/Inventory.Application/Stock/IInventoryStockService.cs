namespace Inventory.Application.Stock;

public interface IInventoryStockService
{
    Task<IReadOnlyList<WarehouseDto>> GetWarehousesAsync(
        CancellationToken cancellationToken = default);

    Task<InventoryResult<WarehouseDto>> CreateWarehouseAsync(
        CreateWarehouseRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StorageLocationDto>> GetLocationsAsync(
        Guid? warehouseId = null,
        CancellationToken cancellationToken = default);

    Task<InventoryResult<StorageLocationDto>> CreateLocationAsync(
        CreateLocationRequest request,
        CancellationToken cancellationToken = default);

    Task<InventoryResult<StockReceiptDto>> ReceiveStockAsync(
        ReceiveStockRequest request,
        CancellationToken cancellationToken = default);

    Task<InventoryResult<StockReservationDto>> ReserveStockAsync(
        CreateStockReservationRequest request,
        CancellationToken cancellationToken = default);

    Task<InventoryResult<StockReservationDto>> ReleaseReservationAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default);

    Task<InventoryResult<StockReservationDto>> CommitReservationAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default);

    Task<StockSummaryDto?> GetStockBySkuAsync(
        string sku,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockMovementDto>> GetStockMovementsAsync(
        string? sku = null,
        int limit = 100,
        CancellationToken cancellationToken = default);
}

public sealed record CreateWarehouseRequest(
    string Code,
    string Name);

public sealed record WarehouseDto(
    Guid Id,
    string Code,
    string Name,
    bool IsActive);

public sealed record CreateLocationRequest(
    Guid WarehouseId,
    string Code);

public sealed record StorageLocationDto(
    Guid Id,
    Guid WarehouseId,
    string Code,
    bool IsActive);

public sealed record ReceiveStockRequest(
    string Sku,
    Guid WarehouseId,
    Guid LocationId,
    long Quantity,
    string? Reason);

public sealed record StockReceiptDto(
    string Sku,
    Guid WarehouseId,
    Guid LocationId,
    long ReceivedQuantity,
    long OnHandQuantity,
    long ReservedQuantity,
    long AvailableQuantity);

public sealed record StockSummaryDto(
    string Sku,
    long TotalOnHandQuantity,
    long TotalReservedQuantity,
    long TotalAvailableQuantity,
    IReadOnlyList<StockLocationBalanceDto> Locations);

public sealed record StockLocationBalanceDto(
    Guid WarehouseId,
    string WarehouseCode,
    Guid LocationId,
    string LocationCode,
    long OnHandQuantity,
    long ReservedQuantity,
    long AvailableQuantity);

public sealed record StockMovementDto(
    Guid Id,
    string Sku,
    Guid WarehouseId,
    string WarehouseCode,
    Guid LocationId,
    string LocationCode,
    string Type,
    long Quantity,
    string? Reason,
    DateTimeOffset CreatedAt);

public sealed record CreateStockReservationRequest(
    string Sku,
    Guid WarehouseId,
    Guid LocationId,
    long Quantity,
    string? Reference);

public sealed record StockReservationDto(
    Guid Id,
    string Sku,
    Guid WarehouseId,
    Guid LocationId,
    long Quantity,
    string Status,
    string? Reference,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReleasedAt,
    DateTimeOffset? CommittedAt);

public sealed record InventoryResult<T>(
    bool IsSuccess,
    T? Value,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static InventoryResult<T> Success(T value)
        => new(true, value, null, null);

    public static InventoryResult<T> Failure(string errorCode, string errorMessage)
        => new(false, default, errorCode, errorMessage);
}
