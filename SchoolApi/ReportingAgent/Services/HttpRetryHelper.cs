using System.Net;

namespace GameAi.Api.ReportingAgent.Services
{
    /// <summary>
    /// Helper class for HTTP retry logic with exponential backoff
    /// </summary>
    public static class HttpRetryHelper
    {
        private static readonly HashSet<HttpStatusCode> RetryableStatusCodes = new()
        {
            HttpStatusCode.RequestTimeout,
            HttpStatusCode.TooManyRequests,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.BadGateway,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.GatewayTimeout
        };

        /// <summary>
        /// Execute an HTTP request with retry logic
        /// </summary>
        /// <param name="httpClient">The HTTP client to use</param>
        /// <param name="requestFactory">Factory function to create the HTTP request message</param>
        /// <param name="maxRetries">Maximum number of retries (default: 3)</param>
        /// <param name="baseDelayMs">Base delay in milliseconds for exponential backoff (default: 1000)</param>
        /// <returns>The HTTP response message</returns>
        public static async Task<HttpResponseMessage> ExecuteWithRetryAsync(
            HttpClient httpClient,
            Func<HttpRequestMessage> requestFactory,
            int maxRetries = 3,
            int baseDelayMs = 1000)
        {
            HttpResponseMessage? lastResponse = null;
            Exception? lastException = null;

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var request = requestFactory();
                    lastResponse = await httpClient.SendAsync(request);
                    
                    // If successful or not retryable, return immediately
                    if (lastResponse.IsSuccessStatusCode || 
                        !RetryableStatusCodes.Contains(lastResponse.StatusCode))
                    {
                        return lastResponse;
                    }

                    // If this was the last attempt, return the response
                    if (attempt == maxRetries)
                    {
                        return lastResponse;
                    }

                    // Calculate delay with exponential backoff
                    var delayMs = baseDelayMs * (int)Math.Pow(2, attempt);
                    await Task.Delay(delayMs);
                }
                catch (HttpRequestException ex)
                {
                    lastException = ex;
                    // If this was the last attempt, throw
                    if (attempt == maxRetries)
                    {
                        throw new HttpRequestException(
                            $"HTTP request failed after {maxRetries + 1} attempts.", ex);
                    }

                    // Calculate delay with exponential backoff
                    var delayMs = baseDelayMs * (int)Math.Pow(2, attempt);
                    await Task.Delay(delayMs);
                }
                catch (TaskCanceledException ex)
                {
                    lastException = ex;
                    // If this was the last attempt, throw
                    if (attempt == maxRetries)
                    {
                        throw new HttpRequestException(
                            $"HTTP request timed out after {maxRetries + 1} attempts.", ex);
                    }

                    // Calculate delay with exponential backoff
                    var delayMs = baseDelayMs * (int)Math.Pow(2, attempt);
                    await Task.Delay(delayMs);
                }
            }

            // Should never reach here, but handle it just in case
            if (lastResponse != null)
                return lastResponse;

            throw lastException ?? new InvalidOperationException("HTTP request failed for unknown reason.");
        }
    }
}

