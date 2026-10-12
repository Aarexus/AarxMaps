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

    public MapsInvalidRequestException(
        string? message = null,
        HttpStatusCode? status = null,
        string? response = null,
        Exception? inner = null
    ) : base(message ?? DefaultMessage, status, response, inner) { }
}