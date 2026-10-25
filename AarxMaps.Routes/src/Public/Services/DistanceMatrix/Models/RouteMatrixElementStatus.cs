namespace AarxMaps.Routes.Services.DistanceMatrix;

/// <summary>
/// Represents the error of a route matrix element that could not be computed.
/// </summary>
public sealed class RouteMatrixElementStatus {
    /// <summary>
    /// Gets the gRPC status code of the error (e.g., 3 for INVALID_ARGUMENT, 5 for NOT_FOUND).
    /// </summary>
    public int Code { get; internal set; }

    /// <summary>
    /// Gets the error message, in English.
    /// </summary>
    public string? Message { get; internal set; }
}
