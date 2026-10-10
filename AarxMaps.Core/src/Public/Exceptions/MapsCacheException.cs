namespace AarxMaps.Core.Exceptions;

/// <summary>
/// Represents errors that occur during AarxMaps caching operations.
/// </summary>
public class MapsCacheException : MapsApiException {
    private const string DefaultMessage = "Error during caching-operation.";

    /// <summary>
    /// Initializes a new instance of the <see cref="MapsCacheException"/> class.
    /// </summary>
    /// <param name="message">
    /// The exception message. If not provided, a default not-found message will be used.
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
    public MapsCacheException(
        string? message = null,
        string? status = null,
        string? raw = null,
        Exception? inner = null
    ) : base(message ?? DefaultMessage, status, raw, inner) { }
}