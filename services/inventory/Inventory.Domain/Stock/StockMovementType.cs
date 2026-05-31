namespace Inventory.Domain.Stock;

public enum StockMovementType
{
    Receipt = 1,
    Adjustment = 2,
    Shipment = 3,
    Reservation = 4,
    Release = 5
}
