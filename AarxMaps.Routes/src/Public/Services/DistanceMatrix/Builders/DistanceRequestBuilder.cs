using AarxMaps.Core.Exceptions;
using AarxMaps.Routes.Common.Models;
using AarxMaps.Routes.Internal.Constants;
using AarxMaps.Routes.Internal.Validation;

namespace AarxMaps.Routes.Services.DistanceMatrix;

/// <summary>
/// Provides a fluent API for constructing <see cref="DistanceRequest"/> instances.
/// </summary>
/// <remarks>
/// <see cref="Build"/> checks Google's size limits and rejects the combinations Google fails on or
/// silently ignores. An unset travel mode counts as <see cref="TravelMode.Drive"/>, Google's default.
/// <para><b>Limits:</b> origins × destinations is at most 625, or 100 with
/// <see cref="RoutingPreference.TrafficAwareOptimal"/> or <see cref="TravelMode.Transit"/>. At most 50
/// origins and destinations together can be given as a place ID or an address.</para>
/// </remarks>
public sealed class DistanceRequestBuilder {
    // -------------------- INIT --------------------
    private List<RouteMatrixOrigin> _origins = [];
    private List<Waypoint> _destinations = [];
    private TravelMode? _travelMode;
    private RoutingPreference? _routingPreference;
    private DateTimeOffset? _departureTime;
    private DateTimeOffset? _arrivalTime;
    private Units? _units;
    private TrafficModel? _trafficModel;
    private TransitPreferences? _transitPreferences;

    // -------------------- BUILD --------------------
    /// <summary>
    /// Builds a new <see cref="DistanceRequest"/> instance.
    /// </summary>
    /// <returns>
    /// A fully configured <see cref="DistanceRequest"/>.
    /// </returns>
    /// <exception cref="MapsInvalidRequestException">
    /// Thrown when there are no origins or no destinations, a size limit is exceeded, or the configuration
    /// is one Google rejects or ignores:
    /// <list type="bullet">
    /// <item><description>both a departure and an arrival time, or an arrival time without <see cref="TravelMode.Transit"/>;</description></item>
    /// <item><description>a routing preference without <see cref="TravelMode.Drive"/> or <see cref="TravelMode.TwoWheeler"/>;</description></item>
    /// <item><description>a traffic model without <see cref="TravelMode.Drive"/> and <see cref="RoutingPreference.TrafficAwareOptimal"/>;</description></item>
    /// <item><description>transit preferences without <see cref="TravelMode.Transit"/>;</description></item>
    /// <item><description>a waypoint heading, vehicle stopover or side of road without <see cref="TravelMode.Drive"/> or <see cref="TravelMode.TwoWheeler"/>;</description></item>
    /// <item><description>a via waypoint (via is only allowed on directions intermediates).</description></item>
    /// </list>
    /// </exception>
    public DistanceRequest Build() {
        // REQUIRED
        if (_origins.Count == 0)
            throw new MapsInvalidRequestException("At least one origin is required. Call AddOrigin() or WithOrigins() before Build().");
        if (_destinations.Count == 0)
            throw new MapsInvalidRequestException("At least one destination is required. Call AddDestination() or WithDestinations() before Build().");

        // MODE-DEPENDENT OPTIONS
        RoutesRequestValidator.ValidateModeOptions(
            _travelMode, _routingPreference, _trafficModel, _transitPreferences, _departureTime, _arrivalTime);

        // WAYPOINTS
        foreach (var origin in _origins)
            RoutesRequestValidator.ValidateTerminalWaypoint(origin.Waypoint, _travelMode, "Origin");
        foreach (var destination in _destinations)
            RoutesRequestValidator.ValidateTerminalWaypoint(destination, _travelMode, "Destination");

        // LIMITS
        var maxElements = _routingPreference is RoutingPreference.TrafficAwareOptimal || _travelMode is TravelMode.Transit
            ? RoutesConstants.MaxMatrixElementsRestricted : RoutesConstants.MaxMatrixElements;
        if (_origins.Count * _destinations.Count > maxElements)
            throw new MapsInvalidRequestException(
                $"Origins x destinations must be at most {maxElements} for this travel mode and routing preference.");

        var byPlaceIdOrAddress = _origins.Count(o => IsPlaceIdOrAddress(o.Waypoint)) + _destinations.Count(IsPlaceIdOrAddress);
        if (byPlaceIdOrAddress > RoutesConstants.MaxMatrixPlaceIdOrAddressWaypoints)
            throw new MapsInvalidRequestException(
                $"At most {RoutesConstants.MaxMatrixPlaceIdOrAddressWaypoints} origins and destinations together can be given as a PlaceId or an Address.");

        return new DistanceRequest() {
            Origins = [.. _origins],
            Destinations = [.. _destinations],
            TravelMode = _travelMode,
            RoutingPreference = _routingPreference,
            DepartureTime = _departureTime,
            ArrivalTime = _arrivalTime,
            Units = _units,
            TrafficModel = _trafficModel,
            TransitPreferences = _transitPreferences
        };
    }

    // -------------------- CONFIG --------------------
    // WAYPOINTS

    /// <summary>
    /// Replaces the origins, one per row of the matrix.
    /// </summary>
    /// <param name="origins">The origin waypoints. The values are copied.</param>
    /// <param name="routeModifiers">Optional conditions applied to every route from these origins.</param>
    /// <returns>The current <see cref="DistanceRequestBuilder"/> instance.</returns>
    /// <exception cref="MapsInvalidRequestException">Thrown when the collection is null or contains null values.</exception>
    public DistanceRequestBuilder WithOrigins(IEnumerable<Waypoint> origins, RouteModifiers? routeModifiers = null) {
        _origins = [.. CopyWaypoints(origins, "Origins")
            .Select(waypoint => new RouteMatrixOrigin() { Waypoint = waypoint, RouteModifiers = routeModifiers })];
        return this;
    }

    /// <summary>
    /// Appends an origin after the ones already added.
    /// </summary>
    /// <param name="origin">The origin waypoint, built with <see cref="WaypointBuilder"/>.</param>
    /// <param name="routeModifiers">Optional conditions applied to every route from this origin.</param>
    /// <returns>The current <see cref="DistanceRequestBuilder"/> instance.</returns>
    /// <exception cref="MapsInvalidRequestException">Thrown when the waypoint is null.</exception>
    public DistanceRequestBuilder AddOrigin(Waypoint origin, RouteModifiers? routeModifiers = null) {
        if (origin is null)
            throw new MapsInvalidRequestException("Origin cannot be null.");

        _origins.Add(new RouteMatrixOrigin() { Waypoint = origin, RouteModifiers = routeModifiers });
        return this;
    }

    /// <summary>
    /// Replaces the destinations, one per column of the matrix.
    /// </summary>
    /// <param name="destinations">The destination waypoints. The values are copied.</param>
    /// <returns>The current <see cref="DistanceRequestBuilder"/> instance.</returns>
    /// <exception cref="MapsInvalidRequestException">Thrown when the collection is null or contains null values.</exception>
    public DistanceRequestBuilder WithDestinations(IEnumerable<Waypoint> destinations) {
        _destinations = CopyWaypoints(destinations, "Destinations");
        return this;
    }

    /// <summary>
    /// Appends a destination after the ones already added.
    /// </summary>
    /// <param name="destination">The destination waypoint, built with <see cref="WaypointBuilder"/>.</param>
    /// <returns>The current <see cref="DistanceRequestBuilder"/> instance.</returns>
    /// <exception cref="MapsInvalidRequestException">Thrown when the waypoint is null.</exception>
    public DistanceRequestBuilder AddDestination(Waypoint destination) {
        if (destination is null)
            throw new MapsInvalidRequestException("Destination cannot be null.");

        _destinations.Add(destination);
        return this;
    }

    // ROUTING

    /// <summary>
    /// Sets the mode of transportation. Defaults to <see cref="TravelMode.Drive"/>.
    /// </summary>
    /// <param name="travelMode">The travel mode.</param>
    /// <returns>The current <see cref="DistanceRequestBuilder"/> instance.</returns>
    /// <exception cref="MapsInvalidRequestException">Thrown when the value is <see cref="TravelMode.Unknown"/>.</exception>
    public DistanceRequestBuilder WithTravelMode(TravelMode travelMode) {
        _travelMode = RoutesRequestValidator.ValidateTravelMode(travelMode);
        return this;
    }

    /// <summary>
    /// Sets how traffic is taken into account. <see cref="TravelMode.Drive"/> and <see cref="TravelMode.TwoWheeler"/> only.
    /// </summary>
    /// <param name="routingPreference">
    /// The routing preference. Traffic-aware preferences are billed at a higher rate, and
    /// <see cref="RoutingPreference.TrafficAwareOptimal"/> limits the matrix to 100 elements.
    /// </param>
    /// <returns>The current <see cref="DistanceRequestBuilder"/> instance.</returns>
    public DistanceRequestBuilder WithRoutingPreference(RoutingPreference routingPreference) {
        _routingPreference = routingPreference;
        return this;
    }

    /// <summary>
    /// Sets the assumptions used for travel time in traffic.
    /// <see cref="TravelMode.Drive"/> with <see cref="RoutingPreference.TrafficAwareOptimal"/> only.
    /// </summary>
    /// <param name="trafficModel">The traffic model.</param>
    /// <returns>The current <see cref="DistanceRequestBuilder"/> instance.</returns>
    public DistanceRequestBuilder WithTrafficModel(TrafficModel trafficModel) {
        _trafficModel = trafficModel;
        return this;
    }

    /// <summary>
    /// Sets the preferences for transit routes. <see cref="TravelMode.Transit"/> only.
    /// </summary>
    /// <param name="transitPreferences">The transit preferences.</param>
    /// <returns>The current <see cref="DistanceRequestBuilder"/> instance.</returns>
    public DistanceRequestBuilder WithTransitPreferences(TransitPreferences transitPreferences) {
        _transitPreferences = transitPreferences;
        return this;
    }

    // TIME

    /// <summary>
    /// Sets the departure time. Defaults to the time of the request.
    /// </summary>
    /// <param name="departureTime">The departure time. It is sent in UTC with whole-second precision.</param>
    /// <returns>The current <see cref="DistanceRequestBuilder"/> instance.</returns>
    /// <remarks>
    /// A time in the past is only accepted for <see cref="TravelMode.Transit"/>. Transit times must be
    /// within 7 days in the past and 100 days in the future. Mutually exclusive with <see cref="WithArrivalTime"/>.
    /// </remarks>
    public DistanceRequestBuilder WithDepartureTime(DateTimeOffset departureTime) {
        _departureTime = departureTime;
        return this;
    }

    /// <summary>
    /// Sets the desired arrival time. <see cref="TravelMode.Transit"/> only.
    /// </summary>
    /// <param name="arrivalTime">The arrival time. It is sent in UTC with whole-second precision.</param>
    /// <returns>The current <see cref="DistanceRequestBuilder"/> instance.</returns>
    /// <remarks>
    /// Must be within 7 days in the past and 100 days in the future. Mutually exclusive with <see cref="WithDepartureTime"/>.
    /// </remarks>
    public DistanceRequestBuilder WithArrivalTime(DateTimeOffset arrivalTime) {
        _arrivalTime = arrivalTime;
        return this;
    }

    // RESPONSE

    /// <summary>
    /// Sets the units of measure for display text. Defaults to the units of the first origin's country.
    /// </summary>
    /// <param name="units">The units. Distances in meters and durations are not affected.</param>
    /// <returns>The current <see cref="DistanceRequestBuilder"/> instance.</returns>
    public DistanceRequestBuilder WithUnits(Units units) {
        _units = units;
        return this;
    }

    // -------------------- INNER METHS --------------------
    private static List<Waypoint> CopyWaypoints(IEnumerable<Waypoint> waypoints, string name) {
        if (waypoints is null)
            throw new MapsInvalidRequestException($"{name} cannot be null.");

        List<Waypoint> copy = [.. waypoints];
        if (copy.Contains(null!))
            throw new MapsInvalidRequestException($"{name} cannot contain null values.");

        return copy;
    }

    private static bool IsPlaceIdOrAddress(Waypoint waypoint) => waypoint.Location is null;
}
