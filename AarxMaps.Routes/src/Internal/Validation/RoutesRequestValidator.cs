using AarxMaps.Core.Exceptions;
using AarxMaps.Routes.Common.Models;

namespace AarxMaps.Routes.Internal.Validation;

// Rules Google applies per travel mode, shared by the Directions and DistanceMatrix builders.
// An unset travel mode is Google's default, DRIVE.
internal static class RoutesRequestValidator {
    internal static TravelMode ValidateTravelMode(TravelMode travelMode)
        => travelMode == TravelMode.Unknown
            ? throw new MapsInvalidRequestException("TravelMode.Unknown cannot be sent to the API.")
            : travelMode;

    internal static void ValidateModeOptions(
        TravelMode? travelMode, RoutingPreference? routingPreference, TrafficModel? trafficModel,
        TransitPreferences? transitPreferences, DateTimeOffset? departureTime, DateTimeOffset? arrivalTime
    ) {
        if (departureTime is not null && arrivalTime is not null)
            throw new MapsInvalidRequestException("Only one of DepartureTime or ArrivalTime can be set.");

        // Google ignores the arrival time outside transit instead of failing
        if (arrivalTime is not null && travelMode is not TravelMode.Transit)
            throw new MapsInvalidRequestException("ArrivalTime can only be used when TravelMode is Transit.");

        if (routingPreference is not null && !IsDriveOrTwoWheeler(travelMode))
            throw new MapsInvalidRequestException("RoutingPreference can only be used when TravelMode is Drive or TwoWheeler.");

        if (trafficModel is not null
            && (travelMode is not (null or TravelMode.Drive) || routingPreference is not RoutingPreference.TrafficAwareOptimal))
            throw new MapsInvalidRequestException(
                "TrafficModel can only be used when TravelMode is Drive and RoutingPreference is TrafficAwareOptimal.");

        if (transitPreferences is not null && travelMode is not TravelMode.Transit)
            throw new MapsInvalidRequestException("TransitPreferences can only be used when TravelMode is Transit.");
    }

    // Origins and destinations: via is only allowed on intermediates
    internal static void ValidateTerminalWaypoint(Waypoint waypoint, TravelMode? travelMode, string name) {
        if (waypoint.Via)
            throw new MapsInvalidRequestException($"{name} cannot be a via waypoint. Via is only allowed on intermediates.");

        ValidateWaypointForMode(waypoint, travelMode, name);
    }

    internal static void ValidateWaypointForMode(Waypoint waypoint, TravelMode? travelMode, string name) {
        if (IsDriveOrTwoWheeler(travelMode)) return;

        if (waypoint.Location?.Heading is not null || waypoint.VehicleStopover || waypoint.SideOfRoad)
            throw new MapsInvalidRequestException(
                $"{name}: Heading, VehicleStopover and SideOfRoad can only be used when TravelMode is Drive or TwoWheeler.");
    }

    private static bool IsDriveOrTwoWheeler(TravelMode? travelMode)
        => travelMode is null or TravelMode.Drive or TravelMode.TwoWheeler;
}
