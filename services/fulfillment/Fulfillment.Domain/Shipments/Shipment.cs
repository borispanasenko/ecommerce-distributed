namespace Fulfillment.Domain.Shipments;

public sealed class Shipment
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public ShipmentStatus Status { get; set; }

    public string? Carrier { get; set; }

    public string? TrackingNumber { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? ShippedAt { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }
}
