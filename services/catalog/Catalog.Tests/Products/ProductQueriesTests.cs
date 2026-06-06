using Catalog.Application.Products.Commands;
using Catalog.Infrastructure.Persistence;
using Catalog.Infrastructure.Products;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Tests.Products;

public sealed class ProductQueriesTests
{
    [Fact]
    public async Task GetProductByIdAsync_ShouldReturnProductDetails()
    {
        await using var dbContext = CreateDbContext();

        var commandService = new EfProductCommandService(dbContext);
        var queries = new EfProductQueries(dbContext);

        var productResult = await commandService.CreateProductAsync(new CreateProductRequest(
            BrandId: null,
            Name: "Desk Lamp",
            Slug: "desk-lamp",
            Description: "Minimal desk lamp for home office.",
            CategoryIds: Array.Empty<Guid>()));

        await commandService.AddVariantAsync(
            productResult.Value!.Id,
            new AddProductVariantRequest(
                Sku: "LAMP-BLK",
                Name: "Black",
                PriceAmountMinor: 4900,
                Currency: "USD"));

        await commandService.PublishProductAsync(productResult.Value.Id);

        var product = await queries.GetProductByIdAsync(productResult.Value.Id);

        Assert.NotNull(product);
        Assert.Equal(productResult.Value.Id, product.Id);
        Assert.Equal("Desk Lamp", product.Name);
        Assert.Equal("desk-lamp", product.Slug);
        Assert.Equal("Active", product.Status);
        Assert.Single(product.Variants);
        Assert.Equal("LAMP-BLK", product.Variants[0].Sku);
    }

    [Fact]
    public async Task GetProductByIdAsync_ShouldReturnNull_WhenProductDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var queries = new EfProductQueries(dbContext);

        var product = await queries.GetProductByIdAsync(Guid.NewGuid());

        Assert.Null(product);
    }

    [Fact]
    public async Task GetProductsAsync_ShouldReturnOnlyActiveProducts()
    {
        await using var dbContext = CreateDbContext();

        var commandService = new EfProductCommandService(dbContext);
        var queries = new EfProductQueries(dbContext);

        var activeProductResult = await commandService.CreateProductAsync(new CreateProductRequest(
            BrandId: null,
            Name: "Desk Lamp",
            Slug: "desk-lamp",
            Description: null,
            CategoryIds: Array.Empty<Guid>()));

        await commandService.AddVariantAsync(
            activeProductResult.Value!.Id,
            new AddProductVariantRequest(
                Sku: "LAMP-BLK",
                Name: "Black",
                PriceAmountMinor: 4900,
                Currency: "USD"));

        await commandService.PublishProductAsync(activeProductResult.Value.Id);

        await commandService.CreateProductAsync(new CreateProductRequest(
            BrandId: null,
            Name: "Draft Product",
            Slug: "draft-product",
            Description: null,
            CategoryIds: Array.Empty<Guid>()));

        var products = await queries.GetProductsAsync();

        Assert.Single(products);
        Assert.Equal("Desk Lamp", products[0].Name);
        Assert.Equal("Active", products[0].Status);
    }

    [Fact]
    public async Task GetProductsAsync_ShouldNotReturnArchivedProducts()
    {
        await using var dbContext = CreateDbContext();

        var commandService = new EfProductCommandService(dbContext);
        var queries = new EfProductQueries(dbContext);

        var productResult = await commandService.CreateProductAsync(new CreateProductRequest(
            BrandId: null,
            Name: "Desk Lamp",
            Slug: "desk-lamp",
            Description: null,
            CategoryIds: Array.Empty<Guid>()));

        await commandService.AddVariantAsync(
            productResult.Value!.Id,
            new AddProductVariantRequest(
                Sku: "LAMP-BLK",
                Name: "Black",
                PriceAmountMinor: 4900,
                Currency: "USD"));

        await commandService.PublishProductAsync(productResult.Value.Id);
        await commandService.ArchiveProductAsync(productResult.Value.Id);

        var products = await queries.GetProductsAsync();

        Assert.Empty(products);
    }

    [Fact]
    public async Task GetProductVariantSnapshotAsync_ShouldReturnActiveProductVariantSnapshot()
    {
        await using var dbContext = CreateDbContext();

        var commandService = new EfProductCommandService(dbContext);
        var queries = new EfProductQueries(dbContext);

        var productResult = await commandService.CreateProductAsync(new CreateProductRequest(
            BrandId: null,
            Name: "Monitor Arm",
            Slug: "monitor-arm",
            Description: "Adjustable monitor arm.",
            CategoryIds: Array.Empty<Guid>()));

        var variantResult = await commandService.AddVariantAsync(
            productResult.Value!.Id,
            new AddProductVariantRequest(
                Sku: "arm-blk",
                Name: "Black",
                PriceAmountMinor: 6900,
                Currency: "usd"));

        await commandService.PublishProductAsync(productResult.Value.Id);

        var snapshot = await queries.GetProductVariantSnapshotAsync(variantResult.Value!.Id);

        Assert.NotNull(snapshot);
        Assert.Equal(productResult.Value.Id, snapshot.ProductId);
        Assert.Equal(variantResult.Value.Id, snapshot.ProductVariantId);
        Assert.Equal("ARM-BLK", snapshot.Sku);
        Assert.Equal("Monitor Arm", snapshot.ProductName);
        Assert.Equal("Black", snapshot.VariantName);
        Assert.Equal(6900, snapshot.PriceAmountMinor);
        Assert.Equal("USD", snapshot.Currency);
    }

    [Fact]
    public async Task GetProductVariantSnapshotAsync_ShouldReturnNull_WhenVariantDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var queries = new EfProductQueries(dbContext);

        var snapshot = await queries.GetProductVariantSnapshotAsync(Guid.NewGuid());

        Assert.Null(snapshot);
    }

    [Fact]
    public async Task GetProductVariantSnapshotAsync_ShouldReturnNull_WhenProductIsDraft()
    {
        await using var dbContext = CreateDbContext();

        var commandService = new EfProductCommandService(dbContext);
        var queries = new EfProductQueries(dbContext);

        var productResult = await commandService.CreateProductAsync(new CreateProductRequest(
            BrandId: null,
            Name: "Draft Product",
            Slug: "draft-product",
            Description: null,
            CategoryIds: Array.Empty<Guid>()));

        var variantResult = await commandService.AddVariantAsync(
            productResult.Value!.Id,
            new AddProductVariantRequest(
                Sku: "DRAFT-SKU",
                Name: "Default",
                PriceAmountMinor: 1000,
                Currency: "USD"));

        var snapshot = await queries.GetProductVariantSnapshotAsync(variantResult.Value!.Id);

        Assert.Null(snapshot);
    }

    [Fact]
    public async Task GetProductVariantSnapshotAsync_ShouldReturnNull_WhenProductIsArchived()
    {
        await using var dbContext = CreateDbContext();

        var commandService = new EfProductCommandService(dbContext);
        var queries = new EfProductQueries(dbContext);

        var productResult = await commandService.CreateProductAsync(new CreateProductRequest(
            BrandId: null,
            Name: "Archived Product",
            Slug: "archived-product",
            Description: null,
            CategoryIds: Array.Empty<Guid>()));

        var variantResult = await commandService.AddVariantAsync(
            productResult.Value!.Id,
            new AddProductVariantRequest(
                Sku: "ARCH-SKU",
                Name: "Default",
                PriceAmountMinor: 1000,
                Currency: "USD"));

        await commandService.PublishProductAsync(productResult.Value.Id);
        await commandService.ArchiveProductAsync(productResult.Value.Id);

        var snapshot = await queries.GetProductVariantSnapshotAsync(variantResult.Value!.Id);

        Assert.Null(snapshot);
    }
   
    private static CatalogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new CatalogDbContext(options);
    }
}