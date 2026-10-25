namespace AarxMaps.Routes.Internal.Constants;

internal static class RoutesConstants {
    internal const string FieldMaskHeader = "X-Goog-FieldMask";

    // computeRoutes
    internal const int MaxIntermediates = 25;

    // computeRouteMatrix
    internal const int MaxMatrixElements = 625;
    internal const int MaxMatrixElementsRestricted = 100; // TRAFFIC_AWARE_OPTIMAL or TRANSIT
    internal const int MaxMatrixPlaceIdOrAddressWaypoints = 50; // origins + destinations given as place ID or address
}
