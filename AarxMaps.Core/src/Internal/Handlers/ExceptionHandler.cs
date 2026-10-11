using System.Net;
using System.Net.Http.Headers;
using System.Text;
using AarxMaps.Core.Exceptions;

namespace AarxMaps.Core.Internal.Handlers;

internal static class ExceptionHandler {
    private const int MaxBodyChars = 4096;

    public static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct) {
        if (response.IsSuccessStatusCode) return;

        var status = response.StatusCode;
        var body = await ReadBoundedBodyAsync(response, ct).ConfigureAwait(false);
        var message = $"Maps API returned {(int)status} {status}.";

        throw status switch {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                => new MapsApiAuthException(message, status, body),
            HttpStatusCode.TooManyRequests
                => new MapsRateLimitException(message, status, body, GetRetryAfter(response.Headers.RetryAfter)),
            HttpStatusCode.NotFound
                => new MapsNotFoundException(message, status, body),
            HttpStatusCode.BadRequest
                => new MapsInvalidRequestException(message, status, body),
            _ => new MapsApiException(message, status, body)
        };
    }

    // INNER METHS
    private static TimeSpan? GetRetryAfter(RetryConditionHeaderValue? retry) {
        if (retry is null) return null;
        if (retry.Delta is { } delta) return delta;
        if (retry.Date is { } date) return date - DateTimeOffset.UtcNow;
        return null;
    }

    private static async Task<string?> ReadBoundedBodyAsync(HttpResponseMessage response, CancellationToken ct) {
        try {
            await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            var buffer = new char[MaxBodyChars];
            var read = await reader.ReadBlockAsync(buffer.AsMemory(), ct).ConfigureAwait(false);

            return read == 0 ? null : new string(buffer, 0, read);
        }
        catch (Exception ex) when (ex is not OperationCanceledException) { return null; }
    }
}