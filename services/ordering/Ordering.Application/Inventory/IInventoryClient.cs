namespace Ordering.Application.Inventory;

public interface IInventoryClient
{
    Task<InventoryClientResult<InventoryReservationDto>> AllocateStockAsync(
        AllocateStockRequest request,
        CancellationToken cancellationToken = default);

    Task<InventoryClientResult<InventoryReservationDto>> ReleaseReservationAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default);

    Task<InventoryClientResult<InventoryReservationDto>> CommitReservationAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default);
}

public sealed record AllocateStockRequest(
    string Sku,
    long Quantity,
    string? Reference);

public sealed record InventoryReservationDto(
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

public sealed record InventoryClientResult<T>(
    bool IsSuccess,
    T? Value,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static InventoryClientResult<T> Success(T value)
        => new(true, value, null, null);

    public static InventoryClientResult<T> Failure(string errorCode, string errorMessage)
        => new(false, default, errorCode, errorMessage);
}
