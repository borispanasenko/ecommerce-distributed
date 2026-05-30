namespace Catalog.Application.Products.Queries;

public sealed record ProductVariantListItemDto(
    Guid Id,
    string Sku,
    string Name,
    long PriceAmountMinor,
    string Currency,
    bool IsActive);