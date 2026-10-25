namespace AarxMaps.Routes.Common.Models;

/// <summary>
/// Represents preferences for routes computed with <see cref="TravelMode.Transit"/>.
/// </summary>
/// <remarks>
/// Immutable. Only valid when the travel mode is <see cref="TravelMode.Transit"/>.
/// </remarks>
public sealed class TransitPreferences {
    /// <summary>
    /// Initializes a new instance of the <see cref="TransitPreferences"/> class.
    /// </summary>
    /// <param name="allowedTravelModes">
    /// The kinds of transit to use, or <c>null</c> or empty to allow all of them. The values are copied.
    /// </param>
    /// <param name="routingPreference">
    /// What the route should optimize for, or <c>null</c> for Google's default.
    /// </param>
    public TransitPreferences(
        IEnumerable<TransitTravelMode>? allowedTravelModes = null,
        TransitRoutingPreference? routingPreference = null
    ) {
        AllowedTravelModes = [.. allowedTravelModes ?? []];
        RoutingPreference = routingPreference;
    }

    /// <summary>
    /// Gets the kinds of transit the route may use. Empty means all of them.
    /// </summary>
    public IReadOnlyList<TransitTravelMode> AllowedTravelModes { get; }

    /// <summary>
    /// Gets what the route should optimize for, or <c>null</c> for Google's default.
    /// </summary>
    public TransitRoutingPreference? RoutingPreference { get; }
}
