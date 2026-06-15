using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ordering.Application.Catalog;

namespace Ordering.Infrastructure.Catalog;

public sealed class HttpCatalogClient : ICatalogClient
{
    private readonly HttpClient _httpClient;

    public HttpCatalogClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CatalogClientResult<ProductVariantSnapshotDto>> GetProductVariantSnapshotAsync(
        Guid productVariantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"/api/products/variants/{productVariantId}/snapshot",
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return CatalogClientResult<ProductVariantSnapshotDto>.Failure(
                    "catalog_variant_snapshot_not_found",
                    "Active product variant snapshot was not found.");
            }

            if (response.IsSuccessStatusCode)
            {
                var snapshot = await response.Content.ReadFromJsonAsync<ProductVariantSnapshotDto>(
                    cancellationToken: cancellationToken);

                if (snapshot is null)
                {
                    return CatalogClientResult<ProductVariantSnapshotDto>.Failure(
                        "catalog_empty_response",
                        "Catalog returned an empty product variant snapshot response.");
                }

                return CatalogClientResult<ProductVariantSnapshotDto>.Success(snapshot);
            }

            var error = await response.Content.ReadFromJsonAsync<CatalogErrorResponse>(
                cancellationToken: cancellationToken);

            return CatalogClientResult<ProductVariantSnapshotDto>.Failure(
                error?.Error ?? "catalog_request_failed",
                error?.Message ?? "Catalog request failed.");
        }
        catch (HttpRequestException)
        {
            return CatalogClientResult<ProductVariantSnapshotDto>.Failure(
                "catalog_unavailable",
                "Catalog API is unavailable.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return CatalogClientResult<ProductVariantSnapshotDto>.Failure(
                "catalog_timeout",
                "Catalog API request timed out.");
        }
        catch (JsonException)
        {
            return CatalogClientResult<ProductVariantSnapshotDto>.Failure(
                "catalog_response_invalid",
                "Catalog API returned an invalid JSON response.");
        }
    }

    private sealed record CatalogErrorResponse(
        string Error,
        string Message);
}
