using System.Net.Http.Json;
using Payment.Application.Ordering;

namespace Payment.Infrastructure.Ordering;

public sealed class HttpOrderingClient : IOrderingClient
{
    private readonly HttpClient _httpClient;

    public HttpOrderingClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<OrderingClientResult<OrderDetailsDto>> MarkOrderPaidAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync(
            $"/api/orders/{orderId}/mark-paid",
            content: null,
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var order = await response.Content.ReadFromJsonAsync<OrderDetailsDto>(
                cancellationToken: cancellationToken);

            if (order is null)
            {
                return OrderingClientResult<OrderDetailsDto>.Failure(
                    "ordering_empty_response",
                    "Ordering returned an empty response.");
            }

            return OrderingClientResult<OrderDetailsDto>.Success(order);
        }

        var error = await response.Content.ReadFromJsonAsync<OrderingErrorResponse>(
            cancellationToken: cancellationToken);

        return OrderingClientResult<OrderDetailsDto>.Failure(
            error?.Error ?? "ordering_mark_paid_failed",
            error?.Message ?? "Ordering mark-paid request failed.");
    }

    private sealed record OrderingErrorResponse(
        string Error,
        string Message);
}
