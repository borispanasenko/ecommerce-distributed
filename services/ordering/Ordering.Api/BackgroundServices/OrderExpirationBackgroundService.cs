using Microsoft.Extensions.Options;
using Ordering.Application.Orders;

namespace Ordering.Api.BackgroundServices;

public sealed class OrderExpirationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<OrderExpirationOptions> _options;
    private readonly ILogger<OrderExpirationBackgroundService> _logger;

    public OrderExpirationBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<OrderExpirationOptions> options,
        ILogger<OrderExpirationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var options = _options.CurrentValue;

            try
            {
                if (options.Enabled)
                {
                    await ExpirePendingPaymentOrdersAsync(options, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Order expiration worker failed.");
            }

            var scanIntervalSeconds = Math.Max(1, options.ScanIntervalSeconds);

            await Task.Delay(
                TimeSpan.FromSeconds(scanIntervalSeconds),
                stoppingToken);
        }
    }

    private async Task ExpirePendingPaymentOrdersAsync(
        OrderExpirationOptions options,
        CancellationToken cancellationToken)
    {
        var paymentTimeoutMinutes = Math.Max(1, options.PaymentTimeoutMinutes);
        var batchSize = Math.Clamp(options.BatchSize, 1, 500);

        var expiresBefore = DateTimeOffset.UtcNow.Subtract(
            TimeSpan.FromMinutes(paymentTimeoutMinutes));

        using var scope = _scopeFactory.CreateScope();

        var orderingService = scope.ServiceProvider.GetRequiredService<IOrderingService>();

        var result = await orderingService.ExpirePendingPaymentOrdersAsync(
            expiresBefore,
            batchSize,
            cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning(
                "Order expiration scan failed. ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}",
                result.ErrorCode,
                result.ErrorMessage);

            return;
        }

        var summary = result.Value!;

        if (summary.CheckedCount == 0)
        {
            return;
        }

        _logger.LogInformation(
            "Order expiration scan completed. Checked={CheckedCount}, Expired={ExpiredCount}, Failed={FailedCount}",
            summary.CheckedCount,
            summary.ExpiredCount,
            summary.FailedCount);
    }
}
