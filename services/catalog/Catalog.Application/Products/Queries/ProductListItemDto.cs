namespace Catalog.Application.Products.Queries;

public sealed record ProductListItemDto(
    Guid Id,
    string Name,
    string Slug,
    string? BrandName,
    string Status,
    string? PrimaryImageUrl,
    IReadOnlyList<string> Categories,
    IReadOnlyList<ProductVariantListItemDto> Variants);