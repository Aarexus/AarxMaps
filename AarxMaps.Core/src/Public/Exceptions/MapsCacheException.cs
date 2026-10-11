using System.Net;

namespace AarxMaps.Core.Exceptions;

/// <summary>
/// Represents errors that occur during AarxMaps caching operations.
/// </summary>
public class MapsCacheException : MapsApiException {
    private const string DefaultMessage = "Error during caching-operation.";

    public MapsCacheException(
        string? message = null,
        HttpStatusCode? status = null,
        string? response = null,
        Exception? inner = null
    ) : base(message ?? DefaultMessage, status, response, inner) { }
}