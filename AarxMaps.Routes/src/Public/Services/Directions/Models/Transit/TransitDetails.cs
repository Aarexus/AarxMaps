namespace AarxMaps.Routes.Services.Directions;

/// <summary>
/// Represents the transit part of a step: the line taken, where to board and leave, and when.
/// </summary>
public sealed class TransitDetails {
    /// <summary>
    /// Gets the stop where the vehicle is boarded.
    /// </summary>
    public TransitStop? DepartureStop { get; internal set; }

    /// <summary>
    /// Gets the stop where the vehicle is left.
    /// </summary>
    public TransitStop? ArrivalStop { get; internal set; }

    /// <summary>
    /// Gets the estimated departure time, in UTC.
    /// </summary>
    public DateTimeOffset? DepartureTime { get; internal set; }

    /// <summary>
    /// Gets the estimated arrival time, in UTC.
    /// </summary>
    public DateTimeOffset? ArrivalTime { get; internal set; }

    /// <summary>
    /// Gets the departure time formatted in the local time of the departure stop (e.g., "10:30 AM").
    /// </summary>
    public string? DepartureTimeText { get; internal set; }

    /// <summary>
    /// Gets the arrival time formatted in the local time of the arrival stop.
    /// </summary>
    public string? ArrivalTimeText { get; internal set; }

    /// <summary>
    /// Gets the direction to travel on the line, as shown on the vehicle or at the stop (often the terminus).
    /// </summary>
    public string? Headsign { get; internal set; }

    /// <summary>
    /// Gets the number of stops from the departure stop to the arrival stop, counting the arrival stop.
    /// </summary>
    public int StopCount { get; internal set; }

    /// <summary>
    /// Gets the transit line used in the step.
    /// </summary>
    public TransitLine? Line { get; internal set; }
}
