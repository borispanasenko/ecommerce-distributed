namespace Catalog.Application.Products.Queries;

public sealed record ProductDetailsDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    ProductBrandDto? Brand,
    string Status,
    IReadOnlyList<ProductCategoryDto> Categories,
    IReadOnlyList<ProductVariantListItemDto> Variants,
    IReadOnlyList<ProductImageDto> Images);

public sealed record ProductBrandDto(
    Guid Id,
    string Name,
    string Slug);

public sealed record ProductCategoryDto(
    Guid Id,
    string Name,
    string Slug);

public sealed record ProductImageDto(
    Guid Id,
    string Url,
    string? AltText,
    int SortOrder,
    bool IsPrimary,
    Guid? VariantId);