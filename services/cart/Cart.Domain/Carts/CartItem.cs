namespace Cart.Domain.Carts;

public sealed class CartItem
{
    public Guid Id { get; set; }

    public Guid CartId { get; set; }

    public ShoppingCart Cart { get; set; } = null!;

    public Guid ProductVariantId { get; set; }

    public int Quantity { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
