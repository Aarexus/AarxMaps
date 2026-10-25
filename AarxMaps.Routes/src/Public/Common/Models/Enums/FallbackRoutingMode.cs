namespace AarxMaps.Routes.Common.Models;

/// <summary>
/// Represents the routing mode Google actually used when it fell back from the requested one.
/// </summary>
public enum FallbackRoutingMode {
    /// <summary>A value Google returned that this SDK does not recognize.</summary>
    Unknown,

    /// <summary>The result was computed without live traffic.</summary>
    FallbackTrafficUnaware,

    /// <summary>The result was computed with live traffic.</summary>
    FallbackTrafficAware
}
