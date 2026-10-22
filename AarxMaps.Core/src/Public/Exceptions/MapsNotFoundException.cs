using System.Net;

namespace AarxMaps.Core.Exceptions;

/// <summary>
/// Represents an error indicating that the requested resource
/// could not be found in the Google Maps API.
/// </summary>
public class MapsNotFoundException : MapsApiException {
    private const string DefaultMessage = "The requested resource was not found in Google Maps API.";

    /// <summary>
    /// Initializes a new instance of the <see cref="MapsNotFoundException"/> class.
    /// </summary>
    /// <param name="message">The error message, or <c>null</c> to use the default message.</param>
    /// <param name="status">The HTTP status code returned by the Maps API, if any.</param>
    /// <param name="response">The raw response body returned by the Maps API, if any.</param>
    /// <param name="inner">The exception that caused this one, if any.</param>
    public MapsNotFoundException(
        string? message = null,
        HttpStatusCode? status = null,
        string? response = null,
        Exception? inner = null
    ) : base(message ?? DefaultMessage, status, response, inner) { }
}