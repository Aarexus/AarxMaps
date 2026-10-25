namespace AarxMaps.Routes.Common.Models;

/// <summary>
/// Specifies a kind of public transit allowed in a transit route.
/// </summary>
public enum TransitTravelMode {
    /// <summary>Travel by bus.</summary>
    Bus,

    /// <summary>Travel by subway.</summary>
    Subway,

    /// <summary>Travel by train.</summary>
    Train,

    /// <summary>Travel by light rail or tram.</summary>
    LightRail,

    /// <summary>Travel by rail. Equivalent to <see cref="Subway"/>, <see cref="Train"/> and <see cref="LightRail"/> together.</summary>
    Rail
}
