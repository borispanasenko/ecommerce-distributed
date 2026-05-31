using Catalog.Application.Products.Commands;

namespace Catalog.Api.Endpoints;

public static class ProductManagementEndpoints
{
    public static IEndpointRouteBuilder MapProductManagementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products")
            .WithTags("Products");

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

        return app;
    }
}