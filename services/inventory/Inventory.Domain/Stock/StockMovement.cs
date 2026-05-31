namespace Inventory.Domain.Stock;

public sealed class StockMovement
{
    public Guid Id { get; set; }

    public string Sku { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    public Warehouse Warehouse { get; set; } = null!;

    public Guid LocationId { get; set; }

    public StorageLocation Location { get; set; } = null!;

    public StockMovementType Type { get; set; }

    public long Quantity { get; set; }

    public string? Reason { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
