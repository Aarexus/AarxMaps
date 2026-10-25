using AarxMaps.Core.Exceptions;
using AarxMaps.Routes.Common.Models;
using AarxMaps.Routes.Internal.Constants;
using AarxMaps.Routes.Internal.Validation;

namespace AarxMaps.Routes.Services.Directions;

/// <summary>
/// Provides a fluent API for constructing <see cref="RouteRequest"/> instances.
/// </summary>
/// <remarks>
/// <see cref="Build"/> rejects the combinations Google fails on or silently ignores, such as an arrival
/// time outside transit, or a routing preference with a travel mode that does not support it.
/// An unset travel mode counts as <see cref="TravelMode.Drive"/>, Google's default.
/// </remarks>
public sealed class RouteRequestBuilder {
    // -------------------- INIT --------------------
    private Waypoint? _origin;
    private Waypoint? _destination;
    private List<Waypoint> _intermediates = [];
    private TravelMode? _travelMode;
    private RoutingPreference? _routingPreference;
    private PolylineQuality? _polylineQuality;
    private DateTimeOffset? _departureTime;
    private DateTimeOffset? _arrivalTime;
    private bool _computeAlternativeRoutes;
    private bool _optimizeWaypointOrder;
    private RouteModifiers? _routeModifiers;
    private Units? _units;
    private TrafficModel? _trafficModel;
    private TransitPreferences? _transitPreferences;

    // -------------------- BUILD --------------------
    /// <summary>
    /// Builds a new <see cref="RouteRequest"/> instance.
    /// </summary>
    /// <returns>
    /// A fully configured <see cref="RouteRequest"/>.
    /// </returns>
    /// <exception cref="MapsInvalidRequestException">
    /// Thrown when the origin or destination is missing, or when the configuration is one Google
    /// rejects or ignores:
    /// <list type="bullet">
    /// <item><description>both a departure and an arrival time, or an arrival time without <see cref="TravelMode.Transit"/>;</description></item>
    /// <item><description>a routing preference without <see cref="TravelMode.Drive"/> or <see cref="TravelMode.TwoWheeler"/>;</description></item>
    /// <item><description>a traffic model without <see cref="TravelMode.Drive"/> and <see cref="RoutingPreference.TrafficAwareOptimal"/>;</description></item>
    /// <item><description>transit preferences without <see cref="TravelMode.Transit"/>;</description></item>
    /// <item><description>a waypoint heading, vehicle stopover or side of road without <see cref="TravelMode.Drive"/> or <see cref="TravelMode.TwoWheeler"/>;</description></item>
    /// <item><description>a via origin or destination, or via intermediates with waypoint order optimization;</description></item>
    /// <item><description>waypoint order optimization without intermediates.</description></item>
    /// </list>
    /// </exception>
    public RouteRequest Build() {
        // REQUIRED
        if (_origin is null)
            throw new MapsInvalidRequestException("Origin is required. Call From() before Build().");
        if (_destination is null)
            throw new MapsInvalidRequestException("Destination is required. Call To() before Build().");

        // MODE-DEPENDENT OPTIONS
        RoutesRequestValidator.ValidateModeOptions(
            _travelMode, _routingPreference, _trafficModel, _transitPreferences, _departureTime, _arrivalTime);

        // WAYPOINTS
        RoutesRequestValidator.ValidateTerminalWaypoint(_origin, _travelMode, "Origin");
        RoutesRequestValidator.ValidateTerminalWaypoint(_destination, _travelMode, "Destination");
        foreach (var intermediate in _intermediates) {
            RoutesRequestValidator.ValidateWaypointForMode(intermediate, _travelMode, "Intermediate");
            if (intermediate.Via && _optimizeWaypointOrder)
                throw new MapsInvalidRequestException("Via intermediates cannot be used with OptimizeWaypointOrder.");
        }
        if (_optimizeWaypointOrder && _intermediates.Count == 0)
            throw new MapsInvalidRequestException("OptimizeWaypointOrder requires at least one intermediate.");

        return new RouteRequest() {
            Origin = _origin,
            Destination = _destination,
            Intermediates = [.. _intermediates],
            TravelMode = _travelMode,
            RoutingPreference = _routingPreference,
            PolylineQuality = _polylineQuality,
            DepartureTime = _departureTime,
            ArrivalTime = _arrivalTime,
            ComputeAlternativeRoutes = _computeAlternativeRoutes,
            OptimizeWaypointOrder = _optimizeWaypointOrder,
            RouteModifiers = _routeModifiers,
            Units = _units,
            TrafficModel = _trafficModel,
            TransitPreferences = _transitPreferences
        };
    }

    // -------------------- CONFIG --------------------
    // WAYPOINTS

    /// <summary>
    /// Sets the waypoint the route starts at. Required.
    /// </summary>
    /// <param name="origin">The origin, built with <see cref="WaypointBuilder"/>. Cannot be a via waypoint.</param>
    /// <returns>The current <see cref="RouteRequestBuilder"/> instance.</returns>
    public RouteRequestBuilder From(Waypoint origin) {
        _origin = origin;
        return this;
    }

    /// <summary>
    /// Sets the waypoint the route ends at. Required.
    /// </summary>
    /// <param name="destination">The destination, built with <see cref="WaypointBuilder"/>. Cannot be a via waypoint.</param>
    /// <returns>The current <see cref="RouteRequestBuilder"/> instance.</returns>
    public RouteRequestBuilder To(Waypoint destination) {
        _destination = destination;
        return this;
    }

    /// <summary>
    /// Replaces the intermediate waypoints, in visiting order.
    /// </summary>
    /// <param name="intermediates">Up to 25 waypoints. The values are copied.</param>
    /// <returns>The current <see cref="RouteRequestBuilder"/> instance.</returns>
    /// <remarks>
    /// Google returns no alternative routes for requests with intermediates.
    /// </remarks>
    /// <exception cref="MapsInvalidRequestException">
    /// Thrown when the collection is null, contains null values or has more than 25 waypoints.
    /// </exception>
    public RouteRequestBuilder WithIntermediates(IEnumerable<Waypoint> intermediates) {
        if (intermediates is null)
            throw new MapsInvalidRequestException("Intermediates cannot be null.");

        List<Waypoint> copy = [.. intermediates];
        if (copy.Contains(null!))
            throw new MapsInvalidRequestException("Intermediates cannot contain null values.");
        if (copy.Count > RoutesConstants.MaxIntermediates)
            throw new MapsInvalidRequestException($"At most {RoutesConstants.MaxIntermediates} Intermediates can be specified.");

        _intermediates = copy;
        return this;
    }

    /// <summary>
    /// Appends an intermediate waypoint after the ones already added.
    /// </summary>
    /// <param name="intermediate">The waypoint, built with <see cref="WaypointBuilder"/>.</param>
    /// <returns>The current <see cref="RouteRequestBuilder"/> instance.</returns>
    /// <exception cref="MapsInvalidRequestException">
    /// Thrown when the waypoint is null or 25 intermediates were already added.
    /// </exception>
    public RouteRequestBuilder AddIntermediate(Waypoint intermediate) {
        if (intermediate is null)
            throw new MapsInvalidRequestException("Intermediate cannot be null.");
        if (_intermediates.Count == RoutesConstants.MaxIntermediates)
            throw new MapsInvalidRequestException($"At most {RoutesConstants.MaxIntermediates} Intermediates can be specified.");

        _intermediates.Add(intermediate);
        return this;
    }

    // ROUTING

    /// <summary>
    /// Sets the mode of transportation. Defaults to <see cref="TravelMode.Drive"/>.
    /// </summary>
    /// <param name="travelMode">The travel mode.</param>
    /// <returns>The current <see cref="RouteRequestBuilder"/> instance.</returns>
    /// <exception cref="MapsInvalidRequestException">Thrown when the value is <see cref="TravelMode.Unknown"/>.</exception>
    public RouteRequestBuilder WithTravelMode(TravelMode travelMode) {
        _travelMode = RoutesRequestValidator.ValidateTravelMode(travelMode);
        return this;
    }

    /// <summary>
    /// Sets how traffic is taken into account. <see cref="TravelMode.Drive"/> and <see cref="TravelMode.TwoWheeler"/> only.
    /// </summary>
    /// <param name="routingPreference">The routing preference. Traffic-aware preferences are billed at a higher rate.</param>
    /// <returns>The current <see cref="RouteRequestBuilder"/> instance.</returns>
    public RouteRequestBuilder WithRoutingPreference(RoutingPreference routingPreference) {
        _routingPreference = routingPreference;
        return this;
    }

    /// <summary>
    /// Sets the conditions the route should satisfy, such as roads to avoid.
    /// </summary>
    /// <param name="routeModifiers">The route modifiers.</param>
    /// <returns>The current <see cref="RouteRequestBuilder"/> instance.</returns>
    public RouteRequestBuilder WithRouteModifiers(RouteModifiers routeModifiers) {
        _routeModifiers = routeModifiers;
        return this;
    }

    /// <summary>
    /// Sets the assumptions used for travel time in traffic.
    /// <see cref="TravelMode.Drive"/> with <see cref="RoutingPreference.TrafficAwareOptimal"/> only.
    /// </summary>
    /// <param name="trafficModel">The traffic model.</param>
    /// <returns>The current <see cref="RouteRequestBuilder"/> instance.</returns>
    public RouteRequestBuilder WithTrafficModel(TrafficModel trafficModel) {
        _trafficModel = trafficModel;
        return this;
    }

    /// <summary>
    /// Sets the preferences for transit routes. <see cref="TravelMode.Transit"/> only.
    /// </summary>
    /// <param name="transitPreferences">The transit preferences.</param>
    /// <returns>The current <see cref="RouteRequestBuilder"/> instance.</returns>
    public RouteRequestBuilder WithTransitPreferences(TransitPreferences transitPreferences) {
        _transitPreferences = transitPreferences;
        return this;
    }

    // TIME

    /// <summary>
    /// Sets the departure time. Defaults to the time of the request.
    /// </summary>
    /// <param name="departureTime">The departure time. It is sent in UTC with whole-second precision.</param>
    /// <returns>The current <see cref="RouteRequestBuilder"/> instance.</returns>
    /// <remarks>
    /// A time in the past is only accepted for <see cref="TravelMode.Transit"/>. Transit times must be
    /// within 7 days in the past and 100 days in the future. Mutually exclusive with <see cref="WithArrivalTime"/>.
    /// </remarks>
    public RouteRequestBuilder WithDepartureTime(DateTimeOffset departureTime) {
        _departureTime = departureTime;
        return this;
    }

    /// <summary>
    /// Sets the desired arrival time. <see cref="TravelMode.Transit"/> only.
    /// </summary>
    /// <param name="arrivalTime">The arrival time. It is sent in UTC with whole-second precision.</param>
    /// <returns>The current <see cref="RouteRequestBuilder"/> instance.</returns>
    /// <remarks>
    /// Must be within 7 days in the past and 100 days in the future. Mutually exclusive with <see cref="WithDepartureTime"/>.
    /// </remarks>
    public RouteRequestBuilder WithArrivalTime(DateTimeOffset arrivalTime) {
        _arrivalTime = arrivalTime;
        return this;
    }

    // RESPONSE

    /// <summary>
    /// Sets how detailed the returned polylines are. Defaults to <see cref="PolylineQuality.Overview"/>.
    /// </summary>
    /// <param name="polylineQuality">The polyline quality.</param>
    /// <returns>The current <see cref="RouteRequestBuilder"/> instance.</returns>
    public RouteRequestBuilder WithPolylineQuality(PolylineQuality polylineQuality) {
        _polylineQuality = polylineQuality;
        return this;
    }

    /// <summary>
    /// Sets the units of measure for display text. Defaults to the units of the origin's country.
    /// </summary>
    /// <param name="units">The units. Distances in meters and durations are not affected.</param>
    /// <returns>The current <see cref="RouteRequestBuilder"/> instance.</returns>
    public RouteRequestBuilder WithUnits(Units units) {
        _units = units;
        return this;
    }

    /// <summary>
    /// Requests up to two alternative routes in addition to the main route.
    /// </summary>
    /// <returns>The current <see cref="RouteRequestBuilder"/> instance.</returns>
    /// <remarks>
    /// Google returns no alternatives for requests with intermediates.
    /// </remarks>
    public RouteRequestBuilder WithAlternativeRoutes() {
        _computeAlternativeRoutes = true;
        return this;
    }

    /// <summary>
    /// Lets Google reorder the intermediate waypoints to minimize the route's cost.
    /// </summary>
    /// <returns>The current <see cref="RouteRequestBuilder"/> instance.</returns>
    /// <remarks>
    /// The new order is returned in <see cref="Route.OptimizedWaypointOrder"/>. Requires at least one
    /// intermediate and no via intermediates. Billed at a higher rate.
    /// </remarks>
    public RouteRequestBuilder WithOptimizedWaypointOrder() {
        _optimizeWaypointOrder = true;
        return this;
    }
}
