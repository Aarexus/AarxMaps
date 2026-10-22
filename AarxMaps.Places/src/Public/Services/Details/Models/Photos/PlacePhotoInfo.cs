namespace AarxMaps.Places.Services.Details;

/// <summary>
/// Represents a photo associated with a place.
/// </summary>
/// <remarks>
/// Photos can be retrieved using <see cref="IDetailsService.GetPhotosAsync"/>.
/// <para><b>Attribution:</b> Google requires showing the author attribution with the photo.</para>
/// </remarks>
public sealed class PlacePhotoInfo {
    /// <summary>
    /// Gets the URI of the image, sized according to the requested <see cref="PhotoOptions"/>.
    /// </summary>
    public Uri Uri { get; internal set; } = null!;

    /// <summary>
    /// Gets the resource name of the photo.
    /// </summary>
    /// <remarks>
    /// Photo names expire and must not be cached.
    /// </remarks>
    public string Name { get; internal set; } = null!;

    /// <summary>
    /// Gets the name of the photo's author, if available.
    /// </summary>
    public string? AuthorName { get; internal set; }

    /// <summary>
    /// Gets the URI of the author's profile, if available.
    /// </summary>
    public Uri? AuthorUri { get; internal set; }

    /// <summary>
    /// Gets the original height of the photo in pixels.
    /// </summary>
    public int? HeightPx { get; internal set; }

    /// <summary>
    /// Gets the original width of the photo in pixels.
    /// </summary>
    public int? WidthPx { get; internal set; }
}
