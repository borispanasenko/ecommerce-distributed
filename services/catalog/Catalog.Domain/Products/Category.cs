namespace Catalog.Domain.Products;

public sealed class Category
{
    public Guid Id { get; set; }

    public Guid? ParentId { get; set; }

    public Category? Parent { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public List<Category> Children { get; set; } = [];

    public List<ProductCategory> ProductCategories { get; set; } = [];
}