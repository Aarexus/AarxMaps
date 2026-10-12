namespace AarxMaps.Core.Configuration;

/// <summary>
/// Represents the configuration settings used by the Maps SDK.
/// </summary>
/// <remarks>
/// This configuration defines authentication, localization, request timeout,
/// and logging behavior for all Maps API operations.
/// </remarks>
public sealed class MapsConfiguration {
    /// <summary>
    /// The API key used to authenticate requests to the Maps API.
    /// </summary>
    public string ApiKey { get; set; } = default!;

    /// <summary>
    /// The preferred language used for API responses.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>en</c>.
    /// </remarks>
    public string Language { get; set; } = "en";

    /// <summary>
    /// The region code used to bias API responses.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>US</c>.
    /// </remarks>
    public string Region { get; set; } = "US";

    /// <summary>
    /// The request timeout for API calls.
    /// </summary>
    /// <remarks>
    /// Defaults to 30 seconds.
    /// </remarks>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
}