using Catalog.Application.Products.Commands;
using Catalog.Infrastructure.Persistence;
using Catalog.Infrastructure.Products;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Tests.Products;

public sealed class ProductCommandServiceTests
{
    [Fact]
    public async Task CreateProductAsync_ShouldCreateDraftProduct()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfProductCommandService(dbContext);

        var result = await service.CreateProductAsync(new CreateProductRequest(
            BrandId: null,
            Name: "Desk Lamp",
            Slug: "desk-lamp",
            Description: "Minimal desk lamp for home office.",
            CategoryIds: Array.Empty<Guid>()));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Desk Lamp", result.Value.Name);
        Assert.Equal("desk-lamp", result.Value.Slug);
        Assert.Equal("Draft", result.Value.Status);

        var productExists = await dbContext.Products
            .AnyAsync(product => product.Id == result.Value.Id);

        Assert.True(productExists);
    }

    [Fact]
    public async Task CreateProductAsync_ShouldRejectDuplicateSlug()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfProductCommandService(dbContext);

        var firstResult = await service.CreateProductAsync(new CreateProductRequest(
            BrandId: null,
            Name: "Desk Lamp",
            Slug: "desk-lamp",
            Description: null,
            CategoryIds: Array.Empty<Guid>()));

        var secondResult = await service.CreateProductAsync(new CreateProductRequest(
            BrandId: null,
            Name: "Another Desk Lamp",
            Slug: "desk-lamp",
            Description: null,
            CategoryIds: Array.Empty<Guid>()));

        Assert.True(firstResult.IsSuccess);
        Assert.False(secondResult.IsSuccess);
        Assert.Equal("product_slug_already_exists", secondResult.ErrorCode);
    }

    [Fact]
    public async Task AddVariantAsync_ShouldAddActiveVariant()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfProductCommandService(dbContext);

        var productResult = await service.CreateProductAsync(new CreateProductRequest(
            BrandId: null,
            Name: "Desk Lamp",
            Slug: "desk-lamp",
            Description: null,
            CategoryIds: Array.Empty<Guid>()));

        var variantResult = await service.AddVariantAsync(
            productResult.Value!.Id,
            new AddProductVariantRequest(
                Sku: "lamp-blk",
                Name: "Black",
                PriceAmountMinor: 4900,
                Currency: "usd"));

        Assert.True(variantResult.IsSuccess);
        Assert.NotNull(variantResult.Value);
        Assert.Equal("LAMP-BLK", variantResult.Value.Sku);
        Assert.Equal("USD", variantResult.Value.Currency);
        Assert.True(variantResult.Value.IsActive);

        var variantExists = await dbContext.ProductVariants
            .AnyAsync(variant => variant.Sku == "LAMP-BLK");

        Assert.True(variantExists);
    }

    [Fact]
    public async Task AddVariantAsync_ShouldRejectDuplicateSku()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfProductCommandService(dbContext);

        var productResult = await service.CreateProductAsync(new CreateProductRequest(
            BrandId: null,
            Name: "Desk Lamp",
            Slug: "desk-lamp",
            Description: null,
            CategoryIds: Array.Empty<Guid>()));

        var firstVariantResult = await service.AddVariantAsync(
            productResult.Value!.Id,
            new AddProductVariantRequest(
                Sku: "LAMP-BLK",
                Name: "Black",
                PriceAmountMinor: 4900,
                Currency: "USD"));

        var secondVariantResult = await service.AddVariantAsync(
            productResult.Value!.Id,
            new AddProductVariantRequest(
                Sku: "lamp-blk",
                Name: "Black duplicate",
                PriceAmountMinor: 4900,
                Currency: "USD"));

        Assert.True(firstVariantResult.IsSuccess);
        Assert.False(secondVariantResult.IsSuccess);
        Assert.Equal("sku_already_exists", secondVariantResult.ErrorCode);
    }

    [Fact]
    public async Task PublishProductAsync_ShouldRejectProductWithoutActiveVariants()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfProductCommandService(dbContext);

        var productResult = await service.CreateProductAsync(new CreateProductRequest(
            BrandId: null,
            Name: "Desk Lamp",
            Slug: "desk-lamp",
            Description: null,
            CategoryIds: Array.Empty<Guid>()));

        var publishResult = await service.PublishProductAsync(productResult.Value!.Id);

        Assert.False(publishResult.IsSuccess);
        Assert.Equal("product_has_no_active_variants", publishResult.ErrorCode);
    }

    [Fact]
    public async Task PublishProductAsync_ShouldPublishProductWithActiveVariant()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfProductCommandService(dbContext);

        var productResult = await service.CreateProductAsync(new CreateProductRequest(
            BrandId: null,
            Name: "Desk Lamp",
            Slug: "desk-lamp",
            Description: null,
            CategoryIds: Array.Empty<Guid>()));

        await service.AddVariantAsync(
            productResult.Value!.Id,
            new AddProductVariantRequest(
                Sku: "LAMP-BLK",
                Name: "Black",
                PriceAmountMinor: 4900,
                Currency: "USD"));

        var publishResult = await service.PublishProductAsync(productResult.Value.Id);

        Assert.True(publishResult.IsSuccess);
        Assert.NotNull(publishResult.Value);
        Assert.Equal("Active", publishResult.Value.Status);
    }

    private static CatalogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new CatalogDbContext(options);
    }
}