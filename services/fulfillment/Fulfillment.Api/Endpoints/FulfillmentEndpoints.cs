using Fulfillment.Application.Shipments;

namespace Fulfillment.Api.Endpoints;

public static class FulfillmentEndpoints
{
    public static IEndpointRouteBuilder MapFulfillmentEndpoints(this IEndpointRouteBuilder app)
    {
        var shipments = app.MapGroup("/api/shipments")
            .WithTags("Shipments");

        shipments.MapGet("/", async (
            IFulfillmentService fulfillmentService,
            CancellationToken cancellationToken) =>
        {
            var result = await fulfillmentService.GetShipmentsAsync(cancellationToken);

            return Results.Ok(result);
        })
        .WithName("GetShipments")
        .WithOpenApi();

        shipments.MapGet("/{shipmentId:guid}", async (
            Guid shipmentId,
            IFulfillmentService fulfillmentService,
            CancellationToken cancellationToken) =>
        {
            var shipment = await fulfillmentService.GetShipmentByIdAsync(
                shipmentId,
                cancellationToken);

            if (shipment is null)
            {
                return Results.NotFound(new
                {
                    error = "shipment_not_found",
                    message = "Shipment was not found."
                });
            }

            return Results.Ok(shipment);
        })
        .WithName("GetShipmentById")
        .WithOpenApi();

        shipments.MapPost("/", async (
            CreateShipmentRequest request,
            IFulfillmentService fulfillmentService,
            CancellationToken cancellationToken) =>
        {
            var result = await fulfillmentService.CreateShipmentAsync(
                request,
                cancellationToken);

            if (!result.IsSuccess)
            {
                return ToHttpResult(result);
            }

            return Results.Created($"/api/shipments/{result.Value!.Id}", result.Value);
        })
        .WithName("CreateShipment")
        .WithOpenApi();

        shipments.MapPost("/{shipmentId:guid}/ship", async (
            Guid shipmentId,
            ShipShipmentRequest request,
            IFulfillmentService fulfillmentService,
            CancellationToken cancellationToken) =>
        {
            var result = await fulfillmentService.ShipShipmentAsync(
                shipmentId,
                request,
                cancellationToken);

            return ToHttpResult(result);
        })
        .WithName("ShipShipment")
        .WithOpenApi();

        shipments.MapPost("/{shipmentId:guid}/cancel", async (
            Guid shipmentId,
            IFulfillmentService fulfillmentService,
            CancellationToken cancellationToken) =>
        {
            var result = await fulfillmentService.CancelShipmentAsync(
                shipmentId,
                cancellationToken);

            return ToHttpResult(result);
        })
        .WithName("CancelShipment")
        .WithOpenApi();

        return app;
    }

    private static IResult ToHttpResult<T>(FulfillmentResult<T> result)
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
            "shipment_not_found" => Results.NotFound(error),
            "order_not_found" => Results.NotFound(error),
            _ => Results.BadRequest(error)
        };
    }
}
