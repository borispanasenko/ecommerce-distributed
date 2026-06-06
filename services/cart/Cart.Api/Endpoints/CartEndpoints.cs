using Cart.Application.Carts;

namespace Cart.Api.Endpoints;

public static class CartEndpoints
{
    public static IEndpointRouteBuilder MapCartEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/carts")
            .WithTags("Carts");

        group.MapPost("/", async (
            ICartService cartService,
            CancellationToken cancellationToken) =>
        {
            var result = await cartService.CreateCartAsync(cancellationToken);

            return Results.Created($"/api/carts/{result.Value!.Id}", result.Value);
        })
        .WithName("CreateCart")
        .WithOpenApi();

        group.MapGet("/{cartId:guid}", async (
            Guid cartId,
            ICartService cartService,
            CancellationToken cancellationToken) =>
        {
            var cart = await cartService.GetCartByIdAsync(cartId, cancellationToken);

            if (cart is null)
            {
                return Results.NotFound(new
                {
                    error = "cart_not_found",
                    message = "Cart was not found."
                });
            }

            return Results.Ok(cart);
        })
        .WithName("GetCartById")
        .WithOpenApi();

        group.MapPost("/{cartId:guid}/items", async (
            Guid cartId,
            AddCartItemRequest request,
            ICartService cartService,
            CancellationToken cancellationToken) =>
        {
            var result = await cartService.AddItemAsync(cartId, request, cancellationToken);

            return ToHttpResult(result);
        })
        .WithName("AddCartItem")
        .WithOpenApi();

        group.MapPut("/{cartId:guid}/items/{productVariantId:guid}", async (
            Guid cartId,
            Guid productVariantId,
            UpdateCartItemRequest request,
            ICartService cartService,
            CancellationToken cancellationToken) =>
        {
            var result = await cartService.UpdateItemAsync(
                cartId,
                productVariantId,
                request,
                cancellationToken);

            return ToHttpResult(result);
        })
        .WithName("UpdateCartItem")
        .WithOpenApi();

        group.MapDelete("/{cartId:guid}/items/{productVariantId:guid}", async (
            Guid cartId,
            Guid productVariantId,
            ICartService cartService,
            CancellationToken cancellationToken) =>
        {
            var result = await cartService.RemoveItemAsync(
                cartId,
                productVariantId,
                cancellationToken);

            return ToHttpResult(result);
        })
        .WithName("RemoveCartItem")
        .WithOpenApi();

        group.MapPost("/{cartId:guid}/clear", async (
            Guid cartId,
            ICartService cartService,
            CancellationToken cancellationToken) =>
        {
            var result = await cartService.ClearCartAsync(cartId, cancellationToken);

            return ToHttpResult(result);
        })
        .WithName("ClearCart")
        .WithOpenApi();

        return app;
    }

    private static IResult ToHttpResult<T>(CartResult<T> result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(result.Value);
        }

        var error = new
        {
            error = result.ErrorCode,
            message = result.ErrorMessage
        };

        return result.ErrorCode switch
        {
            "cart_not_found" => Results.NotFound(error),
            "cart_item_not_found" => Results.NotFound(error),
            _ => Results.BadRequest(error)
        };
    }
}
