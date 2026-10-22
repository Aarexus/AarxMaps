using System.Net;

namespace AarxMaps.Core.Exceptions;

/// <summary>
/// Represents an authentication error returned by the Google Maps API.
/// </summary>
/// <remarks>
/// This exception is thrown when the provided API key is invalid,
/// missing, or does not have the required permissions to access
/// the requested service.
/// </remarks>
public class MapsApiAuthException : MapsApiException {
    private const string DefaultMessage = "The provided API Key is invalid or lacks required permissions.";

    /// <summary>
    /// Initializes a new instance of the <see cref="MapsApiAuthException"/> class.
    /// </summary>
    /// <param name="message">The error message, or <c>null</c> to use the default message.</param>
    /// <param name="status">The HTTP status code returned by the Maps API, if any.</param>
    /// <param name="response">The raw response body returned by the Maps API, if any.</param>
    /// <param name="inner">The exception that caused this one, if any.</param>
    public MapsApiAuthException(
        string? message = null,
        HttpStatusCode? status = null,
        string? response = null,
        Exception? inner = null
    ) : base(message ?? DefaultMessage, status, response, inner) { }
}