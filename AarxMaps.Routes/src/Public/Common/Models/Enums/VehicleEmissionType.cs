namespace AarxMaps.Routes.Common.Models;

/// <summary>
/// Specifies the emission type of the vehicle, used to apply emission-based road restrictions.
/// </summary>
public enum VehicleEmissionType {
    /// <summary>Gasoline or petrol fueled vehicle.</summary>
    Gasoline,

    /// <summary>Electric vehicle.</summary>
    Electric,

    /// <summary>Hybrid fuel vehicle (such as gasoline and electric).</summary>
    Hybrid,

    /// <summary>Diesel fueled vehicle.</summary>
    Diesel
}
