namespace Inventory.Domain.Stock;

public sealed class Warehouse
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public List<StorageLocation> Locations { get; set; } = [];

    public List<StockItem> StockItems { get; set; } = [];

    public List<StockMovement> StockMovements { get; set; } = [];
}
