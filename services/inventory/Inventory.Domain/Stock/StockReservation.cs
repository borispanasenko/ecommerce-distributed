namespace Inventory.Domain.Stock;

public sealed class StockReservation
{
    public Guid Id { get; set; }

    public string Sku { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    public Warehouse Warehouse { get; set; } = null!;

    public Guid LocationId { get; set; }

    public StorageLocation Location { get; set; } = null!;

    public long Quantity { get; set; }

    public StockReservationStatus Status { get; set; }

    public string? Reference { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ReleasedAt { get; set; }

    public DateTimeOffset? CommittedAt { get; set; }
}
