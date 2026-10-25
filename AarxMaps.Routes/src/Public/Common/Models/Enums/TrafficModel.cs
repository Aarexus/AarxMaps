namespace AarxMaps.Routes.Common.Models;

/// <summary>
/// Specifies the assumptions used when predicting travel time in traffic.
/// </summary>
/// <remarks>
/// Only valid with <see cref="TravelMode.Drive"/> and <see cref="RoutingPreference.TrafficAwareOptimal"/>.
/// Google uses <see cref="BestGuess"/> when it is not set.
/// </remarks>
public enum TrafficModel {
    /// <summary>The best estimate from historical traffic and live conditions.</summary>
    BestGuess,

    /// <summary>Longer than the actual travel time on most days.</summary>
    Pessimistic,

    /// <summary>Shorter than the actual travel time on most days.</summary>
    Optimistic
}
