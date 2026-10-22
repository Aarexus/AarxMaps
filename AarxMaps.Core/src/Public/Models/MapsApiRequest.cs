using AarxMaps.Core.Enums;

namespace AarxMaps.Core.Models;

/// <summary>
/// Represents a request definition used to call a Maps API endpoint.
/// </summary>
/// <remarks>
/// This model describes all the information required to construct an HTTP request,
/// including the HTTP method, base URL, endpoint path, headers, query parameters,
/// and an optional request payload.
/// </remarks>
public sealed class MapsApiRequest {
    /// <summary>
    /// Gets the HTTP method used to send the request.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="HttpMethod.Get"/>.
    /// </remarks>
    public HttpMethod Method { get; init; } = HttpMethod.Get;

    /// <summary>
    /// Gets the location where the API key will be included in the request.
    /// </summary>
    /// <remarks>
    /// By default, the API key is sent in the request headers using the 
    /// <c>X-Goog-Api-Key</c> header. Some endpoints (such as media/photo endpoints)
    /// require the API key to be passed as a query parameter instead.
    /// </remarks>
    /// <example>
    /// ApiKeyLocation.Header
    /// </example>
    public ApiKeyLocation ApiKeyLocation { get; init; } = ApiKeyLocation.Header;

    /// <summary>
    /// Gets the base URL of the API endpoint.
    /// </summary>
    /// <example>
    /// https://places.googleapis.com/v1/
    /// </example>
    public Uri BaseUrl { get; init; } = null!;

    /// <summary>
    /// Gets the relative endpoint path for the request.
    /// </summary>
    /// <example>
    /// places:autocomplete
    /// </example>
    public string Endpoint { get; init; } = null!;

    /// <summary>
    /// Gets the HTTP headers that should be included in the request.
    /// </summary>
    public IDictionary<string, string>? Headers { get; init; }

    /// <summary>
    /// Gets the query parameters appended to the request URL.
    /// </summary>
    public IDictionary<string, string>? Query { get; init; }

    /// <summary>
    /// Gets the request payload that will be serialized as JSON.
    /// </summary>
    /// <remarks>
    /// This value is typically used for POST requests where the API expects
    /// a JSON body.
    /// </remarks>
    public object? Payload { get; init; }

    /// <summary>
    /// Gets whether a caching layer may store and serve the response to this request.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>true</c>. Set it to <c>false</c> for responses that must always be fresh,
    /// such as content Google does not allow to be cached (for example Places photo names,
    /// which expire). Has no effect when no cache layer is registered.
    /// </remarks>
    public bool Cacheable { get; init; } = true;
}