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