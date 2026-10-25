using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Models.Geometry;

namespace AarxMaps.Routes.Common.Models;

/// <summary>
/// Provides a fluent API for constructing <see cref="Waypoint"/> instances.
/// </summary>
/// <remarks>
/// A waypoint is defined by exactly one of a place ID, a location or an address.
/// <para><b>Travel mode:</b> a heading, <see cref="AsVehicleStopover"/> and <see cref="WithSideOfRoad"/> only
/// apply to <see cref="TravelMode.Drive"/> and <see cref="TravelMode.TwoWheeler"/>. The request builders
/// reject them for other modes.</para>
/// </remarks>
public sealed class WaypointBuilder {
    // -------------------- INIT --------------------
    private const int MaxHeading = 360;

    private string? _placeId;
    private Location? _location;
    private string? _address;
    private bool _via;
    private bool _vehicleStopover;
    private bool _sideOfRoad;

    // -------------------- BUILD --------------------
    /// <summary>
    /// Builds a new <see cref="Waypoint"/> instance.
    /// </summary>
    /// <returns>
    /// A fully configured <see cref="Waypoint"/>.
    /// </returns>
    /// <exception cref="MapsInvalidRequestException">
    /// Thrown when no source or more than one source (place ID, location, address) is set, or when
    /// <see cref="AsVehicleStopover"/> is used on a waypoint that is not defined by a location.
    /// </exception>
    public Waypoint Build() {
        var sources = (_placeId is null ? 0 : 1) + (_location is null ? 0 : 1) + (_address is null ? 0 : 1);
        if (sources == 0)
            throw new MapsInvalidRequestException(
                "A waypoint source is required. Call FromPlaceId(), FromLocation() or FromAddress() before Build().");
        if (sources > 1)
            throw new MapsInvalidRequestException("Only one of PlaceId, Location or Address can be set.");
        if (_vehicleStopover && _location is null)
            throw new MapsInvalidRequestException("VehicleStopover can only be used on a waypoint defined by Location.");

        return new Waypoint() {
            PlaceId = _placeId,
            Location = _location,
            Address = _address,
            Via = _via,
            VehicleStopover = _vehicleStopover,
            SideOfRoad = _sideOfRoad
        };
    }

    // -------------------- CONFIG --------------------
    /// <summary>
    /// Defines the waypoint by a place ID.
    /// </summary>
    /// <param name="placeId">
    /// The place ID, as returned by Places search or autocomplete. Leading and trailing whitespace is removed.
    /// </param>
    /// <returns>
    /// The current <see cref="WaypointBuilder"/> instance.
    /// </returns>
    /// <exception cref="MapsInvalidRequestException">
    /// Thrown when the place ID is null, empty or whitespace.
    /// </exception>
    public WaypointBuilder FromPlaceId(string placeId) {
        var trimmed = placeId?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new MapsInvalidRequestException("Empty PlaceId is not allowed.");

        _placeId = trimmed;
        return this;
    }

    /// <summary>
    /// Defines the waypoint by coordinates.
    /// </summary>
    /// <param name="location">
    /// The coordinates of the waypoint.
    /// </param>
    /// <param name="heading">
    /// Optional compass heading of the direction of travel, from 0 to 360 (0 is north). It tells
    /// Google which side of the road to pick up or drop off at.
    /// </param>
    /// <returns>
    /// The current <see cref="WaypointBuilder"/> instance.
    /// </returns>
    /// <exception cref="MapsInvalidRequestException">
    /// Thrown when the location is null or the heading is outside 0 to 360.
    /// </exception>
    public WaypointBuilder FromLocation(LatLng location, int? heading = null) {
        if (location is null)
            throw new MapsInvalidRequestException("Location cannot be null.");
        if (heading is < 0 or > MaxHeading)
            throw new MapsInvalidRequestException($"Heading must be between 0 and {MaxHeading}.");

        _location = new Location() { LatLng = location, Heading = heading };
        return this;
    }

    /// <summary>
    /// Defines the waypoint by an address or a plus code.
    /// </summary>
    /// <param name="address">
    /// A human-readable address or a plus code. Leading and trailing whitespace is removed.
    /// </param>
    /// <returns>
    /// The current <see cref="WaypointBuilder"/> instance.
    /// </returns>
    /// <remarks>
    /// Google geocodes the address on every request. Prefer a place ID or coordinates when you have one.
    /// </remarks>
    /// <exception cref="MapsInvalidRequestException">
    /// Thrown when the address is null, empty or whitespace.
    /// </exception>
    public WaypointBuilder FromAddress(string address) {
        var trimmed = address?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new MapsInvalidRequestException("Empty Address is not allowed.");

        _address = trimmed;
        return this;
    }

    /// <summary>
    /// Makes the route pass through the waypoint without stopping.
    /// </summary>
    /// <returns>
    /// The current <see cref="WaypointBuilder"/> instance.
    /// </returns>
    /// <remarks>
    /// A via waypoint does not split the route into a new leg. Only valid on intermediate waypoints,
    /// and not together with waypoint order optimization.
    /// </remarks>
    public WaypointBuilder AsVia() {
        _via = true;
        return this;
    }

    /// <summary>
    /// Marks the waypoint as a pickup or drop-off point.
    /// </summary>
    /// <returns>
    /// The current <see cref="WaypointBuilder"/> instance.
    /// </returns>
    /// <remarks>
    /// Google avoids roads that are unsuitable for stopping. Requires a waypoint defined by
    /// <see cref="FromLocation"/>.
    /// </remarks>
    public WaypointBuilder AsVehicleStopover() {
        _vehicleStopover = true;
        return this;
    }

    /// <summary>
    /// Makes the vehicle stop on the side of the road the waypoint is on.
    /// </summary>
    /// <returns>
    /// The current <see cref="WaypointBuilder"/> instance.
    /// </returns>
    public WaypointBuilder WithSideOfRoad() {
        _sideOfRoad = true;
        return this;
    }
}
