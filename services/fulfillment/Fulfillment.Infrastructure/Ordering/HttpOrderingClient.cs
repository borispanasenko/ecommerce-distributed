using System.Net.Http.Json;
using System.Text.Json;
using Fulfillment.Application.Ordering;
using Fulfillment.Infrastructure.Http;

namespace Fulfillment.Infrastructure.Ordering;

public sealed class HttpOrderingClient : IOrderingClient
{
    private readonly HttpClient _httpClient;

    public HttpOrderingClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<OrderingClientResult<OrderingOrderDto>> GetOrderByIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"/api/orders/{orderId}",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var order = await response.Content.ReadFromJsonAsync<OrderingOrderDto>(
                    cancellationToken: cancellationToken);

                if (order is null)
                {
                    return OrderingClientResult<OrderingOrderDto>.Failure(
                        "ordering_order_response_invalid",
                        "Ordering returned an invalid order response.");
                }

                return OrderingClientResult<OrderingOrderDto>.Success(order);
            }

            var error = await response.Content.ReadFromJsonAsync<ApiError>(
                cancellationToken: cancellationToken);

            return OrderingClientResult<OrderingOrderDto>.Failure(
                error?.Error ?? "ordering_get_order_failed",
                error?.Message ?? "Ordering get-order request failed.");
        }
        catch (HttpRequestException)
        {
            return OrderingClientResult<OrderingOrderDto>.Failure(
                "ordering_unavailable",
                "Ordering API is unavailable.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return OrderingClientResult<OrderingOrderDto>.Failure(
                "ordering_timeout",
                "Ordering API request timed out.");
        }
        catch (JsonException)
        {
            return OrderingClientResult<OrderingOrderDto>.Failure(
                "ordering_response_invalid",
                "Ordering API returned an invalid JSON response.");
        }
    }

    public async Task<OrderingClientResult> MarkOrderShippedAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await HttpRetryPolicy.SendAsync(
                async retryToken => await _httpClient.PostAsync(
                    $"/api/orders/{orderId}/mark-shipped",
                    content: null,
                    retryToken),
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
        catch (HttpRequestException)
        {
            return OrderingClientResult.Failure(
                "ordering_unavailable",
                "Ordering API is unavailable.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return OrderingClientResult.Failure(
                "ordering_timeout",
                "Ordering API request timed out.");
        }
        catch (JsonException)
        {
            return OrderingClientResult.Failure(
                "ordering_response_invalid",
                "Ordering API returned an invalid JSON response.");
        }
    }

    private sealed record ApiError(
        string Error,
        string Message);
}
