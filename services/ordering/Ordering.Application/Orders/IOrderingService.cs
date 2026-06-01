namespace Ordering.Application.Orders;

public interface IOrderingService
{
    Task<OrderingResult<OrderDetailsDto>> CreateOrderAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderListItemDto>> GetOrdersAsync(
        CancellationToken cancellationToken = default);

    Task<OrderDetailsDto?> GetOrderByIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);
}

public sealed record CreateOrderRequest(
    string CustomerName,
    string CustomerEmail,
    IReadOnlyCollection<CreateOrderItemRequest> Items);

public sealed record CreateOrderItemRequest(
    Guid ProductId,
    Guid ProductVariantId,
    string Sku,
    string ProductName,
    string VariantName,
    long UnitPriceAmountMinor,
    string Currency,
    int Quantity);

public sealed record OrderListItemDto(
    Guid Id,
    string CustomerName,
    string CustomerEmail,
    string Status,
    long TotalAmountMinor,
    string Currency,
    int ItemCount,
    DateTimeOffset CreatedAt);

public sealed record OrderDetailsDto(
    Guid Id,
    string CustomerName,
    string CustomerEmail,
    string Status,
    long TotalAmountMinor,
    string Currency,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<OrderItemDto> Items);

public sealed record OrderItemDto(
    Guid Id,
    Guid ProductId,
    Guid ProductVariantId,
    string Sku,
    string ProductName,
    string VariantName,
    long UnitPriceAmountMinor,
    string Currency,
    int Quantity,
    long LineTotalAmountMinor,
    Guid? InventoryReservationId);

public sealed record OrderingResult<T>(
    bool IsSuccess,
    T? Value,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static OrderingResult<T> Success(T value)
        => new(true, value, null, null);

    public static OrderingResult<T> Failure(string errorCode, string errorMessage)
        => new(false, default, errorCode, errorMessage);
}
