namespace Catalog.Application.Products.Commands;

public interface IProductCommandService
{
    Task<ProductCommandResult<ProductCommandResponse>> CreateProductAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default);

    Task<ProductCommandResult<ProductVariantCommandResponse>> AddVariantAsync(
        Guid productId,
        AddProductVariantRequest request,
        CancellationToken cancellationToken = default);

    Task<ProductCommandResult<ProductCommandResponse>> PublishProductAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<ProductCommandResult<ProductCommandResponse>> ArchiveProductAsync(
        Guid productId,
        CancellationToken cancellationToken = default);
}

public sealed record CreateProductRequest(
    Guid? BrandId,
    string Name,
    string Slug,
    string? Description,
    IReadOnlyCollection<Guid> CategoryIds);

public sealed record AddProductVariantRequest(
    string Sku,
    string Name,
    long PriceAmountMinor,
    string Currency);

public sealed record ProductCommandResponse(
    Guid Id,
    string Name,
    string Slug,
    string Status);

public sealed record ProductVariantCommandResponse(
    Guid Id,
    Guid ProductId,
    string Sku,
    string Name,
    long PriceAmountMinor,
    string Currency,
    bool IsActive);

public sealed record ProductCommandResult<T>(
    bool IsSuccess,
    T? Value,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static ProductCommandResult<T> Success(T value)
        => new(true, value, null, null);

    public static ProductCommandResult<T> Failure(string errorCode, string errorMessage)
        => new(false, default, errorCode, errorMessage);
}