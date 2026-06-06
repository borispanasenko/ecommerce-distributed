namespace Ordering.Application.Catalog;

public interface ICatalogClient
{
    Task<CatalogClientResult<ProductVariantSnapshotDto>> GetProductVariantSnapshotAsync(
        Guid productVariantId,
        CancellationToken cancellationToken = default);
}

public sealed record ProductVariantSnapshotDto(
    Guid ProductId,
    Guid ProductVariantId,
    string Sku,
    string ProductName,
    string VariantName,
    long PriceAmountMinor,
    string Currency);

public sealed record CatalogClientResult<T>(
    bool IsSuccess,
    T? Value,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static CatalogClientResult<T> Success(T value)
        => new(true, value, null, null);

    public static CatalogClientResult<T> Failure(string errorCode, string errorMessage)
        => new(false, default, errorCode, errorMessage);
}
