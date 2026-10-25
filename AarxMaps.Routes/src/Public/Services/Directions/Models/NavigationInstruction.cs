namespace AarxMaps.Routes.Services.Directions;

/// <summary>
/// Represents what to do at a navigation step or segment.
/// </summary>
public sealed class NavigationInstruction {
    /// <summary>
    /// Gets the action to take, for example to pick the icon to show.
    /// </summary>
    public Maneuver Maneuver { get; internal set; }

    /// <summary>
    /// Gets the instruction text (e.g., "Turn left onto Main St"), in the request language.
    /// </summary>
    public string? Instructions { get; internal set; }
}
