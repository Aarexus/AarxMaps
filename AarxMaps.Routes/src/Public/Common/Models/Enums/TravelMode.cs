namespace AarxMaps.Routes.Common.Models;

/// <summary>
/// Represents the mode of transportation of a route, a step or a segment.
/// </summary>
/// <remarks>
/// Leaving the travel mode unset on a request means <see cref="Drive"/>, Google's default.
/// </remarks>
public enum TravelMode {
    /// <summary>A value Google returned that this SDK does not recognize. Not valid in requests.</summary>
    Unknown,

    /// <summary>Travel by passenger car.</summary>
    Drive,

    /// <summary>Travel by bicycle.</summary>
    Bicycle,

    /// <summary>Travel by walking.</summary>
    Walk,

    /// <summary>Two-wheeled, motorized vehicle, such as a motorcycle.</summary>
    TwoWheeler,

    /// <summary>Travel by public transit routes, where available.</summary>
    Transit
}
