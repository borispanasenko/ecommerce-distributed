namespace Catalog.Application.Products.Queries;

public interface IProductQueries
{
    Task<IReadOnlyList<ProductListItemDto>> GetProductsAsync(
        CancellationToken cancellationToken = default);

    Task<ProductDetailsDto?> GetProductByIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default);
}