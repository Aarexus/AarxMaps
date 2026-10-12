namespace AarxMaps.Core.Enums;

/// <summary>
/// Specifies where the API key should be included in an HTTP request.
/// </summary>
public enum ApiKeyLocation {   
    /// <summary>
    /// Indicates that the API key is sent in the request headers
    /// using the <c>X-Goog-Api-Key</c> header.
    /// </summary>
    Header,

    /// <summary>
    /// Indicates that the API key is sent as a query parameter
    /// using the <c>key</c> parameter.
    /// </summary>
    Query
}