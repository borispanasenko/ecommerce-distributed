namespace Catalog.Domain.Products;

public sealed class ProductVariant
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public long PriceAmountMinor { get; set; }

    public string Currency { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public List<ProductImage> Images { get; set; } = [];
}