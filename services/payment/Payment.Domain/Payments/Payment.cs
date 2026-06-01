namespace Payment.Domain.Payments;

public sealed class Payment
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public long AmountMinor { get; set; }

    public string Currency { get; set; } = string.Empty;

    public PaymentStatus Status { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string? ProviderReference { get; set; }

    public string? FailureReason { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? SucceededAt { get; set; }

    public DateTimeOffset? FailedAt { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }
}
