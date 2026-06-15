using System.Net.Http.Json;
using System.Text.Json;
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
        try
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
        catch (HttpRequestException)
        {
            return OrderingClientResult<OrderDetailsDto>.Failure(
                "ordering_unavailable",
                "Ordering API is unavailable.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return OrderingClientResult<OrderDetailsDto>.Failure(
                "ordering_timeout",
                "Ordering API request timed out.");
        }
        catch (JsonException)
        {
            return OrderingClientResult<OrderDetailsDto>.Failure(
                "ordering_response_invalid",
                "Ordering API returned an invalid JSON response.");
        }
    }

    private sealed record OrderingErrorResponse(
        string Error,
        string Message);
}
