using AarxMaps.Routes.Common.Models;

namespace AarxMaps.Routes.Services.DistanceMatrix;

/// <summary>
/// Represents a request for the distance and duration of the route between every origin and every destination.
/// </summary>
/// <remarks>
/// This model is constructed using <see cref="DistanceRequestBuilder"/>, which checks Google's size limits
/// and the combinations Google rejects or silently ignores. <c>null</c> values are not sent, so Google's
/// defaults apply.
/// </remarks>
public sealed class DistanceRequest {
    internal DistanceRequest() {}

    /// <summary>
    /// Gets the origins, one per row of the matrix.
    /// </summary>
    public IReadOnlyList<RouteMatrixOrigin> Origins { get; internal set; } = [];

    /// <summary>
    /// Gets the destinations, one per column of the matrix.
    /// </summary>
    public IReadOnlyList<Waypoint> Destinations { get; internal set; } = [];

    /// <summary>
    /// Gets the mode of transportation, or <c>null</c> for Google's default (<see cref="TravelMode.Drive"/>).
    /// </summary>
    public TravelMode? TravelMode { get; internal set; }

    /// <summary>
    /// Gets how traffic is taken into account, or <c>null</c> for Google's default
    /// (<see cref="RoutingPreference.TrafficUnaware"/>).
    /// </summary>
    public RoutingPreference? RoutingPreference { get; internal set; }

    /// <summary>
    /// Gets the departure time, or <c>null</c> for "now".
    /// </summary>
    public DateTimeOffset? DepartureTime { get; internal set; }

    /// <summary>
    /// Gets the desired arrival time. Transit only.
    /// </summary>
    public DateTimeOffset? ArrivalTime { get; internal set; }

    /// <summary>
    /// Gets the units of measure for display text, or <c>null</c> to let Google infer them from the first origin.
    /// </summary>
    public Units? Units { get; internal set; }

    /// <summary>
    /// Gets the assumptions used for travel time in traffic, or <c>null</c> for Google's default
    /// (<see cref="TrafficModel.BestGuess"/>).
    /// </summary>
    public TrafficModel? TrafficModel { get; internal set; }

    /// <summary>
    /// Gets the preferences for transit routes.
    /// </summary>
    public TransitPreferences? TransitPreferences { get; internal set; }
}
