using System.Net;

namespace AarxMaps.Core.Exceptions;

/// <summary>
/// Represents an error indicating that the requested resource
/// could not be found in the Google Maps API.
/// </summary>
public class MapsNotFoundException : MapsApiException {
    private const string DefaultMessage = "The requested resource was not found in Google Maps API.";

    public MapsNotFoundException(
        string? message = null,
        HttpStatusCode? status = null,
        string? response = null,
        Exception? inner = null
    ) : base(message ?? DefaultMessage, status, response, inner) { }
}