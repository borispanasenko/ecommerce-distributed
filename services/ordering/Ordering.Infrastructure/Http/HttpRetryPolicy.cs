using System.Net;

namespace Ordering.Infrastructure.Http;

internal static class HttpRetryPolicy
{
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromMilliseconds(200),
        TimeSpan.FromMilliseconds(500)
    ];

    public static async Task<HttpResponseMessage> SendAsync(
        Func<CancellationToken, Task<HttpResponseMessage>> sendRequest,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                var response = await sendRequest(cancellationToken);

                if (!ShouldRetry(response.StatusCode) || attempt >= RetryDelays.Length)
                {
                    return response;
                }

                response.Dispose();
            }
            catch (HttpRequestException) when (attempt < RetryDelays.Length)
            {
            }
            catch (TaskCanceledException) when (
                !cancellationToken.IsCancellationRequested &&
                attempt < RetryDelays.Length)
            {
            }

            await Task.Delay(RetryDelays[attempt], cancellationToken);
        }
    }

    private static bool ShouldRetry(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout
            || (int)statusCode >= 500;
    }
}
