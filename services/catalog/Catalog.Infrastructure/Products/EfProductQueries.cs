using Catalog.Application.Products.Queries;
using Catalog.Domain.Products;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Products;

public sealed class EfProductQueries : IProductQueries
{
    private readonly CatalogDbContext _dbContext;

    public EfProductQueries(CatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ProductListItemDto>> GetProductsAsync(
        CancellationToken cancellationToken = default)
    {
        var products = await _dbContext.Products
            .AsNoTracking()
            .AsSplitQuery()
            .Include(product => product.Brand)
            .Include(product => product.Variants)
            .Include(product => product.Images)
            .Include(product => product.ProductCategories)
                .ThenInclude(productCategory => productCategory.Category)
            .Where(product => product.Status == ProductStatus.Active)
            .OrderBy(product => product.Name)
            .ToListAsync(cancellationToken);

        return products
            .Select(product => new ProductListItemDto(
                Id: product.Id,
                Name: product.Name,
                Slug: product.Slug,
                BrandName: product.Brand?.Name,
                Status: product.Status.ToString(),
                PrimaryImageUrl: product.Images
                    .Where(image => image.IsPrimary)
                    .OrderBy(image => image.SortOrder)
                    .Select(image => image.Url)
                    .FirstOrDefault(),
                Categories: product.ProductCategories
                    .OrderBy(productCategory => productCategory.Category.SortOrder)
                    .Select(productCategory => productCategory.Category.Name)
                    .ToList(),
                Variants: product.Variants
                    .Where(variant => variant.IsActive)
                    .OrderBy(variant => variant.Sku)
                    .Select(variant => new ProductVariantListItemDto(
                        Id: variant.Id,
                        Sku: variant.Sku,
                        Name: variant.Name,
                        PriceAmountMinor: variant.PriceAmountMinor,
                        Currency: variant.Currency,
                        IsActive: variant.IsActive))
                    .ToList()))
            .ToList();
    }

    public async Task<ProductDetailsDto?> GetProductByIdAsync(
    Guid productId,
    CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .AsSplitQuery()
            .Include(product => product.Brand)
            .Include(product => product.Variants)
            .Include(product => product.Images)
            .Include(product => product.ProductCategories)
                .ThenInclude(productCategory => productCategory.Category)
            .FirstOrDefaultAsync(product => product.Id == productId, cancellationToken);

        if (product is null)
        {
            return null;
        }

        return new ProductDetailsDto(
            Id: product.Id,
            Name: product.Name,
            Slug: product.Slug,
            Description: product.Description,
            Brand: product.Brand is null
                ? null
                : new ProductBrandDto(
                    Id: product.Brand.Id,
                    Name: product.Brand.Name,
                    Slug: product.Brand.Slug),
            Status: product.Status.ToString(),
            Categories: product.ProductCategories
                .OrderBy(productCategory => productCategory.Category.SortOrder)
                .Select(productCategory => new ProductCategoryDto(
                    Id: productCategory.Category.Id,
                    Name: productCategory.Category.Name,
                    Slug: productCategory.Category.Slug))
                .ToList(),
            Variants: product.Variants
                .OrderBy(variant => variant.Sku)
                .Select(variant => new ProductVariantListItemDto(
                    Id: variant.Id,
                    Sku: variant.Sku,
                    Name: variant.Name,
                    PriceAmountMinor: variant.PriceAmountMinor,
                    Currency: variant.Currency,
                    IsActive: variant.IsActive))
                .ToList(),
            Images: product.Images
                .OrderBy(image => image.SortOrder)
                .Select(image => new ProductImageDto(
                    Id: image.Id,
                    Url: image.Url,
                    AltText: image.AltText,
                    SortOrder: image.SortOrder,
                    IsPrimary: image.IsPrimary,
                    VariantId: image.VariantId))
                .ToList());
    }
}