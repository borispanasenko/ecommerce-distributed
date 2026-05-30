namespace Catalog.Application.Products.Queries;

public interface IProductQueries
{
    Task<IReadOnlyList<ProductListItemDto>> GetProductsAsync(
        CancellationToken cancellationToken = default);
}