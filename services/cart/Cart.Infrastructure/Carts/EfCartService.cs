using Cart.Application.Carts;
using Cart.Domain.Carts;
using Cart.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cart.Infrastructure.Carts;

public sealed class EfCartService : ICartService
{
    private readonly CartDbContext _dbContext;

    public EfCartService(CartDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CartResult<CartDto>> CreateCartAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var cart = new ShoppingCart
        {
            Id = Guid.NewGuid(),
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Carts.Add(cart);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return CartResult<CartDto>.Success(ToDto(cart));
    }

    public async Task<CartDto?> GetCartByIdAsync(
        Guid cartId,
        CancellationToken cancellationToken = default)
    {
        var cart = await GetCartEntityAsync(cartId, cancellationToken);

        return cart is null ? null : ToDto(cart);
    }

    public async Task<CartResult<CartDto>> AddItemAsync(
        Guid cartId,
        AddCartItemRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ProductVariantId == Guid.Empty)
        {
            return CartResult<CartDto>.Failure(
                "product_variant_id_required",
                "Product variant id is required.");
        }

        if (request.Quantity <= 0)
        {
            return CartResult<CartDto>.Failure(
                "quantity_invalid",
                "Quantity must be greater than zero.");
        }

        var cart = await _dbContext.Carts
            .FirstOrDefaultAsync(cart => cart.Id == cartId, cancellationToken);

        if (cart is null)
        {
            return CartResult<CartDto>.Failure(
                "cart_not_found",
                "Cart was not found.");
        }

        var now = DateTimeOffset.UtcNow;

        var existingItem = await _dbContext.CartItems
            .FirstOrDefaultAsync(
                item => item.CartId == cartId &&
                        item.ProductVariantId == request.ProductVariantId,
                cancellationToken);

        if (existingItem is not null)
        {
            existingItem.Quantity += request.Quantity;
            existingItem.UpdatedAt = now;
        }
        else
        {
            _dbContext.CartItems.Add(new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                ProductVariantId = request.ProductVariantId,
                Quantity = request.Quantity,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        cart.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var updatedCart = await GetCartEntityAsync(cartId, cancellationToken);

        return CartResult<CartDto>.Success(ToDto(updatedCart!));
    }

    public async Task<CartResult<CartDto>> UpdateItemAsync(
        Guid cartId,
        Guid productVariantId,
        UpdateCartItemRequest request,
        CancellationToken cancellationToken = default)
    {
        if (productVariantId == Guid.Empty)
        {
            return CartResult<CartDto>.Failure(
                "product_variant_id_required",
                "Product variant id is required.");
        }

        if (request.Quantity <= 0)
        {
            return CartResult<CartDto>.Failure(
                "quantity_invalid",
                "Quantity must be greater than zero.");
        }

        var cart = await _dbContext.Carts
            .FirstOrDefaultAsync(cart => cart.Id == cartId, cancellationToken);

        if (cart is null)
        {
            return CartResult<CartDto>.Failure(
                "cart_not_found",
                "Cart was not found.");
        }

        var item = await _dbContext.CartItems
            .FirstOrDefaultAsync(
                item => item.CartId == cartId &&
                        item.ProductVariantId == productVariantId,
                cancellationToken);

        if (item is null)
        {
            return CartResult<CartDto>.Failure(
                "cart_item_not_found",
                "Cart item was not found.");
        }

        var now = DateTimeOffset.UtcNow;

        item.Quantity = request.Quantity;
        item.UpdatedAt = now;
        cart.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var updatedCart = await GetCartEntityAsync(cartId, cancellationToken);

        return CartResult<CartDto>.Success(ToDto(updatedCart!));
    }

    public async Task<CartResult<CartDto>> RemoveItemAsync(
        Guid cartId,
        Guid productVariantId,
        CancellationToken cancellationToken = default)
    {
        if (productVariantId == Guid.Empty)
        {
            return CartResult<CartDto>.Failure(
                "product_variant_id_required",
                "Product variant id is required.");
        }

        var cart = await _dbContext.Carts
            .FirstOrDefaultAsync(cart => cart.Id == cartId, cancellationToken);

        if (cart is null)
        {
            return CartResult<CartDto>.Failure(
                "cart_not_found",
                "Cart was not found.");
        }

        var item = await _dbContext.CartItems
            .FirstOrDefaultAsync(
                item => item.CartId == cartId &&
                        item.ProductVariantId == productVariantId,
                cancellationToken);

        if (item is null)
        {
            return CartResult<CartDto>.Failure(
                "cart_item_not_found",
                "Cart item was not found.");
        }

        _dbContext.CartItems.Remove(item);
        cart.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var updatedCart = await GetCartEntityAsync(cartId, cancellationToken);

        return CartResult<CartDto>.Success(ToDto(updatedCart!));
    }

    public async Task<CartResult<CartDto>> ClearCartAsync(
        Guid cartId,
        CancellationToken cancellationToken = default)
    {
        var cart = await _dbContext.Carts
            .FirstOrDefaultAsync(cart => cart.Id == cartId, cancellationToken);

        if (cart is null)
        {
            return CartResult<CartDto>.Failure(
                "cart_not_found",
                "Cart was not found.");
        }

        var items = await _dbContext.CartItems
            .Where(item => item.CartId == cartId)
            .ToListAsync(cancellationToken);

        _dbContext.CartItems.RemoveRange(items);
        cart.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var updatedCart = await GetCartEntityAsync(cartId, cancellationToken);

        return CartResult<CartDto>.Success(ToDto(updatedCart!));
    }

    private Task<ShoppingCart?> GetCartEntityAsync(
        Guid cartId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Carts
            .Include(cart => cart.Items)
            .FirstOrDefaultAsync(cart => cart.Id == cartId, cancellationToken);
    }

    private static CartDto ToDto(ShoppingCart cart)
    {
        return new CartDto(
            Id: cart.Id,
            CreatedAt: cart.CreatedAt,
            UpdatedAt: cart.UpdatedAt,
            Items: cart.Items
                .OrderBy(item => item.CreatedAt)
                .Select(item => new CartItemDto(
                    Id: item.Id,
                    ProductVariantId: item.ProductVariantId,
                    Quantity: item.Quantity,
                    CreatedAt: item.CreatedAt,
                    UpdatedAt: item.UpdatedAt))
                .ToList());
    }
}
