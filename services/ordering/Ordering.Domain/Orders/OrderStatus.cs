namespace Ordering.Domain.Orders;

public enum OrderStatus
{
    Draft = 1,
    PendingPayment = 2,
    Paid = 3,
    Cancelled = 4,
    Shipped = 5
}
