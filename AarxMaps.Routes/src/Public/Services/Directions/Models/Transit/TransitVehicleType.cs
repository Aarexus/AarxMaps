namespace AarxMaps.Routes.Services.Directions;

/// <summary>
/// Represents the kind of vehicle that serves a transit line.
/// </summary>
public enum TransitVehicleType {
    /// <summary>A value Google returned that this SDK does not recognize.</summary>
    Unknown,

    /// <summary>Bus.</summary>
    Bus,

    /// <summary>A vehicle that operates on a cable, usually on the ground. Aerial cable cars may be <see cref="GondolaLift"/>.</summary>
    CableCar,

    /// <summary>Commuter rail.</summary>
    CommuterTrain,

    /// <summary>Ferry.</summary>
    Ferry,

    /// <summary>A vehicle pulled up a steep incline by a cable.</summary>
    Funicular,

    /// <summary>An aerial cable car.</summary>
    GondolaLift,

    /// <summary>Heavy rail.</summary>
    HeavyRail,

    /// <summary>High speed train.</summary>
    HighSpeedTrain,

    /// <summary>Intercity bus.</summary>
    IntercityBus,

    /// <summary>Long distance train.</summary>
    LongDistanceTrain,

    /// <summary>Light rail transit.</summary>
    MetroRail,

    /// <summary>Monorail.</summary>
    Monorail,

    /// <summary>Any other vehicle.</summary>
    Other,

    /// <summary>Rail.</summary>
    Rail,

    /// <summary>A kind of bus that can be boarded and left anywhere along its route.</summary>
    ShareTaxi,

    /// <summary>Underground light rail.</summary>
    Subway,

    /// <summary>Above ground light rail.</summary>
    Tram,

    /// <summary>Trolleybus.</summary>
    Trolleybus
}
