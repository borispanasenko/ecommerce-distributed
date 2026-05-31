namespace Inventory.Domain.Stock;

public sealed class StorageLocation
{
    public Guid Id { get; set; }

    public Guid WarehouseId { get; set; }

    public Warehouse Warehouse { get; set; } = null!;

    public string Code { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public List<StockItem> StockItems { get; set; } = [];

    public List<StockMovement> StockMovements { get; set; } = [];
    
    public List<StockReservation> StockReservations { get; set; } = [];
}
