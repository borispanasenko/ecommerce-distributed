namespace Fulfillment.Application.Ordering;

public interface IOrderingClient
{
    Task<OrderingClientResult<OrderingOrderDto>> GetOrderByIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<OrderingClientResult> MarkOrderShippedAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);
}

public sealed record OrderingOrderDto(
    Guid Id,
    string Status);

public sealed record OrderingClientResult(
    bool IsSuccess,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static OrderingClientResult Success()
        => new(true, null, null);

    public static OrderingClientResult Failure(string errorCode, string errorMessage)
        => new(false, errorCode, errorMessage);
}

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