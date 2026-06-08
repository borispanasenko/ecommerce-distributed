using System.Net.Http.Json;
using Fulfillment.Application.Ordering;

namespace Fulfillment.Infrastructure.Ordering;

public sealed class HttpOrderingClient : IOrderingClient
{
    private readonly HttpClient _httpClient;

    public HttpOrderingClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<OrderingClientResult> MarkOrderShippedAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync(
            $"/api/orders/{orderId}/mark-shipped",
            content: null,
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return OrderingClientResult.Success();
        }

        var error = await response.Content.ReadFromJsonAsync<ApiError>(
            cancellationToken: cancellationToken);

        return OrderingClientResult.Failure(
            error?.Error ?? "ordering_mark_shipped_failed",
            error?.Message ?? "Ordering mark-shipped request failed.");
    }

    private sealed record ApiError(
        string Error,
        string Message);
}
