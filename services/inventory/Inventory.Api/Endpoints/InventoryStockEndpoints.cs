using Inventory.Application.Stock;

namespace Inventory.Api.Endpoints;

public static class InventoryStockEndpoints
{
    public static IEndpointRouteBuilder MapInventoryStockEndpoints(this IEndpointRouteBuilder app)
    {
        var warehouses = app.MapGroup("/api/warehouses")
            .WithTags("Warehouses");

        warehouses.MapGet("/", async (
            IInventoryStockService inventoryStockService,
            CancellationToken cancellationToken) =>
        {
            var result = await inventoryStockService.GetWarehousesAsync(cancellationToken);

            return Results.Ok(result);
        })
        .WithName("GetWarehouses")
        .WithOpenApi();

        warehouses.MapPost("/", async (
            CreateWarehouseRequest request,
            IInventoryStockService inventoryStockService,
            CancellationToken cancellationToken) =>
        {
            var result = await inventoryStockService.CreateWarehouseAsync(request, cancellationToken);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new
                {
                    error = result.ErrorCode,
                    message = result.ErrorMessage
                });
            }

            return Results.Created($"/api/warehouses/{result.Value!.Id}", result.Value);
        })
        .WithName("CreateWarehouse")
        .WithOpenApi();

        var locations = app.MapGroup("/api/locations")
            .WithTags("Locations");

        locations.MapGet("/", async (
            Guid? warehouseId,
            IInventoryStockService inventoryStockService,
            CancellationToken cancellationToken) =>
        {
            var result = await inventoryStockService.GetLocationsAsync(warehouseId, cancellationToken);

            return Results.Ok(result);
        })
        .WithName("GetLocations")
        .WithOpenApi();

        locations.MapPost("/", async (
            CreateLocationRequest request,
            IInventoryStockService inventoryStockService,
            CancellationToken cancellationToken) =>
        {
            var result = await inventoryStockService.CreateLocationAsync(request, cancellationToken);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new
                {
                    error = result.ErrorCode,
                    message = result.ErrorMessage
                });
            }

            return Results.Created($"/api/locations/{result.Value!.Id}", result.Value);
        })
        .WithName("CreateLocation")
        .WithOpenApi();

        var stock = app.MapGroup("/api/stock")
            .WithTags("Stock");

        stock.MapPost("/receipts", async (
            ReceiveStockRequest request,
            IInventoryStockService inventoryStockService,
            CancellationToken cancellationToken) =>
        {
            var result = await inventoryStockService.ReceiveStockAsync(request, cancellationToken);

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
        .WithName("ReceiveStock")
        .WithOpenApi();

        stock.MapGet("/movements", async (
            string? sku,
            int? limit,
            IInventoryStockService inventoryStockService,
            CancellationToken cancellationToken) =>
        {
            var result = await inventoryStockService.GetStockMovementsAsync(
                sku,
                limit ?? 100,
                cancellationToken);

            return Results.Ok(result);
        })
        .WithName("GetStockMovements")
        .WithOpenApi();

        stock.MapGet("/{sku}", async (
            string sku,
            IInventoryStockService inventoryStockService,
            CancellationToken cancellationToken) =>
        {
            var result = await inventoryStockService.GetStockBySkuAsync(sku, cancellationToken);

            if (result is null)
            {
                return Results.NotFound(new
                {
                    error = "stock_not_found",
                    message = "Stock was not found for the specified SKU."
                });
            }

            return Results.Ok(result);
        })
        .WithName("GetStockBySku")
        .WithOpenApi();

        return app;
    }
}
