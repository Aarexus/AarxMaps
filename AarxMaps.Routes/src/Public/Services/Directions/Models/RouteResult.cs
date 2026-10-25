using AarxMaps.Routes.Common.Models;

namespace AarxMaps.Routes.Services.Directions;

/// <summary>
/// Represents the result of a directions request.
/// </summary>
public sealed class RouteResult {
    /// <summary>
    /// Gets the computed routes. Empty when no route exists between the waypoints.
    /// </summary>
    /// <remarks>
    /// The route labeled <see cref="RouteLabel.DefaultRoute"/> (normally the first one) is the main route.
    /// Up to three routes are returned when alternative routes are requested.
    /// </remarks>
    public IReadOnlyList<Route> Routes { get; internal set; } = [];

    /// <summary>
    /// Gets how the result was computed when Google could not honor the requested routing preference,
    /// or <c>null</c> when no fallback happened.
    /// </summary>
    public FallbackInfo? FallbackInfo { get; internal set; }
}
