namespace AarxMaps.Places.Services.Details;

/// <summary>
/// Represents parking availability information for a place.
/// </summary>
public sealed class ParkingOptions {
    /// <summary>
    /// Gets whether free parking lots are available.
    /// </summary>
    public bool? FreeParkingLot { get; internal set; }
    
    /// <summary>
    /// Gets whether free street parking is available.
    /// </summary>
    public bool? FreeStreetParking { get; internal set; }
}