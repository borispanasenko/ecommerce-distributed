using System.Net.Http.Json;
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
        var response = await _httpClient.PostAsJsonAsync(
            "/api/stock/reservations/allocate",
            request,
            cancellationToken);

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
            error?.Error ?? "inventory_allocation_failed",
            error?.Message ?? "Inventory stock allocation failed.");
    }

    public async Task<InventoryClientResult<InventoryReservationDto>> ReleaseReservationAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync(
            $"/api/stock/reservations/{reservationId}/release",
            content: null,
            cancellationToken);

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
            error?.Error ?? "inventory_release_failed",
            error?.Message ?? "Inventory reservation release failed.");
    }

    public async Task<InventoryClientResult<InventoryReservationDto>> CommitReservationAsync(
    Guid reservationId,
    CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync(
            $"/api/stock/reservations/{reservationId}/commit",
            content: null,
            cancellationToken);

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
            error?.Error ?? "inventory_commit_failed",
            error?.Message ?? "Inventory reservation commit failed.");
    }

    private sealed record InventoryErrorResponse(
        string Error,
        string Message);
}
