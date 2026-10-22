namespace AarxMaps.Places.Services.Details;

/// <summary>
/// Configures how many photos <see cref="IDetailsService.GetPhotosAsync"/> retrieves and their size.
/// </summary>
/// <remarks>
/// This model is constructed using <see cref="PhotoOptionsBuilder"/>.
/// </remarks>
public sealed class PhotoOptions {
    internal PhotoOptions() {}

    /// <summary>
    /// Gets the maximum number of photos to retrieve, from 1 to 10.
    /// </summary>
    /// <remarks>
    /// Defaults to 10.
    /// </remarks>
    public int MaxPhotos { get; internal set; }

    /// <summary>
    /// Gets the maximum height of the returned photos in pixels, from 1 to 4800.
    /// </summary>
    /// <remarks>
    /// Defaults to 480.
    /// </remarks>
    public int MaxHeightPx { get; internal set; }

    /// <summary>
    /// Gets the maximum width of the returned photos in pixels, from 1 to 4800.
    /// </summary>
    /// <remarks>
    /// Defaults to 480.
    /// </remarks>
    public int MaxWidthPx { get; internal set; }
}
