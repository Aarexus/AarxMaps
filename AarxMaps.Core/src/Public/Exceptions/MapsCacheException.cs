using System.Net;

namespace AarxMaps.Core.Exceptions;

/// <summary>
/// Represents errors that occur during AarxMaps caching operations.
/// </summary>
public class MapsCacheException : MapsApiException {
    private const string DefaultMessage = "Error during caching-operation.";

    /// <summary>
    /// Initializes a new instance of the <see cref="MapsCacheException"/> class.
    /// </summary>
    /// <param name="message">The error message, or <c>null</c> to use the default message.</param>
    /// <param name="status">The HTTP status code returned by the Maps API, if any.</param>
    /// <param name="response">The raw response body returned by the Maps API, if any.</param>
    /// <param name="inner">The exception that caused this one, if any.</param>
    public MapsCacheException(
        string? message = null,
        HttpStatusCode? status = null,
        string? response = null,
        Exception? inner = null
    ) : base(message ?? DefaultMessage, status, response, inner) { }
}