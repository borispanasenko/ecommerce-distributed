using Fulfillment.Application.Ordering;
using Fulfillment.Application.Shipments;
using Fulfillment.Domain.Shipments;
using Fulfillment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fulfillment.Infrastructure.Shipments;

public sealed class EfFulfillmentService : IFulfillmentService
{
    private readonly FulfillmentDbContext _dbContext;
    private readonly IOrderingClient _orderingClient;

    public EfFulfillmentService(
        FulfillmentDbContext dbContext,
        IOrderingClient orderingClient)
    {
        _dbContext = dbContext;
        _orderingClient = orderingClient;
    }

    public async Task<FulfillmentResult<ShipmentDetailsDto>> CreateShipmentAsync(
        CreateShipmentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.OrderId == Guid.Empty)
        {
            return FulfillmentResult<ShipmentDetailsDto>.Failure(
                "order_id_required",
                "Order id is required.");
        }

        var orderResult = await _orderingClient.GetOrderByIdAsync(
            request.OrderId,
            cancellationToken);

        if (!orderResult.IsSuccess)
        {
            return FulfillmentResult<ShipmentDetailsDto>.Failure(
                orderResult.ErrorCode ?? "ordering_get_order_failed",
                orderResult.ErrorMessage ?? "Ordering get-order request failed.");
        }

        if (!string.Equals(orderResult.Value!.Status, "Paid", StringComparison.Ordinal))
        {
            return FulfillmentResult<ShipmentDetailsDto>.Failure(
                "order_not_ready_for_shipment",
                "Only paid orders can have shipments.");
        }

        var now = DateTimeOffset.UtcNow;

        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = request.OrderId,
            Status = ShipmentStatus.Pending,
            Carrier = NormalizeOptionalText(request.Carrier),
            TrackingNumber = NormalizeOptionalText(request.TrackingNumber),
            CreatedAt = now,
            UpdatedAt = now,
            ShippedAt = null,
            CancelledAt = null
        };

        _dbContext.Shipments.Add(shipment);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return FulfillmentResult<ShipmentDetailsDto>.Success(ToDetailsDto(shipment));
    }

    public async Task<IReadOnlyList<ShipmentListItemDto>> GetShipmentsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Shipments
            .AsNoTracking()
            .OrderByDescending(shipment => shipment.CreatedAt)
            .Select(shipment => new ShipmentListItemDto(
                shipment.Id,
                shipment.OrderId,
                shipment.Status.ToString(),
                shipment.Carrier,
                shipment.TrackingNumber,
                shipment.CreatedAt,
                shipment.UpdatedAt,
                shipment.ShippedAt,
                shipment.CancelledAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<ShipmentDetailsDto?> GetShipmentByIdAsync(
        Guid shipmentId,
        CancellationToken cancellationToken = default)
    {
        var shipment = await _dbContext.Shipments
            .AsNoTracking()
            .FirstOrDefaultAsync(shipment => shipment.Id == shipmentId, cancellationToken);

        return shipment is null ? null : ToDetailsDto(shipment);
    }

    public async Task<FulfillmentResult<ShipmentDetailsDto>> ShipShipmentAsync(
        Guid shipmentId,
        ShipShipmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var shipment = await _dbContext.Shipments
            .FirstOrDefaultAsync(shipment => shipment.Id == shipmentId, cancellationToken);

        if (shipment is null)
        {
            return FulfillmentResult<ShipmentDetailsDto>.Failure(
                "shipment_not_found",
                "Shipment was not found.");
        }

        if (shipment.Status != ShipmentStatus.Pending)
        {
            return FulfillmentResult<ShipmentDetailsDto>.Failure(
                "shipment_cannot_be_shipped",
                "Only pending shipments can be shipped.");
        }

        var markOrderShippedResult = await _orderingClient.MarkOrderShippedAsync(
            shipment.OrderId,
            cancellationToken);

        if (!markOrderShippedResult.IsSuccess)
        {
            return FulfillmentResult<ShipmentDetailsDto>.Failure(
                markOrderShippedResult.ErrorCode ?? "ordering_mark_shipped_failed",
                markOrderShippedResult.ErrorMessage ?? "Ordering mark-shipped request failed.");
        }

        var now = DateTimeOffset.UtcNow;

        shipment.Status = ShipmentStatus.Shipped;
        shipment.Carrier = NormalizeOptionalText(request.Carrier) ?? shipment.Carrier;
        shipment.TrackingNumber = NormalizeOptionalText(request.TrackingNumber) ?? shipment.TrackingNumber;
        shipment.ShippedAt = now;
        shipment.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return FulfillmentResult<ShipmentDetailsDto>.Success(ToDetailsDto(shipment));
    }

    public async Task<FulfillmentResult<ShipmentDetailsDto>> CancelShipmentAsync(
        Guid shipmentId,
        CancellationToken cancellationToken = default)
    {
        var shipment = await _dbContext.Shipments
            .FirstOrDefaultAsync(shipment => shipment.Id == shipmentId, cancellationToken);

        if (shipment is null)
        {
            return FulfillmentResult<ShipmentDetailsDto>.Failure(
                "shipment_not_found",
                "Shipment was not found.");
        }

        if (shipment.Status != ShipmentStatus.Pending)
        {
            return FulfillmentResult<ShipmentDetailsDto>.Failure(
                "shipment_cannot_be_cancelled",
                "Only pending shipments can be cancelled.");
        }

        var now = DateTimeOffset.UtcNow;

        shipment.Status = ShipmentStatus.Cancelled;
        shipment.CancelledAt = now;
        shipment.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return FulfillmentResult<ShipmentDetailsDto>.Success(ToDetailsDto(shipment));
    }

    private static ShipmentDetailsDto ToDetailsDto(Shipment shipment)
    {
        return new ShipmentDetailsDto(
            shipment.Id,
            shipment.OrderId,
            shipment.Status.ToString(),
            shipment.Carrier,
            shipment.TrackingNumber,
            shipment.CreatedAt,
            shipment.UpdatedAt,
            shipment.ShippedAt,
            shipment.CancelledAt);
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
