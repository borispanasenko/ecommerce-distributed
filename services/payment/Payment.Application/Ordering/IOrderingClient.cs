namespace Payment.Application.Ordering;

public interface IOrderingClient
{
    Task<OrderingClientResult<OrderDetailsDto>> MarkOrderPaidAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);
}

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

public sealed record OrderingClientResult<T>(
    bool IsSuccess,
    T? Value,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static OrderingClientResult<T> Success(T value)
        => new(true, value, null, null);

    public static OrderingClientResult<T> Failure(string errorCode, string errorMessage)
        => new(false, default, errorCode, errorMessage);
}
