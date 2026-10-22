using System.Net;

namespace AarxMaps.Core.Exceptions;

/// <summary>
/// Represents an error caused by an invalid request sent to the Google Maps API.
/// </summary>
/// <remarks>
/// This exception is typically thrown when required parameters are missing
/// or when the request contains invalid values.
/// </remarks>
public class MapsInvalidRequestException : MapsApiException {
    private const string DefaultMessage = "The request sent to Google Maps API contains invalid or missing parameters.";

    /// <summary>
    /// Initializes a new instance of the <see cref="MapsInvalidRequestException"/> class.
    /// </summary>
    /// <param name="message">The error message, or <c>null</c> to use the default message.</param>
    /// <param name="status">The HTTP status code returned by the Maps API, if any.</param>
    /// <param name="response">The raw response body returned by the Maps API, if any.</param>
    /// <param name="inner">The exception that caused this one, if any.</param>
    public MapsInvalidRequestException(
        string? message = null,
        HttpStatusCode? status = null,
        string? response = null,
        Exception? inner = null
    ) : base(message ?? DefaultMessage, status, response, inner) { }
}