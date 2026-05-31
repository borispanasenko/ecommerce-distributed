namespace Inventory.Domain.Stock;

public sealed class StockItem
{
    public Guid Id { get; set; }

    public string Sku { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    public Warehouse Warehouse { get; set; } = null!;

    public Guid LocationId { get; set; }

    public StorageLocation Location { get; set; } = null!;

    public long OnHandQuantity { get; set; }

    public long ReservedQuantity { get; set; }

    public long AvailableQuantity => OnHandQuantity - ReservedQuantity;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
