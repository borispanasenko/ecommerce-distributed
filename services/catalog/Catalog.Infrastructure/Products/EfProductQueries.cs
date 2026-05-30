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
}