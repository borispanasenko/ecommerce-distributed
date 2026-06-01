namespace Ordering.Domain.Orders;

public sealed class Order
{
    public Guid Id { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string CustomerEmail { get; set; } = string.Empty;

    public OrderStatus Status { get; set; }

    public long TotalAmountMinor { get; set; }

    public string Currency { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public List<OrderItem> Items { get; set; } = [];
}
