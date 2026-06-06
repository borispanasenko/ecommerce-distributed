using Catalog.Application.Products.Commands;
using Catalog.Application.Products.Queries;

namespace Catalog.Api.Endpoints;

public static class ProductManagementEndpoints
{
    public static IEndpointRouteBuilder MapProductManagementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products")
            .WithTags("Products");

        group.MapGet("/", async (
            IProductQueries productQueries,
            CancellationToken cancellationToken) =>
        {
            var products = await productQueries.GetProductsAsync(cancellationToken);

            return Results.Ok(products);
        })
        .WithName("GetProducts")
        .WithOpenApi();

        group.MapGet("/{productId:guid}", async (
            Guid productId,
            IProductQueries productQueries,
            CancellationToken cancellationToken) =>
        {
            var product = await productQueries.GetProductByIdAsync(productId, cancellationToken);

            if (product is null)
            {
                return Results.NotFound(new
                {
                    error = "product_not_found",
                    message = "Product was not found."
                });
            }

            return Results.Ok(product);
        })
        .WithName("GetProductById")
        .WithOpenApi();

        group.MapPost("/", async (
            CreateProductRequest request,
            IProductCommandService productCommandService,
            CancellationToken cancellationToken) =>
        {
            var result = await productCommandService.CreateProductAsync(request, cancellationToken);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new
                {
                    error = result.ErrorCode,
                    message = result.ErrorMessage
                });
            }

            return Results.Created($"/api/products/{result.Value!.Id}", result.Value);
        });

        group.MapPost("/{productId:guid}/variants", async (
            Guid productId,
            AddProductVariantRequest request,
            IProductCommandService productCommandService,
            CancellationToken cancellationToken) =>
        {
            var result = await productCommandService.AddVariantAsync(productId, request, cancellationToken);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new
                {
                    error = result.ErrorCode,
                    message = result.ErrorMessage
                });
            }

            return Results.Created(
                $"/api/products/{productId}/variants/{result.Value!.Id}",
                result.Value);
        });

        group.MapPost("/{productId:guid}/publish", async (
            Guid productId,
            IProductCommandService productCommandService,
            CancellationToken cancellationToken) =>
        {
            var result = await productCommandService.PublishProductAsync(productId, cancellationToken);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new
                {
                    error = result.ErrorCode,
                    message = result.ErrorMessage
                });
            }

            return Results.Ok(result.Value);
        });

        group.MapGet("/variants/{productVariantId:guid}/snapshot", async (
            Guid productVariantId,
            IProductQueries productQueries,
            CancellationToken cancellationToken) =>
        {
            var snapshot = await productQueries.GetProductVariantSnapshotAsync(
                productVariantId,
                cancellationToken);

            if (snapshot is null)
            {
                return Results.NotFound(new
                {
                    error = "product_variant_snapshot_not_found",
                    message = "Active product variant snapshot was not found."
                });
            }

            return Results.Ok(snapshot);
        })
        .WithName("GetProductVariantSnapshot")
        .WithOpenApi();

        group.MapPost("/{productId:guid}/archive", async (
            Guid productId,
            IProductCommandService productCommandService,
            CancellationToken cancellationToken) =>
        {
            var result = await productCommandService.ArchiveProductAsync(productId, cancellationToken);

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
        .WithName("ArchiveProduct")
        .WithOpenApi();

        return app;
    }
}