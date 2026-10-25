namespace AarxMaps.Routes.Common.Models;

/// <summary>
/// Represents additional information the user should be told about a route.
/// </summary>
public sealed class RouteTravelAdvisory {
    /// <summary>
    /// Gets whether Google ignored some of the requested restrictions (such as route modifiers)
    /// because no route satisfied all of them.
    /// </summary>
    public bool RouteRestrictionsPartiallyIgnored { get; internal set; }

    /// <summary>
    /// Gets the total transit fare, or <c>null</c> when it is not available.
    /// </summary>
    /// <remarks>
    /// Transit only, and only when Google knows the fare of every transit step.
    /// </remarks>
    public Money? TransitFare { get; internal set; }
}
