using Catalog.Application.Products.Commands;
using Catalog.Domain.Products;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Products;

public sealed class EfProductCommandService : IProductCommandService
{
    private readonly CatalogDbContext _db;

    public EfProductCommandService(CatalogDbContext db)
    {
        _db = db;
    }

    public async Task<ProductCommandResult<ProductCommandResponse>> CreateProductAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ProductCommandResult<ProductCommandResponse>.Failure(
                "product_name_required",
                "Product name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Slug))
        {
            return ProductCommandResult<ProductCommandResponse>.Failure(
                "product_slug_required",
                "Product slug is required.");
        }

        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();

        var slugExists = await _db.Products
            .AnyAsync(x => x.Slug == normalizedSlug, cancellationToken);

        if (slugExists)
        {
            return ProductCommandResult<ProductCommandResponse>.Failure(
                "product_slug_already_exists",
                "Product slug already exists.");
        }

        Brand? brand = null;

        if (request.BrandId is not null)
        {
            brand = await _db.Brands
                .FirstOrDefaultAsync(x => x.Id == request.BrandId, cancellationToken);

            if (brand is null)
            {
                return ProductCommandResult<ProductCommandResponse>.Failure(
                    "brand_not_found",
                    "Brand was not found.");
            }
        }

        var categoryIds = request.CategoryIds
            .Distinct()
            .ToArray();

        var categories = await _db.Categories
            .Where(x => categoryIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (categories.Count != categoryIds.Length)
        {
            return ProductCommandResult<ProductCommandResponse>.Failure(
                "category_not_found",
                "One or more categories were not found.");
        }

        var now = DateTimeOffset.UtcNow;

        var product = new Product
        {
            Id = Guid.NewGuid(),
            BrandId = request.BrandId,
            Brand = brand,
            Name = request.Name.Trim(),
            Slug = normalizedSlug,
            Description = request.Description?.Trim(),
            Status = ProductStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now
        };

        foreach (var category in categories)
        {
            product.ProductCategories.Add(new ProductCategory
            {
                ProductId = product.Id,
                Product = product,
                CategoryId = category.Id,
                Category = category
            });
        }

        _db.Products.Add(product);

        await _db.SaveChangesAsync(cancellationToken);

        return ProductCommandResult<ProductCommandResponse>.Success(
            new ProductCommandResponse(
                product.Id,
                product.Name,
                product.Slug,
                product.Status.ToString()));
    }

    public async Task<ProductCommandResult<ProductVariantCommandResponse>> AddVariantAsync(
        Guid productId,
        AddProductVariantRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = await _db.Products
            .Include(x => x.Variants)
            .FirstOrDefaultAsync(x => x.Id == productId, cancellationToken);

        if (product is null)
        {
            return ProductCommandResult<ProductVariantCommandResponse>.Failure(
                "product_not_found",
                "Product was not found.");
        }

        if (product.Status == ProductStatus.Archived)
        {
            return ProductCommandResult<ProductVariantCommandResponse>.Failure(
                "product_archived",
                "Archived product cannot be changed.");
        }

        if (string.IsNullOrWhiteSpace(request.Sku))
        {
            return ProductCommandResult<ProductVariantCommandResponse>.Failure(
                "sku_required",
                "SKU is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ProductCommandResult<ProductVariantCommandResponse>.Failure(
                "variant_name_required",
                "Variant name is required.");
        }

        if (request.PriceAmountMinor < 0)
        {
            return ProductCommandResult<ProductVariantCommandResponse>.Failure(
                "price_invalid",
                "Price cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(request.Currency) || request.Currency.Trim().Length != 3)
        {
            return ProductCommandResult<ProductVariantCommandResponse>.Failure(
                "currency_invalid",
                "Currency must be a 3-letter code.");
        }

        var normalizedSku = request.Sku.Trim().ToUpperInvariant();

        var skuExists = await _db.ProductVariants
            .AnyAsync(x => x.Sku == normalizedSku, cancellationToken);

        if (skuExists)
        {
            return ProductCommandResult<ProductVariantCommandResponse>.Failure(
                "sku_already_exists",
                "SKU already exists.");
        }

        var now = DateTimeOffset.UtcNow;

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Product = product,
            Sku = normalizedSku,
            Name = request.Name.Trim(),
            PriceAmountMinor = request.PriceAmountMinor,
            Currency = request.Currency.Trim().ToUpperInvariant(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        product.Variants.Add(variant);
        product.UpdatedAt = now;

        await _db.SaveChangesAsync(cancellationToken);

        return ProductCommandResult<ProductVariantCommandResponse>.Success(
            new ProductVariantCommandResponse(
                variant.Id,
                variant.ProductId,
                variant.Sku,
                variant.Name,
                variant.PriceAmountMinor,
                variant.Currency,
                variant.IsActive));
    }

    public async Task<ProductCommandResult<ProductCommandResponse>> PublishProductAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var product = await _db.Products
            .Include(x => x.Variants)
            .FirstOrDefaultAsync(x => x.Id == productId, cancellationToken);

        if (product is null)
        {
            return ProductCommandResult<ProductCommandResponse>.Failure(
                "product_not_found",
                "Product was not found.");
        }

        if (product.Status == ProductStatus.Archived)
        {
            return ProductCommandResult<ProductCommandResponse>.Failure(
                "product_archived",
                "Archived product cannot be published.");
        }

        var hasActiveVariant = product.Variants.Any(x => x.IsActive);

        if (!hasActiveVariant)
        {
            return ProductCommandResult<ProductCommandResponse>.Failure(
                "product_has_no_active_variants",
                "Product must have at least one active variant before publishing.");
        }

        product.Status = ProductStatus.Active;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return ProductCommandResult<ProductCommandResponse>.Success(
            new ProductCommandResponse(
                product.Id,
                product.Name,
                product.Slug,
                product.Status.ToString()));
    }
}