namespace AarxMaps.Routes.Services.Directions;

/// <summary>
/// Represents a characteristic Google assigned to a route.
/// </summary>
public enum RouteLabel {
    /// <summary>A value Google returned that this SDK does not recognize.</summary>
    Unknown,

    /// <summary>The main ("best") route Google computed.</summary>
    DefaultRoute,

    /// <summary>An alternative to the default route, returned when alternative routes are requested.</summary>
    DefaultRouteAlternate,

    /// <summary>A route optimized for fuel consumption.</summary>
    FuelEfficient,

    /// <summary>A route optimized for travel distance.</summary>
    ShorterDistance
}
