namespace Catalog.Application.Products.Queries;

public sealed record ProductVariantSnapshotDto(
    Guid ProductId,
    Guid ProductVariantId,
    string Sku,
    string ProductName,
    string VariantName,
    long PriceAmountMinor,
    string Currency);