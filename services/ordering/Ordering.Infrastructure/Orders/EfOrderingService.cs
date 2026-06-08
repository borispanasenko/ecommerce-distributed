using Microsoft.EntityFrameworkCore;
using Ordering.Application.Orders;
using Ordering.Domain.Orders;
using Ordering.Infrastructure.Persistence;
using Ordering.Application.Inventory;
using Ordering.Application.Catalog;

namespace Ordering.Infrastructure.Orders;

public sealed class EfOrderingService : IOrderingService
{
    private readonly OrderingDbContext _dbContext;
    private readonly IInventoryClient _inventoryClient;

    private readonly ICatalogClient _catalogClient;

    public EfOrderingService(
        OrderingDbContext dbContext,
        IInventoryClient inventoryClient,
        ICatalogClient catalogClient)
    {
        _dbContext = dbContext;
        _inventoryClient = inventoryClient;
        _catalogClient = catalogClient;
    }

    public async Task<OrderingResult<OrderDetailsDto>> CreateOrderAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName))
        {
            return OrderingResult<OrderDetailsDto>.Failure(
                "customer_name_required",
                "Customer name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.CustomerEmail))
        {
            return OrderingResult<OrderDetailsDto>.Failure(
                "customer_email_required",
                "Customer email is required.");
        }

        if (request.Items.Count == 0)
        {
            return OrderingResult<OrderDetailsDto>.Failure(
                "order_items_required",
                "Order must contain at least one item.");
        }

        var normalizedItems = new List<ResolvedOrderItem>();

        foreach (var item in request.Items)
        {

            if (item.ProductVariantId == Guid.Empty)
            {
                return OrderingResult<OrderDetailsDto>.Failure(
                    "product_variant_id_required",
                    "Product variant id is required.");
            }

            if (item.Quantity <= 0)
            {
                return OrderingResult<OrderDetailsDto>.Failure(
                    "quantity_invalid",
                    "Quantity must be greater than zero.");
            }

            var snapshotResult = await _catalogClient.GetProductVariantSnapshotAsync(
                item.ProductVariantId,
                cancellationToken);

            if (!snapshotResult.IsSuccess)
            {
                return OrderingResult<OrderDetailsDto>.Failure(
                    snapshotResult.ErrorCode ?? "catalog_variant_snapshot_failed",
                    snapshotResult.ErrorMessage ?? "Catalog product variant snapshot request failed.");
            }

            var snapshot = snapshotResult.Value!;

            if (snapshot.PriceAmountMinor < 0)
            {
                return OrderingResult<OrderDetailsDto>.Failure(
                    "unit_price_invalid",
                    "Unit price cannot be negative.");
            }

            if (string.IsNullOrWhiteSpace(snapshot.Currency) || snapshot.Currency.Trim().Length != 3)
            {
                return OrderingResult<OrderDetailsDto>.Failure(
                    "currency_invalid",
                    "Currency must be a 3-letter code.");
            }

            normalizedItems.Add(new ResolvedOrderItem(
                ProductId: snapshot.ProductId,
                ProductVariantId: snapshot.ProductVariantId,
                Sku: snapshot.Sku.Trim().ToUpperInvariant(),
                ProductName: snapshot.ProductName.Trim(),
                VariantName: snapshot.VariantName.Trim(),
                UnitPriceAmountMinor: snapshot.PriceAmountMinor,
                Currency: snapshot.Currency.Trim().ToUpperInvariant(),
                Quantity: item.Quantity));
        }

        var currency = normalizedItems[0].Currency;

        if (normalizedItems.Any(item => item.Currency != currency))
        {
            return OrderingResult<OrderDetailsDto>.Failure(
                "mixed_currencies_not_supported",
                "All order items must use the same currency.");
        }

        var now = DateTimeOffset.UtcNow;

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerName = request.CustomerName.Trim(),
            CustomerEmail = request.CustomerEmail.Trim(),
            Status = OrderStatus.PendingPayment,
            Currency = currency,
            CreatedAt = now,
            UpdatedAt = now
        };

        var createdReservationIds = new List<Guid>();

        foreach (var item in normalizedItems)
        {
            var reservationResult = await _inventoryClient.AllocateStockAsync(
                new AllocateStockRequest(
                    Sku: item.Sku,
                    Quantity: item.Quantity,
                    Reference: $"ORDER-{order.Id}"),
                cancellationToken);

            if (!reservationResult.IsSuccess)
            {
                foreach (var reservationId in createdReservationIds)
                {
                    await _inventoryClient.ReleaseReservationAsync(reservationId, cancellationToken);
                }

                return OrderingResult<OrderDetailsDto>.Failure(
                    reservationResult.ErrorCode ?? "inventory_reservation_failed",
                    reservationResult.ErrorMessage ?? "Inventory reservation failed.");
            }

            var inventoryReservationId = reservationResult.Value!.Id;
            createdReservationIds.Add(inventoryReservationId);

            var lineTotal = item.UnitPriceAmountMinor * item.Quantity;

            order.Items.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Order = order,
                ProductId = item.ProductId,
                ProductVariantId = item.ProductVariantId,
                Sku = item.Sku,
                ProductName = item.ProductName,
                VariantName = item.VariantName,
                UnitPriceAmountMinor = item.UnitPriceAmountMinor,
                Currency = item.Currency,
                Quantity = item.Quantity,
                LineTotalAmountMinor = lineTotal,
                InventoryReservationId = inventoryReservationId
            });

            order.TotalAmountMinor += lineTotal;
        }

        try
        {
            _dbContext.Orders.Add(order);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            foreach (var reservationId in createdReservationIds)
            {
                await _inventoryClient.ReleaseReservationAsync(reservationId, cancellationToken);
            }

            throw;
        }

        return OrderingResult<OrderDetailsDto>.Success(ToDetailsDto(order));
    }

    public async Task<IReadOnlyList<OrderListItemDto>> GetOrdersAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .OrderByDescending(order => order.CreatedAt)
            .Select(order => new OrderListItemDto(
                order.Id,
                order.CustomerName,
                order.CustomerEmail,
                order.Status.ToString(),
                order.TotalAmountMinor,
                order.Currency,
                order.Items.Count,
                order.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<OrderDetailsDto?> GetOrderByIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.Orders
            .AsNoTracking()
            .AsSplitQuery()
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Id == orderId, cancellationToken);

        return order is null ? null : ToDetailsDto(order);
    }

    public async Task<OrderingResult<OrderDetailsDto>> CancelOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.Orders
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Id == orderId, cancellationToken);

        if (order is null)
        {
            return OrderingResult<OrderDetailsDto>.Failure(
                "order_not_found",
                "Order was not found.");
        }

        if (order.Status != OrderStatus.PendingPayment)
        {
            return OrderingResult<OrderDetailsDto>.Failure(
                "order_cannot_be_cancelled",
                "Only pending payment orders can be cancelled.");
        }

        foreach (var item in order.Items)
        {
            if (item.InventoryReservationId is null)
            {
                continue;
            }

            var releaseResult = await _inventoryClient.ReleaseReservationAsync(
                item.InventoryReservationId.Value,
                cancellationToken);

            if (!releaseResult.IsSuccess)
            {
                return OrderingResult<OrderDetailsDto>.Failure(
                    releaseResult.ErrorCode ?? "inventory_reservation_release_failed",
                    releaseResult.ErrorMessage ?? "Inventory reservation release failed.");
            }
        }

        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return OrderingResult<OrderDetailsDto>.Success(ToDetailsDto(order));
    }

    public async Task<OrderingResult<OrderDetailsDto>> MarkOrderPaidAsync(
    Guid orderId,
    CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.Orders
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Id == orderId, cancellationToken);

        if (order is null)
        {
            return OrderingResult<OrderDetailsDto>.Failure(
                "order_not_found",
                "Order was not found.");
        }

        if (order.Status != OrderStatus.PendingPayment)
        {
            return OrderingResult<OrderDetailsDto>.Failure(
                "order_cannot_be_marked_paid",
                "Only pending payment orders can be marked as paid.");
        }

        foreach (var item in order.Items)
        {
            if (item.InventoryReservationId is null)
            {
                continue;
            }

            var commitResult = await _inventoryClient.CommitReservationAsync(
                item.InventoryReservationId.Value,
                cancellationToken);

            if (!commitResult.IsSuccess)
            {
                return OrderingResult<OrderDetailsDto>.Failure(
                    commitResult.ErrorCode ?? "inventory_reservation_commit_failed",
                    commitResult.ErrorMessage ?? "Inventory reservation commit failed.");
            }
        }

        order.Status = OrderStatus.Paid;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return OrderingResult<OrderDetailsDto>.Success(ToDetailsDto(order));
    }

    public async Task<OrderingResult<OrderDetailsDto>> MarkOrderShippedAsync(
    Guid orderId,
    CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.Orders
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Id == orderId, cancellationToken);

        if (order is null)
        {
            return OrderingResult<OrderDetailsDto>.Failure(
                "order_not_found",
                "Order was not found.");
        }

        if (order.Status != OrderStatus.Paid)
        {
            return OrderingResult<OrderDetailsDto>.Failure(
                "order_cannot_be_marked_shipped",
                "Only paid orders can be marked as shipped.");
        }

        order.Status = OrderStatus.Shipped;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return OrderingResult<OrderDetailsDto>.Success(ToDetailsDto(order));
    }

    private static OrderDetailsDto ToDetailsDto(Order order)
    {
        return new OrderDetailsDto(
            order.Id,
            order.CustomerName,
            order.CustomerEmail,
            order.Status.ToString(),
            order.TotalAmountMinor,
            order.Currency,
            order.CreatedAt,
            order.UpdatedAt,
            order.Items
                .OrderBy(item => item.Sku)
                .Select(item => new OrderItemDto(
                    item.Id,
                    item.ProductId,
                    item.ProductVariantId,
                    item.Sku,
                    item.ProductName,
                    item.VariantName,
                    item.UnitPriceAmountMinor,
                    item.Currency,
                    item.Quantity,
                    item.LineTotalAmountMinor,
                    item.InventoryReservationId))
                .ToList());
    }

    private sealed record ResolvedOrderItem(
        Guid ProductId,
        Guid ProductVariantId,
        string Sku,
        string ProductName,
        string VariantName,
        long UnitPriceAmountMinor,
        string Currency,
        int Quantity);
}
