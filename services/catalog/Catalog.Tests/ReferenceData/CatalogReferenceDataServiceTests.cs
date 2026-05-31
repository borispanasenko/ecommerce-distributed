using Catalog.Infrastructure.Persistence;
using Catalog.Infrastructure.ReferenceData;
using Catalog.Application.ReferenceData;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Tests.ReferenceData;

public sealed class CatalogReferenceDataServiceTests
{
    [Fact]
    public async Task CreateBrandAsync_ShouldCreateBrand()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfCatalogReferenceDataService(dbContext);

        var result = await service.CreateBrandAsync(new CreateBrandRequest(
            Name: "DeskPro",
            Slug: "deskpro"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("DeskPro", result.Value.Name);
        Assert.Equal("deskpro", result.Value.Slug);

        var brandExists = await dbContext.Brands
            .AnyAsync(brand => brand.Id == result.Value.Id);

        Assert.True(brandExists);
    }

    [Fact]
    public async Task CreateBrandAsync_ShouldNormalizeSlug()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfCatalogReferenceDataService(dbContext);

        var result = await service.CreateBrandAsync(new CreateBrandRequest(
            Name: "DeskPro",
            Slug: " DeskPro "));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("deskpro", result.Value.Slug);
    }

    [Fact]
    public async Task CreateBrandAsync_ShouldRejectDuplicateSlug()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfCatalogReferenceDataService(dbContext);

        var firstResult = await service.CreateBrandAsync(new CreateBrandRequest(
            Name: "DeskPro",
            Slug: "deskpro"));

        var secondResult = await service.CreateBrandAsync(new CreateBrandRequest(
            Name: "DeskPro Duplicate",
            Slug: "DeskPro"));

        Assert.True(firstResult.IsSuccess);
        Assert.False(secondResult.IsSuccess);
        Assert.Equal("brand_slug_already_exists", secondResult.ErrorCode);
    }

    [Fact]
    public async Task GetBrandsAsync_ShouldReturnBrandsOrderedByName()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfCatalogReferenceDataService(dbContext);

        await service.CreateBrandAsync(new CreateBrandRequest(
            Name: "Zeta",
            Slug: "zeta"));

        await service.CreateBrandAsync(new CreateBrandRequest(
            Name: "Alpha",
            Slug: "alpha"));

        var brands = await service.GetBrandsAsync();

        Assert.Equal(2, brands.Count);
        Assert.Equal("Alpha", brands[0].Name);
        Assert.Equal("Zeta", brands[1].Name);
    }

    [Fact]
    public async Task CreateCategoryAsync_ShouldCreateCategory()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfCatalogReferenceDataService(dbContext);

        var result = await service.CreateCategoryAsync(new CreateCategoryRequest(
            ParentId: null,
            Name: "Workspace",
            Slug: "workspace",
            Description: "Products for workspace setups.",
            SortOrder: 10));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Workspace", result.Value.Name);
        Assert.Equal("workspace", result.Value.Slug);
        Assert.Equal("Products for workspace setups.", result.Value.Description);
        Assert.True(result.Value.IsActive);
        Assert.Equal(10, result.Value.SortOrder);

        var categoryExists = await dbContext.Categories
            .AnyAsync(category => category.Id == result.Value.Id);

        Assert.True(categoryExists);
    }

    [Fact]
    public async Task CreateCategoryAsync_ShouldNormalizeSlug()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfCatalogReferenceDataService(dbContext);

        var result = await service.CreateCategoryAsync(new CreateCategoryRequest(
            ParentId: null,
            Name: "Workspace",
            Slug: " Workspace ",
            Description: null,
            SortOrder: 10));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("workspace", result.Value.Slug);
    }

    [Fact]
    public async Task CreateCategoryAsync_ShouldRejectDuplicateSlug()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfCatalogReferenceDataService(dbContext);

        var firstResult = await service.CreateCategoryAsync(new CreateCategoryRequest(
            ParentId: null,
            Name: "Workspace",
            Slug: "workspace",
            Description: null,
            SortOrder: 10));

        var secondResult = await service.CreateCategoryAsync(new CreateCategoryRequest(
            ParentId: null,
            Name: "Workspace Duplicate",
            Slug: "Workspace",
            Description: null,
            SortOrder: 11));

        Assert.True(firstResult.IsSuccess);
        Assert.False(secondResult.IsSuccess);
        Assert.Equal("category_slug_already_exists", secondResult.ErrorCode);
    }

    [Fact]
    public async Task CreateCategoryAsync_ShouldRejectNegativeSortOrder()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfCatalogReferenceDataService(dbContext);

        var result = await service.CreateCategoryAsync(new CreateCategoryRequest(
            ParentId: null,
            Name: "Workspace",
            Slug: "workspace",
            Description: null,
            SortOrder: -1));

        Assert.False(result.IsSuccess);
        Assert.Equal("category_sort_order_invalid", result.ErrorCode);
    }

    [Fact]
    public async Task CreateCategoryAsync_ShouldRejectMissingParent()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfCatalogReferenceDataService(dbContext);

        var result = await service.CreateCategoryAsync(new CreateCategoryRequest(
            ParentId: Guid.NewGuid(),
            Name: "Monitor Arms",
            Slug: "monitor-arms",
            Description: null,
            SortOrder: 1));

        Assert.False(result.IsSuccess);
        Assert.Equal("parent_category_not_found", result.ErrorCode);
    }

    [Fact]
    public async Task CreateCategoryAsync_ShouldCreateChildCategory()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfCatalogReferenceDataService(dbContext);

        var parentResult = await service.CreateCategoryAsync(new CreateCategoryRequest(
            ParentId: null,
            Name: "Workspace",
            Slug: "workspace",
            Description: null,
            SortOrder: 10));

        var childResult = await service.CreateCategoryAsync(new CreateCategoryRequest(
            ParentId: parentResult.Value!.Id,
            Name: "Monitor Arms",
            Slug: "monitor-arms",
            Description: null,
            SortOrder: 1));

        Assert.True(childResult.IsSuccess);
        Assert.NotNull(childResult.Value);
        Assert.Equal(parentResult.Value.Id, childResult.Value.ParentId);
    }

    [Fact]
    public async Task GetCategoriesAsync_ShouldReturnCategoriesOrderedBySortOrderThenName()
    {
        await using var dbContext = CreateDbContext();
        var service = new EfCatalogReferenceDataService(dbContext);

        await service.CreateCategoryAsync(new CreateCategoryRequest(
            ParentId: null,
            Name: "Zeta",
            Slug: "zeta",
            Description: null,
            SortOrder: 2));

        await service.CreateCategoryAsync(new CreateCategoryRequest(
            ParentId: null,
            Name: "Alpha",
            Slug: "alpha",
            Description: null,
            SortOrder: 1));

        await service.CreateCategoryAsync(new CreateCategoryRequest(
            ParentId: null,
            Name: "Beta",
            Slug: "beta",
            Description: null,
            SortOrder: 1));

        var categories = await service.GetCategoriesAsync();

        Assert.Equal(3, categories.Count);
        Assert.Equal("Alpha", categories[0].Name);
        Assert.Equal("Beta", categories[1].Name);
        Assert.Equal("Zeta", categories[2].Name);
    }

    private static CatalogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new CatalogDbContext(options);
    }
}