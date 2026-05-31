using Catalog.Application.ReferenceData;
using Catalog.Domain.Products;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.ReferenceData;

public sealed class EfCatalogReferenceDataService : ICatalogReferenceDataService
{
    private readonly CatalogDbContext _dbContext;

    public EfCatalogReferenceDataService(CatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<BrandDto>> GetBrandsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Brands
            .AsNoTracking()
            .OrderBy(brand => brand.Name)
            .Select(brand => new BrandDto(
                brand.Id,
                brand.Name,
                brand.Slug))
            .ToListAsync(cancellationToken);
    }

    public async Task<CatalogReferenceDataResult<BrandDto>> CreateBrandAsync(
        CreateBrandRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return CatalogReferenceDataResult<BrandDto>.Failure(
                "brand_name_required",
                "Brand name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Slug))
        {
            return CatalogReferenceDataResult<BrandDto>.Failure(
                "brand_slug_required",
                "Brand slug is required.");
        }

        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();

        var slugExists = await _dbContext.Brands
            .AnyAsync(brand => brand.Slug == normalizedSlug, cancellationToken);

        if (slugExists)
        {
            return CatalogReferenceDataResult<BrandDto>.Failure(
                "brand_slug_already_exists",
                "Brand slug already exists.");
        }

        var now = DateTimeOffset.UtcNow;

        var brand = new Brand
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Slug = normalizedSlug,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Brands.Add(brand);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return CatalogReferenceDataResult<BrandDto>.Success(
            new BrandDto(
                brand.Id,
                brand.Name,
                brand.Slug));
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .Select(category => new CategoryDto(
                category.Id,
                category.ParentId,
                category.Name,
                category.Slug,
                category.Description,
                category.IsActive,
                category.SortOrder))
            .ToListAsync(cancellationToken);
    }

    public async Task<CatalogReferenceDataResult<CategoryDto>> CreateCategoryAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return CatalogReferenceDataResult<CategoryDto>.Failure(
                "category_name_required",
                "Category name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Slug))
        {
            return CatalogReferenceDataResult<CategoryDto>.Failure(
                "category_slug_required",
                "Category slug is required.");
        }

        if (request.SortOrder < 0)
        {
            return CatalogReferenceDataResult<CategoryDto>.Failure(
                "category_sort_order_invalid",
                "Category sort order cannot be negative.");
        }

        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();

        var slugExists = await _dbContext.Categories
            .AnyAsync(category => category.Slug == normalizedSlug, cancellationToken);

        if (slugExists)
        {
            return CatalogReferenceDataResult<CategoryDto>.Failure(
                "category_slug_already_exists",
                "Category slug already exists.");
        }

        Category? parent = null;

        if (request.ParentId is not null)
        {
            parent = await _dbContext.Categories
                .FirstOrDefaultAsync(category => category.Id == request.ParentId, cancellationToken);

            if (parent is null)
            {
                return CatalogReferenceDataResult<CategoryDto>.Failure(
                    "parent_category_not_found",
                    "Parent category was not found.");
            }
        }

        var now = DateTimeOffset.UtcNow;

        var category = new Category
        {
            Id = Guid.NewGuid(),
            ParentId = request.ParentId,
            Parent = parent,
            Name = request.Name.Trim(),
            Slug = normalizedSlug,
            Description = request.Description?.Trim(),
            IsActive = true,
            SortOrder = request.SortOrder,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Categories.Add(category);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return CatalogReferenceDataResult<CategoryDto>.Success(
            new CategoryDto(
                category.Id,
                category.ParentId,
                category.Name,
                category.Slug,
                category.Description,
                category.IsActive,
                category.SortOrder));
    }
}