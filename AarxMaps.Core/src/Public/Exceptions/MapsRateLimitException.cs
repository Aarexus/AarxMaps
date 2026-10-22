using System.Net;

namespace AarxMaps.Core.Exceptions;

/// <summary>
/// Represents an error indicating that the Google Maps API rate limit has been exceeded.
/// </summary>
/// <remarks>
/// This exception may occur when too many requests are sent within a short period of time
/// according to the quota limits configured for the API key.
/// </remarks>
public class MapsRateLimitException : MapsApiException {
    private const string DefaultMessage = "The Google Maps API rate limit has been exceeded.";

    /// <summary>
    /// Gets how long must wait before making a follow-up request, if available.
    /// </summary>
    public TimeSpan? RetryAfter { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapsRateLimitException"/> class.
    /// </summary>
    /// <param name="message">The error message, or <c>null</c> to use the default message.</param>
    /// <param name="status">The HTTP status code returned by the Maps API, if any.</param>
    /// <param name="response">The raw response body returned by the Maps API, if any.</param>
    /// <param name="retryAfter">How long Google asked the caller to wait before retrying (the <c>Retry-After</c> header), if sent.</param>
    /// <param name="inner">The exception that caused this one, if any.</param>
    public MapsRateLimitException(
        string? message = null,
        HttpStatusCode? status = null,
        string? response = null,
        TimeSpan? retryAfter = null,
        Exception? inner = null
    ) : base(message ?? DefaultMessage, status, response, inner)
    {
        RetryAfter = retryAfter;
    }
}