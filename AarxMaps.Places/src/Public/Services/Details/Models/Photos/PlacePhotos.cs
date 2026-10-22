namespace AarxMaps.Places.Services.Details;

/// <summary>
/// Represents the photos retrieved for a place.
/// </summary>
public sealed class PlacePhotos {
    /// <summary>
    /// Gets the unique identifier of the place.
    /// </summary>
    public string PlaceId { get; internal set; } = null!;

    /// <summary>
    /// Gets the photos, in the order Google returns them. Empty when the place has no photos.
    /// </summary>
    public IReadOnlyList<PlacePhotoInfo> Photos { get; internal set; } = [];
}
