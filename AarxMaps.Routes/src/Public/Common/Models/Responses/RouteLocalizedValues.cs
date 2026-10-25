namespace AarxMaps.Routes.Common.Models;

/// <summary>
/// Represents display text for the totals of a route or a route matrix element.
/// </summary>
/// <remarks>
/// Formatted in the request language and in the requested <see cref="Units"/>, ready to show
/// (e.g., "12.3 km", "25 mins"). Do not parse them; use the numeric properties instead.
/// </remarks>
public sealed class RouteLocalizedValues {
    /// <summary>
    /// Gets the formatted travel distance.
    /// </summary>
    public string? Distance { get; internal set; }

    /// <summary>
    /// Gets the formatted travel duration, with traffic when it was requested.
    /// </summary>
    public string? Duration { get; internal set; }

    /// <summary>
    /// Gets the formatted travel duration without traffic.
    /// </summary>
    public string? StaticDuration { get; internal set; }

    /// <summary>
    /// Gets the formatted transit fare, when Google returns one.
    /// </summary>
    public string? TransitFare { get; internal set; }
}
