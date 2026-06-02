namespace Payment.Application.Payments;

public interface IPaymentService
{
    Task<PaymentResult<PaymentDetailsDto>> CreatePaymentAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentListItemDto>> GetPaymentsAsync(
        CancellationToken cancellationToken = default);

    Task<PaymentDetailsDto?> GetPaymentByIdAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default);

    Task<PaymentResult<PaymentDetailsDto>> MarkPaymentSucceededAsync(
        Guid paymentId,
        CompletePaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<PaymentResult<PaymentDetailsDto>> MarkPaymentFailedAsync(
        Guid paymentId,
        FailPaymentRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record CreatePaymentRequest(
    Guid OrderId,
    long AmountMinor,
    string Currency,
    string Provider);

public sealed record CompletePaymentRequest(
    string? ProviderReference);

public sealed record FailPaymentRequest(
    string? FailureReason);

public sealed record PaymentListItemDto(
    Guid Id,
    Guid OrderId,
    long AmountMinor,
    string Currency,
    string Status,
    string Provider,
    string? ProviderReference,
    DateTimeOffset CreatedAt);

public sealed record PaymentDetailsDto(
    Guid Id,
    Guid OrderId,
    long AmountMinor,
    string Currency,
    string Status,
    string Provider,
    string? ProviderReference,
    string? FailureReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? SucceededAt,
    DateTimeOffset? FailedAt,
    DateTimeOffset? CancelledAt);

public sealed record PaymentResult<T>(
    bool IsSuccess,
    T? Value,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static PaymentResult<T> Success(T value)
        => new(true, value, null, null);

    public static PaymentResult<T> Failure(string errorCode, string errorMessage)
        => new(false, default, errorCode, errorMessage);
}
