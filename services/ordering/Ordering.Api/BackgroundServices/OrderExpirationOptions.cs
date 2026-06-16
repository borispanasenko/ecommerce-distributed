namespace Ordering.Api.BackgroundServices;

public sealed class OrderExpirationOptions
{
    public const string SectionName = "OrderExpiration";

    public bool Enabled { get; set; } = true;

    public int PaymentTimeoutMinutes { get; set; } = 15;

    public int ScanIntervalSeconds { get; set; } = 60;

    public int BatchSize { get; set; } = 50;
}
