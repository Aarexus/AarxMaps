using AarxMaps.Routes.Services.Directions;
using AarxMaps.Routes.Services.DistanceMatrix;

namespace AarxMaps.Routes.Services;

/// <summary>
/// Provides a unified entry point for accessing all Routes-related services.
/// </summary>
/// <remarks>
/// This interface aggregates the different domain services of the Routes module,
/// allowing consumers to access directions and route matrix functionality
/// through a single abstraction.
/// </remarks>
public interface IRoutesService {
    /// <summary>
    /// Gets the directions service used to compute routes between locations.
    /// </summary>
    IDirectionsService Directions { get; }

    /// <summary>
    /// Gets the distance matrix service used to compute distances and durations
    /// between multiple origins and destinations.
    /// </summary>
    IDistanceMatrixService DistanceMatrix { get; }
}
