namespace Cart.Application.Carts;

public interface ICartService
{
    Task<CartResult<CartDto>> CreateCartAsync(
        CancellationToken cancellationToken = default);

    Task<CartDto?> GetCartByIdAsync(
        Guid cartId,
        CancellationToken cancellationToken = default);

    Task<CartResult<CartDto>> AddItemAsync(
        Guid cartId,
        AddCartItemRequest request,
        CancellationToken cancellationToken = default);

    Task<CartResult<CartDto>> UpdateItemAsync(
        Guid cartId,
        Guid productVariantId,
        UpdateCartItemRequest request,
        CancellationToken cancellationToken = default);

    Task<CartResult<CartDto>> RemoveItemAsync(
        Guid cartId,
        Guid productVariantId,
        CancellationToken cancellationToken = default);

    Task<CartResult<CartDto>> ClearCartAsync(
        Guid cartId,
        CancellationToken cancellationToken = default);
}

public sealed record AddCartItemRequest(
    Guid ProductVariantId,
    int Quantity);

public sealed record UpdateCartItemRequest(
    int Quantity);

public sealed record CartDto(
    Guid Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<CartItemDto> Items);

public sealed record CartItemDto(
    Guid Id,
    Guid ProductVariantId,
    int Quantity,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CartResult<T>(
    bool IsSuccess,
    T? Value,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static CartResult<T> Success(T value)
        => new(true, value, null, null);

    public static CartResult<T> Failure(string errorCode, string errorMessage)
        => new(false, default, errorCode, errorMessage);
}
