namespace Catalog.Domain.Products;

public sealed class Product
{
    public Guid Id { get; set; }

    public Guid? BrandId { get; set; }

    public Brand? Brand { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ProductStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public List<ProductVariant> Variants { get; set; } = [];

    public List<ProductImage> Images { get; set; } = [];

    public List<ProductCategory> ProductCategories { get; set; } = [];
}