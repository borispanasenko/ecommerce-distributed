using Catalog.Application.ReferenceData;

namespace Catalog.Api.Endpoints;

public static class CatalogReferenceDataEndpoints
{
    public static IEndpointRouteBuilder MapCatalogReferenceDataEndpoints(this IEndpointRouteBuilder app)
    {
        var brands = app.MapGroup("/api/brands")
            .WithTags("Brands");

        brands.MapGet("/", async (
            ICatalogReferenceDataService referenceDataService,
            CancellationToken cancellationToken) =>
        {
            var result = await referenceDataService.GetBrandsAsync(cancellationToken);

            return Results.Ok(result);
        })
        .WithName("GetBrands")
        .WithOpenApi();

        brands.MapPost("/", async (
            CreateBrandRequest request,
            ICatalogReferenceDataService referenceDataService,
            CancellationToken cancellationToken) =>
        {
            var result = await referenceDataService.CreateBrandAsync(request, cancellationToken);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new
                {
                    error = result.ErrorCode,
                    message = result.ErrorMessage
                });
            }

            return Results.Created($"/api/brands/{result.Value!.Id}", result.Value);
        })
        .WithName("CreateBrand")
        .WithOpenApi();

        var categories = app.MapGroup("/api/categories")
            .WithTags("Categories");

        categories.MapGet("/", async (
            ICatalogReferenceDataService referenceDataService,
            CancellationToken cancellationToken) =>
        {
            var result = await referenceDataService.GetCategoriesAsync(cancellationToken);

            return Results.Ok(result);
        })
        .WithName("GetCategories")
        .WithOpenApi();

        categories.MapPost("/", async (
            CreateCategoryRequest request,
            ICatalogReferenceDataService referenceDataService,
            CancellationToken cancellationToken) =>
        {
            var result = await referenceDataService.CreateCategoryAsync(request, cancellationToken);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new
                {
                    error = result.ErrorCode,
                    message = result.ErrorMessage
                });
            }

            return Results.Created($"/api/categories/{result.Value!.Id}", result.Value);
        })
        .WithName("CreateCategory")
        .WithOpenApi();

        return app;
    }
}