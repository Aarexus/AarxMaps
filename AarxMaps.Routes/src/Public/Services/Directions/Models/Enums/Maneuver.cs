namespace AarxMaps.Routes.Services.Directions;

/// <summary>
/// Represents the action to take at a navigation step, for example to pick the icon to show.
/// </summary>
public enum Maneuver {
    /// <summary>A value Google returned that this SDK does not recognize.</summary>
    Unknown,

    /// <summary>Turn slightly to the left.</summary>
    TurnSlightLeft,

    /// <summary>Turn sharply to the left.</summary>
    TurnSharpLeft,

    /// <summary>Make a left U-turn.</summary>
    UturnLeft,

    /// <summary>Turn left.</summary>
    TurnLeft,

    /// <summary>Turn slightly to the right.</summary>
    TurnSlightRight,

    /// <summary>Turn sharply to the right.</summary>
    TurnSharpRight,

    /// <summary>Make a right U-turn.</summary>
    UturnRight,

    /// <summary>Turn right.</summary>
    TurnRight,

    /// <summary>Go straight.</summary>
    Straight,

    /// <summary>Take the left ramp.</summary>
    RampLeft,

    /// <summary>Take the right ramp.</summary>
    RampRight,

    /// <summary>Merge into traffic.</summary>
    Merge,

    /// <summary>Take the left fork.</summary>
    ForkLeft,

    /// <summary>Take the right fork.</summary>
    ForkRight,

    /// <summary>Take the ferry.</summary>
    Ferry,

    /// <summary>Take the train leading onto the ferry.</summary>
    FerryTrain,

    /// <summary>Turn left at the roundabout.</summary>
    RoundaboutLeft,

    /// <summary>Turn right at the roundabout.</summary>
    RoundaboutRight,

    /// <summary>Initial maneuver.</summary>
    Depart,

    /// <summary>The street name changes.</summary>
    NameChange,

    /// <summary>Arrival at the destination.</summary>
    Destination
}
