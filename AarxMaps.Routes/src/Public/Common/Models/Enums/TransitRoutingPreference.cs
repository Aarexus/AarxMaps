namespace AarxMaps.Routes.Common.Models;

/// <summary>
/// Specifies what a transit route should optimize for.
/// </summary>
public enum TransitRoutingPreference {
    /// <summary>Prefers routes with less walking.</summary>
    LessWalking,

    /// <summary>Prefers routes with fewer transfers.</summary>
    FewerTransfers
}
