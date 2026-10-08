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
    /// Initializes a new instance of the <see cref="MapsRateLimitException"/> class.
    /// </summary>
    /// <param name="message">
    /// The exception message. If not provided, a default rate limit message will be used.
    /// </param>
    /// <param name="status">
    /// The status returned by the Maps API.
    /// </param>
    /// <param name="raw">
    /// The raw response returned by the Maps API.
    /// </param>
    /// <param name="inner">
    /// The inner exception that caused this exception.
    /// </param>
    public MapsRateLimitException(
        string? message = null,
        string? status = null,
        string? raw = null,
        Exception? inner = null
    ) : base(message ?? DefaultMessage, status, raw, inner) { }
}