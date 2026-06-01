namespace Ordering.Domain.Orders;

public sealed class OrderItem
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Order Order { get; set; } = null!;

    public Guid ProductId { get; set; }

    public Guid ProductVariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string VariantName { get; set; } = string.Empty;

    public long UnitPriceAmountMinor { get; set; }

    public string Currency { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public long LineTotalAmountMinor { get; set; }

    public Guid? InventoryReservationId { get; set; }
}
