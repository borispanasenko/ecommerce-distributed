using Microsoft.EntityFrameworkCore;
using Ordering.Application.Orders;
using Ordering.Domain.Orders;
using Ordering.Infrastructure.Persistence;

namespace Ordering.Infrastructure.Orders;

public sealed class EfOrderingService : IOrderingService
{
    private readonly OrderingDbContext _dbContext;

    public EfOrderingService(OrderingDbContext dbContext)
    {
        _dbContext = dbContext;
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

        var normalizedItems = new List<CreateOrderItemRequest>();

        foreach (var item in request.Items)
        {
            if (item.ProductId == Guid.Empty)
            {
                return OrderingResult<OrderDetailsDto>.Failure(
                    "product_id_required",
                    "Product id is required.");
            }

            if (item.ProductVariantId == Guid.Empty)
            {
                return OrderingResult<OrderDetailsDto>.Failure(
                    "product_variant_id_required",
                    "Product variant id is required.");
            }

            if (string.IsNullOrWhiteSpace(item.Sku))
            {
                return OrderingResult<OrderDetailsDto>.Failure(
                    "sku_required",
                    "SKU is required.");
            }

            if (string.IsNullOrWhiteSpace(item.ProductName))
            {
                return OrderingResult<OrderDetailsDto>.Failure(
                    "product_name_required",
                    "Product name is required.");
            }

            if (string.IsNullOrWhiteSpace(item.VariantName))
            {
                return OrderingResult<OrderDetailsDto>.Failure(
                    "variant_name_required",
                    "Variant name is required.");
            }

            if (item.UnitPriceAmountMinor < 0)
            {
                return OrderingResult<OrderDetailsDto>.Failure(
                    "unit_price_invalid",
                    "Unit price cannot be negative.");
            }

            if (item.Quantity <= 0)
            {
                return OrderingResult<OrderDetailsDto>.Failure(
                    "quantity_invalid",
                    "Quantity must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(item.Currency) || item.Currency.Trim().Length != 3)
            {
                return OrderingResult<OrderDetailsDto>.Failure(
                    "currency_invalid",
                    "Currency must be a 3-letter code.");
            }

            normalizedItems.Add(item with
            {
                Sku = item.Sku.Trim().ToUpperInvariant(),
                ProductName = item.ProductName.Trim(),
                VariantName = item.VariantName.Trim(),
                Currency = item.Currency.Trim().ToUpperInvariant()
            });
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

        foreach (var item in normalizedItems)
        {
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
                InventoryReservationId = null
            });

            order.TotalAmountMinor += lineTotal;
        }

        _dbContext.Orders.Add(order);

        await _dbContext.SaveChangesAsync(cancellationToken);

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
}
