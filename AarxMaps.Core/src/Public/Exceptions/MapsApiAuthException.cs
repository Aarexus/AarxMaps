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

    public MapsApiAuthException(
        string? message = null,
        HttpStatusCode? status = null,
        string? response = null,
        Exception? inner = null
    ) : base(message ?? DefaultMessage, status, response, inner) { }
}