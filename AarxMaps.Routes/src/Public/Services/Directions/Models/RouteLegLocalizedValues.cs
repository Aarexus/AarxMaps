namespace AarxMaps.Routes.Services.Directions;

/// <summary>
/// Represents display text for the totals of a leg.
/// </summary>
/// <remarks>
/// Formatted in the request language and units, ready to show. Do not parse them.
/// </remarks>
public sealed class RouteLegLocalizedValues {
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
}
