namespace AarxMaps.Routes.Common.Models;

/// <summary>
/// Specifies how much of the response is requested from Google (the field mask).
/// </summary>
/// <remarks>
/// Each service documents which fields every level includes. Fields outside the level are left empty.
/// </remarks>
public enum RouteDetailLevel {
    /// <summary>Totals only: distance, durations and the fields needed to tell results apart.</summary>
    Summary,

    /// <summary>Totals plus geometry and per-leg totals. The default.</summary>
    Standard,

    /// <summary>Everything the SDK maps, including steps and transit details.</summary>
    Full
}
