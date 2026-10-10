using Microsoft.Extensions.Options;

namespace AarxMaps.Core.Configuration;

/// <summary>
/// Configuration options for AarxMaps HTTP caching.
/// </summary>
public sealed class MapsCachingOptions {
    /// <summary>
    /// Prefix used for all generated cache keys.
    ///
    /// Useful for:
    /// <list type="bullet">
    /// <item><description>Multi-tenant environments</description></item>
    /// <item><description>Shared Redis instances</description></item>
    /// <item><description>Cache key isolation between applications</description></item>
    /// </list>
    ///
    /// Default value: <c>"aarxMaps"</c>.
    /// </summary>
    public string Prefix { get; set; } = "aarxMaps";

    /// <summary>
    /// Absolute expiration time for cached responses.
    ///
    /// Once reached, the cache entry is permanently removed.
    /// </summary>
    public TimeSpan AbsoluteExpiration { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Sliding expiration time for cached responses.
    ///
    /// The expiration timer resets every time the cache entry is accessed.
    /// </summary>
    public TimeSpan SlidingExpiration { get; set; } = TimeSpan.FromMinutes(30);
}