namespace AarxMaps.Routes.Common.Models;

/// <summary>
/// Specifies the units of measure used in display text.
/// </summary>
/// <remarks>
/// Affects localized values and navigation instructions only. Distances in meters and durations
/// are never converted. When unset, Google infers the units from the location of the origin.
/// </remarks>
public enum Units {
    /// <summary>Metric units (kilometers, meters).</summary>
    Metric,

    /// <summary>Imperial units (miles, feet).</summary>
    Imperial
}
