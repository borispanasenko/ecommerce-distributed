using Microsoft.EntityFrameworkCore;
using Payment.Application.Payments;
using Payment.Domain.Payments;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Payments;

public sealed class EfPaymentService : IPaymentService
{
    private readonly PaymentDbContext _dbContext;

    public EfPaymentService(PaymentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaymentResult<PaymentDetailsDto>> CreatePaymentAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.OrderId == Guid.Empty)
        {
            return PaymentResult<PaymentDetailsDto>.Failure(
                "order_id_required",
                "Order id is required.");
        }

        if (request.AmountMinor <= 0)
        {
            return PaymentResult<PaymentDetailsDto>.Failure(
                "amount_invalid",
                "Payment amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(request.Currency) || request.Currency.Trim().Length != 3)
        {
            return PaymentResult<PaymentDetailsDto>.Failure(
                "currency_invalid",
                "Currency must be a 3-letter code.");
        }

        if (string.IsNullOrWhiteSpace(request.Provider))
        {
            return PaymentResult<PaymentDetailsDto>.Failure(
                "provider_required",
                "Payment provider is required.");
        }

        var now = DateTimeOffset.UtcNow;

        var payment = new Domain.Payments.Payment
        {
            Id = Guid.NewGuid(),
            OrderId = request.OrderId,
            AmountMinor = request.AmountMinor,
            Currency = request.Currency.Trim().ToUpperInvariant(),
            Status = PaymentStatus.Pending,
            Provider = request.Provider.Trim(),
            ProviderReference = null,
            FailureReason = null,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Payments.Add(payment);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return PaymentResult<PaymentDetailsDto>.Success(ToDetailsDto(payment));
    }

    public async Task<IReadOnlyList<PaymentListItemDto>> GetPaymentsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payments
            .AsNoTracking()
            .OrderByDescending(payment => payment.CreatedAt)
            .Select(payment => new PaymentListItemDto(
                payment.Id,
                payment.OrderId,
                payment.AmountMinor,
                payment.Currency,
                payment.Status.ToString(),
                payment.Provider,
                payment.ProviderReference,
                payment.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<PaymentDetailsDto?> GetPaymentByIdAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        var payment = await _dbContext.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(payment => payment.Id == paymentId, cancellationToken);

        return payment is null ? null : ToDetailsDto(payment);
    }

    public async Task<PaymentResult<PaymentDetailsDto>> MarkPaymentSucceededAsync(
        Guid paymentId,
        CompletePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var payment = await _dbContext.Payments
            .FirstOrDefaultAsync(payment => payment.Id == paymentId, cancellationToken);

        if (payment is null)
        {
            return PaymentResult<PaymentDetailsDto>.Failure(
                "payment_not_found",
                "Payment was not found.");
        }

        if (payment.Status != PaymentStatus.Pending)
        {
            return PaymentResult<PaymentDetailsDto>.Failure(
                "payment_not_pending",
                "Only pending payment can be marked as succeeded.");
        }

        var now = DateTimeOffset.UtcNow;

        payment.Status = PaymentStatus.Succeeded;
        payment.ProviderReference = request.ProviderReference?.Trim();
        payment.SucceededAt = now;
        payment.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return PaymentResult<PaymentDetailsDto>.Success(ToDetailsDto(payment));
    }

    public async Task<PaymentResult<PaymentDetailsDto>> MarkPaymentFailedAsync(
        Guid paymentId,
        FailPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var payment = await _dbContext.Payments
            .FirstOrDefaultAsync(payment => payment.Id == paymentId, cancellationToken);

        if (payment is null)
        {
            return PaymentResult<PaymentDetailsDto>.Failure(
                "payment_not_found",
                "Payment was not found.");
        }

        if (payment.Status != PaymentStatus.Pending)
        {
            return PaymentResult<PaymentDetailsDto>.Failure(
                "payment_not_pending",
                "Only pending payment can be marked as failed.");
        }

        var now = DateTimeOffset.UtcNow;

        payment.Status = PaymentStatus.Failed;
        payment.FailureReason = request.FailureReason?.Trim();
        payment.FailedAt = now;
        payment.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return PaymentResult<PaymentDetailsDto>.Success(ToDetailsDto(payment));
    }

    private static PaymentDetailsDto ToDetailsDto(Domain.Payments.Payment payment)
    {
        return new PaymentDetailsDto(
            payment.Id,
            payment.OrderId,
            payment.AmountMinor,
            payment.Currency,
            payment.Status.ToString(),
            payment.Provider,
            payment.ProviderReference,
            payment.FailureReason,
            payment.CreatedAt,
            payment.UpdatedAt,
            payment.SucceededAt,
            payment.FailedAt,
            payment.CancelledAt);
    }
}
