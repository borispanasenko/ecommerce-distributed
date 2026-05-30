namespace Catalog.Domain.Products;

public sealed class ProductImage
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public Guid? VariantId { get; set; }

    public ProductVariant? Variant { get; set; }

    public string Url { get; set; } = string.Empty;

    public string? AltText { get; set; }

    public int SortOrder { get; set; }

    public bool IsPrimary { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}