using Ordering.Application.Orders;

namespace Ordering.Api.Endpoints;

public static class OrderingEndpoints
{
    public static IEndpointRouteBuilder MapOrderingEndpoints(this IEndpointRouteBuilder app)
    {
        var orders = app.MapGroup("/api/orders")
            .WithTags("Orders");

        orders.MapGet("/", async (
            IOrderingService orderingService,
            CancellationToken cancellationToken) =>
        {
            var result = await orderingService.GetOrdersAsync(cancellationToken);

            return Results.Ok(result);
        })
        .WithName("GetOrders")
        .WithOpenApi();

        orders.MapGet("/{orderId:guid}", async (
            Guid orderId,
            IOrderingService orderingService,
            CancellationToken cancellationToken) =>
        {
            var order = await orderingService.GetOrderByIdAsync(orderId, cancellationToken);

            if (order is null)
            {
                return Results.NotFound(new
                {
                    error = "order_not_found",
                    message = "Order was not found."
                });
            }

            return Results.Ok(order);
        })
        .WithName("GetOrderById")
        .WithOpenApi();

        orders.MapPost("/", async (
            CreateOrderRequest request,
            IOrderingService orderingService,
            CancellationToken cancellationToken) =>
        {
            var result = await orderingService.CreateOrderAsync(request, cancellationToken);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new
                {
                    error = result.ErrorCode,
                    message = result.ErrorMessage
                });
            }

            return Results.Created($"/api/orders/{result.Value!.Id}", result.Value);
        })
        .WithName("CreateOrder")
        .WithOpenApi();

        orders.MapPost("/{orderId:guid}/cancel", async (
            Guid orderId,
            IOrderingService orderingService,
            CancellationToken cancellationToken) =>
        {
            var result = await orderingService.CancelOrderAsync(orderId, cancellationToken);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new
                {
                    error = result.ErrorCode,
                    message = result.ErrorMessage
                });
            }

            return Results.Ok(result.Value);
        })
        .WithName("CancelOrder")
        .WithOpenApi();

        return app;
    }
}
