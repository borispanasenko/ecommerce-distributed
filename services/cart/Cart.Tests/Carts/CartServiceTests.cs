using Cart.Application.Carts;
using Cart.Infrastructure.Carts;
using Cart.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cart.Tests.Carts;

public sealed class CartServiceTests
{
    [Fact]
    public async Task CreateCartAsync_ShouldCreateEmptyCart()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfCartService(dbContext);

        var result = await service.CreateCartAsync();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.Items);

        var cartExists = await dbContext.Carts.AnyAsync(cart => cart.Id == result.Value.Id);

        Assert.True(cartExists);
    }

    [Fact]
    public async Task AddItemAsync_ShouldAddItemToCart()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfCartService(dbContext);

        var cart = await service.CreateCartAsync();
        var variantId = Guid.NewGuid();

        var result = await service.AddItemAsync(
            cart.Value!.Id,
            new AddCartItemRequest(
                ProductVariantId: variantId,
                Quantity: 2));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Items);
        Assert.Equal(variantId, result.Value.Items[0].ProductVariantId);
        Assert.Equal(2, result.Value.Items[0].Quantity);
    }

    [Fact]
    public async Task AddItemAsync_ShouldIncreaseQuantity_WhenItemAlreadyExists()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfCartService(dbContext);

        var cart = await service.CreateCartAsync();
        var variantId = Guid.NewGuid();

        await service.AddItemAsync(cart.Value!.Id, new AddCartItemRequest(variantId, 2));

        var result = await service.AddItemAsync(cart.Value.Id, new AddCartItemRequest(variantId, 3));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(5, result.Value.Items[0].Quantity);
    }

    [Fact]
    public async Task UpdateItemAsync_ShouldUpdateQuantity()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfCartService(dbContext);

        var cart = await service.CreateCartAsync();
        var variantId = Guid.NewGuid();

        await service.AddItemAsync(cart.Value!.Id, new AddCartItemRequest(variantId, 2));

        var result = await service.UpdateItemAsync(
            cart.Value.Id,
            variantId,
            new UpdateCartItemRequest(5));

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value!.Items[0].Quantity);
    }

    [Fact]
    public async Task RemoveItemAsync_ShouldRemoveItem()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfCartService(dbContext);

        var cart = await service.CreateCartAsync();
        var variantId = Guid.NewGuid();

        await service.AddItemAsync(cart.Value!.Id, new AddCartItemRequest(variantId, 2));

        var result = await service.RemoveItemAsync(cart.Value.Id, variantId);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
    }

    [Fact]
    public async Task ClearCartAsync_ShouldRemoveAllItems()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfCartService(dbContext);

        var cart = await service.CreateCartAsync();

        await service.AddItemAsync(cart.Value!.Id, new AddCartItemRequest(Guid.NewGuid(), 2));
        await service.AddItemAsync(cart.Value.Id, new AddCartItemRequest(Guid.NewGuid(), 1));

        var result = await service.ClearCartAsync(cart.Value.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
    }

    [Fact]
    public async Task AddItemAsync_ShouldRejectMissingProductVariantId()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfCartService(dbContext);

        var cart = await service.CreateCartAsync();

        var result = await service.AddItemAsync(
            cart.Value!.Id,
            new AddCartItemRequest(
                ProductVariantId: Guid.Empty,
                Quantity: 1));

        Assert.False(result.IsSuccess);
        Assert.Equal("product_variant_id_required", result.ErrorCode);
    }

    [Fact]
    public async Task AddItemAsync_ShouldRejectInvalidQuantity()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfCartService(dbContext);

        var cart = await service.CreateCartAsync();

        var result = await service.AddItemAsync(
            cart.Value!.Id,
            new AddCartItemRequest(
                ProductVariantId: Guid.NewGuid(),
                Quantity: 0));

        Assert.False(result.IsSuccess);
        Assert.Equal("quantity_invalid", result.ErrorCode);
    }

    [Fact]
    public async Task AddItemAsync_ShouldRejectMissingCart()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfCartService(dbContext);

        var result = await service.AddItemAsync(
            Guid.NewGuid(),
            new AddCartItemRequest(
                ProductVariantId: Guid.NewGuid(),
                Quantity: 1));

        Assert.False(result.IsSuccess);
        Assert.Equal("cart_not_found", result.ErrorCode);
    }

    [Fact]
    public async Task UpdateItemAsync_ShouldRejectMissingItem()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfCartService(dbContext);

        var cart = await service.CreateCartAsync();

        var result = await service.UpdateItemAsync(
            cart.Value!.Id,
            Guid.NewGuid(),
            new UpdateCartItemRequest(1));

        Assert.False(result.IsSuccess);
        Assert.Equal("cart_item_not_found", result.ErrorCode);
    }

    private static CartDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CartDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new CartDbContext(options);
    }
}
