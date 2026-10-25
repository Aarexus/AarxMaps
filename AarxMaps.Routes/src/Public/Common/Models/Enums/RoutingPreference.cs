namespace AarxMaps.Routes.Common.Models;

/// <summary>
/// Specifies how traffic is taken into account when computing a route.
/// </summary>
/// <remarks>
/// Only valid when the travel mode is <see cref="TravelMode.Drive"/> or <see cref="TravelMode.TwoWheeler"/>.
/// </remarks>
public enum RoutingPreference {
    /// <summary>Ignores live traffic. Fastest response and lowest price.</summary>
    TrafficUnaware,

    /// <summary>Considers live traffic, with optimizations that favor latency over accuracy.</summary>
    TrafficAware,

    /// <summary>Considers live traffic without most latency optimizations. Slowest response.</summary>
    TrafficAwareOptimal
}
