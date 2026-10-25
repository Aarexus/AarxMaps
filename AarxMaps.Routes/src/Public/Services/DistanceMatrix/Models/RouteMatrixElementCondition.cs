namespace AarxMaps.Routes.Services.DistanceMatrix;

/// <summary>
/// Represents whether a route was found for a route matrix element.
/// </summary>
public enum RouteMatrixElementCondition {
    /// <summary>
    /// A value Google returned that this SDK does not recognize, or no condition because the element
    /// failed (see <see cref="RouteMatrixElement.Status"/>).
    /// </summary>
    Unknown,

    /// <summary>A route was found, and the element's metrics are filled.</summary>
    RouteExists,

    /// <summary>No route could be found between the origin and the destination.</summary>
    RouteNotFound
}
