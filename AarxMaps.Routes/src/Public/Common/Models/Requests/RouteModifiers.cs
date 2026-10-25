namespace AarxMaps.Routes.Common.Models;

/// <summary>
/// Represents conditions Google should satisfy when computing a route, such as roads to avoid.
/// </summary>
/// <remarks>
/// Immutable. Every avoid option is a preference, not a guarantee: Google avoids those roads
/// "where reasonable".
/// <para><b>Travel mode:</b> <see cref="AvoidTolls"/>, <see cref="AvoidHighways"/>, <see cref="AvoidFerries"/>
/// and <see cref="EmissionType"/> apply to <see cref="TravelMode.Drive"/> and <see cref="TravelMode.TwoWheeler"/>;
/// <see cref="AvoidIndoor"/> applies to <see cref="TravelMode.Walk"/>. Google ignores them for other modes.</para>
/// </remarks>
public sealed class RouteModifiers {
    /// <summary>
    /// Initializes a new instance of the <see cref="RouteModifiers"/> class.
    /// </summary>
    /// <param name="avoidTolls">Whether to avoid toll roads.</param>
    /// <param name="avoidHighways">Whether to avoid highways.</param>
    /// <param name="avoidFerries">Whether to avoid ferries.</param>
    /// <param name="avoidIndoor">Whether to avoid indoor navigation (walking only).</param>
    /// <param name="emissionType">The emission type of the vehicle, or <c>null</c> to not send one.</param>
    public RouteModifiers(
        bool avoidTolls = false,
        bool avoidHighways = false,
        bool avoidFerries = false,
        bool avoidIndoor = false,
        VehicleEmissionType? emissionType = null
    ) {
        AvoidTolls = avoidTolls;
        AvoidHighways = avoidHighways;
        AvoidFerries = avoidFerries;
        AvoidIndoor = avoidIndoor;
        EmissionType = emissionType;
    }

    /// <summary>
    /// Gets whether toll roads are avoided.
    /// </summary>
    public bool AvoidTolls { get; }

    /// <summary>
    /// Gets whether highways are avoided.
    /// </summary>
    public bool AvoidHighways { get; }

    /// <summary>
    /// Gets whether ferries are avoided.
    /// </summary>
    public bool AvoidFerries { get; }

    /// <summary>
    /// Gets whether indoor navigation is avoided. Walking routes only.
    /// </summary>
    public bool AvoidIndoor { get; }

    /// <summary>
    /// Gets the emission type of the vehicle, or <c>null</c> when not set.
    /// </summary>
    /// <remarks>
    /// Lets Google apply emission-based road restrictions.
    /// </remarks>
    public VehicleEmissionType? EmissionType { get; }
}
