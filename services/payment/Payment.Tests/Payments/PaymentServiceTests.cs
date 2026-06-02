using Microsoft.EntityFrameworkCore;
using Payment.Application.Payments;
using Payment.Infrastructure.Payments;
using Payment.Infrastructure.Persistence;

namespace Payment.Tests.Payments;

public sealed class PaymentServiceTests
{
    [Fact]
    public async Task CreatePaymentAsync_ShouldCreatePendingPayment()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfPaymentService(dbContext);

        var result = await service.CreatePaymentAsync(CreateValidPaymentRequest());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Pending", result.Value.Status);
        Assert.Equal(13800, result.Value.AmountMinor);
        Assert.Equal("USD", result.Value.Currency);
        Assert.Equal("Manual", result.Value.Provider);
        Assert.Null(result.Value.ProviderReference);
        Assert.Null(result.Value.FailureReason);

        var paymentExists = await dbContext.Payments
            .AnyAsync(payment => payment.Id == result.Value.Id);

        Assert.True(paymentExists);
    }

    [Fact]
    public async Task CreatePaymentAsync_ShouldNormalizeCurrencyAndProvider()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfPaymentService(dbContext);

        var result = await service.CreatePaymentAsync(new CreatePaymentRequest(
            OrderId: Guid.NewGuid(),
            AmountMinor: 13800,
            Currency: " usd ",
            Provider: " Manual "));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("USD", result.Value.Currency);
        Assert.Equal("Manual", result.Value.Provider);
    }

    [Fact]
    public async Task CreatePaymentAsync_ShouldRejectMissingOrderId()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfPaymentService(dbContext);

        var request = CreateValidPaymentRequest() with
        {
            OrderId = Guid.Empty
        };

        var result = await service.CreatePaymentAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("order_id_required", result.ErrorCode);
    }

    [Fact]
    public async Task CreatePaymentAsync_ShouldRejectInvalidAmount()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfPaymentService(dbContext);

        var request = CreateValidPaymentRequest() with
        {
            AmountMinor = 0
        };

        var result = await service.CreatePaymentAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("amount_invalid", result.ErrorCode);
    }

    [Fact]
    public async Task CreatePaymentAsync_ShouldRejectInvalidCurrency()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfPaymentService(dbContext);

        var request = CreateValidPaymentRequest() with
        {
            Currency = "US"
        };

        var result = await service.CreatePaymentAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("currency_invalid", result.ErrorCode);
    }

    [Fact]
    public async Task CreatePaymentAsync_ShouldRejectMissingProvider()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfPaymentService(dbContext);

        var request = CreateValidPaymentRequest() with
        {
            Provider = " "
        };

        var result = await service.CreatePaymentAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("provider_required", result.ErrorCode);
    }

    [Fact]
    public async Task GetPaymentsAsync_ShouldReturnPayments()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfPaymentService(dbContext);

        var createdPayment = await service.CreatePaymentAsync(CreateValidPaymentRequest());

        var payments = await service.GetPaymentsAsync();

        Assert.Single(payments);
        Assert.Equal(createdPayment.Value!.Id, payments[0].Id);
        Assert.Equal("Pending", payments[0].Status);
        Assert.Equal(13800, payments[0].AmountMinor);
        Assert.Equal("USD", payments[0].Currency);
    }

    [Fact]
    public async Task GetPaymentByIdAsync_ShouldReturnPaymentDetails()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfPaymentService(dbContext);

        var createdPayment = await service.CreatePaymentAsync(CreateValidPaymentRequest());

        var payment = await service.GetPaymentByIdAsync(createdPayment.Value!.Id);

        Assert.NotNull(payment);
        Assert.Equal(createdPayment.Value.Id, payment.Id);
        Assert.Equal("Pending", payment.Status);
        Assert.Equal("Manual", payment.Provider);
    }

    [Fact]
    public async Task GetPaymentByIdAsync_ShouldReturnNull_WhenPaymentDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfPaymentService(dbContext);

        var payment = await service.GetPaymentByIdAsync(Guid.NewGuid());

        Assert.Null(payment);
    }

    [Fact]
    public async Task MarkPaymentSucceededAsync_ShouldMarkPendingPaymentAsSucceeded()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfPaymentService(dbContext);

        var createdPayment = await service.CreatePaymentAsync(CreateValidPaymentRequest());

        var result = await service.MarkPaymentSucceededAsync(
            createdPayment.Value!.Id,
            new CompletePaymentRequest("MANUAL-APPROVED-001"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Succeeded", result.Value.Status);
        Assert.Equal("MANUAL-APPROVED-001", result.Value.ProviderReference);
        Assert.NotNull(result.Value.SucceededAt);
        Assert.Null(result.Value.FailedAt);
    }

    [Fact]
    public async Task MarkPaymentSucceededAsync_ShouldRejectMissingPayment()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfPaymentService(dbContext);

        var result = await service.MarkPaymentSucceededAsync(
            Guid.NewGuid(),
            new CompletePaymentRequest("MANUAL-APPROVED-001"));

        Assert.False(result.IsSuccess);
        Assert.Equal("payment_not_found", result.ErrorCode);
    }

    [Fact]
    public async Task MarkPaymentSucceededAsync_ShouldRejectAlreadySucceededPayment()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfPaymentService(dbContext);

        var createdPayment = await service.CreatePaymentAsync(CreateValidPaymentRequest());

        var firstResult = await service.MarkPaymentSucceededAsync(
            createdPayment.Value!.Id,
            new CompletePaymentRequest("MANUAL-APPROVED-001"));

        var secondResult = await service.MarkPaymentSucceededAsync(
            createdPayment.Value.Id,
            new CompletePaymentRequest("MANUAL-APPROVED-002"));

        Assert.True(firstResult.IsSuccess);
        Assert.False(secondResult.IsSuccess);
        Assert.Equal("payment_not_pending", secondResult.ErrorCode);
    }

    [Fact]
    public async Task MarkPaymentFailedAsync_ShouldMarkPendingPaymentAsFailed()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfPaymentService(dbContext);

        var createdPayment = await service.CreatePaymentAsync(CreateValidPaymentRequest());

        var result = await service.MarkPaymentFailedAsync(
            createdPayment.Value!.Id,
            new FailPaymentRequest("Card declined"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Failed", result.Value.Status);
        Assert.Equal("Card declined", result.Value.FailureReason);
        Assert.NotNull(result.Value.FailedAt);
        Assert.Null(result.Value.SucceededAt);
    }

    [Fact]
    public async Task MarkPaymentFailedAsync_ShouldRejectMissingPayment()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfPaymentService(dbContext);

        var result = await service.MarkPaymentFailedAsync(
            Guid.NewGuid(),
            new FailPaymentRequest("Card declined"));

        Assert.False(result.IsSuccess);
        Assert.Equal("payment_not_found", result.ErrorCode);
    }

    [Fact]
    public async Task MarkPaymentFailedAsync_ShouldRejectSucceededPayment()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfPaymentService(dbContext);

        var createdPayment = await service.CreatePaymentAsync(CreateValidPaymentRequest());

        var succeededResult = await service.MarkPaymentSucceededAsync(
            createdPayment.Value!.Id,
            new CompletePaymentRequest("MANUAL-APPROVED-001"));

        var failedResult = await service.MarkPaymentFailedAsync(
            createdPayment.Value.Id,
            new FailPaymentRequest("Card declined"));

        Assert.True(succeededResult.IsSuccess);
        Assert.False(failedResult.IsSuccess);
        Assert.Equal("payment_not_pending", failedResult.ErrorCode);
    }

    private static CreatePaymentRequest CreateValidPaymentRequest()
    {
        return new CreatePaymentRequest(
            OrderId: Guid.NewGuid(),
            AmountMinor: 13800,
            Currency: "USD",
            Provider: "Manual");
    }

    private static PaymentDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new PaymentDbContext(options);
    }
}