namespace Fulfillment.Application.Ordering;

public interface IOrderingClient
{
    Task<OrderingClientResult> MarkOrderShippedAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);
}

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
