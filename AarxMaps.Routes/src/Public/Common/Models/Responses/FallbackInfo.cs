namespace AarxMaps.Routes.Common.Models;

/// <summary>
/// Explains that Google could not honor the requested routing preference and how the result
/// was computed instead.
/// </summary>
/// <remarks>
/// Present only when a fallback happened. Durations and traffic data then reflect
/// <see cref="RoutingMode"/>, not the requested preference.
/// </remarks>
public sealed class FallbackInfo {
    /// <summary>
    /// Gets the routing mode used for the result.
    /// </summary>
    public FallbackRoutingMode RoutingMode { get; internal set; }

    /// <summary>
    /// Gets why the requested routing preference was not used.
    /// </summary>
    public FallbackReason Reason { get; internal set; }
}
