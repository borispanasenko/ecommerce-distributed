namespace Catalog.Application.ReferenceData;

public interface ICatalogReferenceDataService
{
    Task<IReadOnlyList<BrandDto>> GetBrandsAsync(
        CancellationToken cancellationToken = default);

    Task<CatalogReferenceDataResult<BrandDto>> CreateBrandAsync(
        CreateBrandRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(
        CancellationToken cancellationToken = default);

    Task<CatalogReferenceDataResult<CategoryDto>> CreateCategoryAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record BrandDto(
    Guid Id,
    string Name,
    string Slug);

public sealed record CreateBrandRequest(
    string Name,
    string Slug);

public sealed record CategoryDto(
    Guid Id,
    Guid? ParentId,
    string Name,
    string Slug,
    string? Description,
    bool IsActive,
    int SortOrder);

public sealed record CreateCategoryRequest(
    Guid? ParentId,
    string Name,
    string Slug,
    string? Description,
    int SortOrder);

public sealed record CatalogReferenceDataResult<T>(
    bool IsSuccess,
    T? Value,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static CatalogReferenceDataResult<T> Success(T value)
        => new(true, value, null, null);

    public static CatalogReferenceDataResult<T> Failure(string errorCode, string errorMessage)
        => new(false, default, errorCode, errorMessage);
}