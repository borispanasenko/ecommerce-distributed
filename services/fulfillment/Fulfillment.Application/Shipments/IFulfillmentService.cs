namespace Fulfillment.Application.Shipments;

public interface IFulfillmentService
{
    Task<FulfillmentResult<ShipmentDetailsDto>> CreateShipmentAsync(
        CreateShipmentRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShipmentListItemDto>> GetShipmentsAsync(
        CancellationToken cancellationToken = default);

    Task<ShipmentDetailsDto?> GetShipmentByIdAsync(
        Guid shipmentId,
        CancellationToken cancellationToken = default);

    Task<FulfillmentResult<ShipmentDetailsDto>> ShipShipmentAsync(
        Guid shipmentId,
        ShipShipmentRequest request,
        CancellationToken cancellationToken = default);

    Task<FulfillmentResult<ShipmentDetailsDto>> CancelShipmentAsync(
        Guid shipmentId,
        CancellationToken cancellationToken = default);
}

public sealed record CreateShipmentRequest(
    Guid OrderId,
    string? Carrier,
    string? TrackingNumber);

public sealed record ShipShipmentRequest(
    string? Carrier,
    string? TrackingNumber);

public sealed record ShipmentListItemDto(
    Guid Id,
    Guid OrderId,
    string Status,
    string? Carrier,
    string? TrackingNumber,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ShippedAt,
    DateTimeOffset? CancelledAt);

public sealed record ShipmentDetailsDto(
    Guid Id,
    Guid OrderId,
    string Status,
    string? Carrier,
    string? TrackingNumber,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ShippedAt,
    DateTimeOffset? CancelledAt);

public sealed record FulfillmentResult<T>(
    bool IsSuccess,
    T? Value,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static FulfillmentResult<T> Success(T value)
        => new(true, value, null, null);

    public static FulfillmentResult<T> Failure(string errorCode, string errorMessage)
        => new(false, default, errorCode, errorMessage);
}
