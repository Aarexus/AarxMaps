namespace AarxMaps.Routes.Services.Directions;

/// <summary>
/// Represents display text for a navigation step.
/// </summary>
/// <remarks>
/// Formatted in the request language and units, ready to show. Do not parse them.
/// </remarks>
public sealed class RouteLegStepLocalizedValues {
    /// <summary>
    /// Gets the formatted travel distance.
    /// </summary>
    public string? Distance { get; internal set; }

    /// <summary>
    /// Gets the formatted travel duration without traffic.
    /// </summary>
    public string? StaticDuration { get; internal set; }
}
