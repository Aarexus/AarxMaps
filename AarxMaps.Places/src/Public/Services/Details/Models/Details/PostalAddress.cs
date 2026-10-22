namespace AarxMaps.Places.Services.Details;

/// <summary>
/// Represents a structured postal address information.
/// </summary>
public sealed class PostalAddress {
    /// <summary>
    /// Gets the region code (e.g., country code).
    /// </summary>
    public string? RegionCode { get; internal set; }
    
    /// <summary>
    /// Gets the postal code of the place.
    /// </summary>
    public string? PostalCode { get; internal set; }

    /// <summary>
    /// Gets the administrative area (e.g., state or province).
    /// </summary>
    public string? AdministrativeArea { get; internal set; }

    /// <summary>
    /// Gets the locality (e.g., city).
    /// </summary>
    public string? Locality { get; internal set; }
    
    /// <summary>
    /// Gets a read-only list with the address lines.
    /// </summary>
    public IReadOnlyList<string> AddressLines { get; internal set; } = [];
}