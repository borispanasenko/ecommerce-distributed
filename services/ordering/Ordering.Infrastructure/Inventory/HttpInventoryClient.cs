using System.Net.Http.Json;
using System.Text.Json;
using Ordering.Application.Inventory;

namespace Ordering.Infrastructure.Inventory;

public sealed class HttpInventoryClient : IInventoryClient
{
    private readonly HttpClient _httpClient;

    public HttpInventoryClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<InventoryClientResult<InventoryReservationDto>> AllocateStockAsync(
        AllocateStockRequest request,
        CancellationToken cancellationToken = default)
    {
        return await SendReservationRequestAsync(
            async () => await _httpClient.PostAsJsonAsync(
                "/api/stock/reservations/allocate",
                request,
                cancellationToken),
            "inventory_allocation_failed",
            "Inventory stock allocation failed.",
            cancellationToken);
    }

    public async Task<InventoryClientResult<InventoryReservationDto>> ReleaseReservationAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        return await SendReservationRequestAsync(
            async () => await _httpClient.PostAsync(
                $"/api/stock/reservations/{reservationId}/release",
                content: null,
                cancellationToken),
            "inventory_release_failed",
            "Inventory reservation release failed.",
            cancellationToken);
    }

    public async Task<InventoryClientResult<InventoryReservationDto>> CommitReservationAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        return await SendReservationRequestAsync(
            async () => await _httpClient.PostAsync(
                $"/api/stock/reservations/{reservationId}/commit",
                content: null,
                cancellationToken),
            "inventory_commit_failed",
            "Inventory reservation commit failed.",
            cancellationToken);
    }

    private static async Task<InventoryClientResult<InventoryReservationDto>> SendReservationRequestAsync(
        Func<Task<HttpResponseMessage>> sendRequest,
        string fallbackErrorCode,
        string fallbackErrorMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await sendRequest();

            if (response.IsSuccessStatusCode)
            {
                var reservation = await response.Content.ReadFromJsonAsync<InventoryReservationDto>(
                    cancellationToken: cancellationToken);

                if (reservation is null)
                {
                    return InventoryClientResult<InventoryReservationDto>.Failure(
                        "inventory_empty_response",
                        "Inventory returned an empty reservation response.");
                }

                return InventoryClientResult<InventoryReservationDto>.Success(reservation);
            }

            var error = await response.Content.ReadFromJsonAsync<InventoryErrorResponse>(
                cancellationToken: cancellationToken);

            return InventoryClientResult<InventoryReservationDto>.Failure(
                error?.Error ?? fallbackErrorCode,
                error?.Message ?? fallbackErrorMessage);
        }
        catch (HttpRequestException)
        {
            return InventoryClientResult<InventoryReservationDto>.Failure(
                "inventory_unavailable",
                "Inventory API is unavailable.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return InventoryClientResult<InventoryReservationDto>.Failure(
                "inventory_timeout",
                "Inventory API request timed out.");
        }
        catch (JsonException)
        {
            return InventoryClientResult<InventoryReservationDto>.Failure(
                "inventory_response_invalid",
                "Inventory API returned an invalid JSON response.");
        }
    }

    private sealed record InventoryErrorResponse(
        string Error,
        string Message);
}
