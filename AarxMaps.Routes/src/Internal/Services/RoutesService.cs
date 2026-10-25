using AarxMaps.Routes.Services;
using AarxMaps.Routes.Services.Directions;
using AarxMaps.Routes.Services.DistanceMatrix;

namespace AarxMaps.Routes.Internal.Services;

internal sealed class RoutesService : IRoutesService {
    // -------------------- INIT --------------------
    public IDirectionsService Directions { get; }
    public IDistanceMatrixService DistanceMatrix { get; }

    public RoutesService(IDirectionsService directionsService, IDistanceMatrixService distanceMatrixService) {
        Directions = directionsService;
        DistanceMatrix = distanceMatrixService;
    }
}
