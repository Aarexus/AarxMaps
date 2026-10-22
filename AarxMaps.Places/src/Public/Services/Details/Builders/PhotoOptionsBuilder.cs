using AarxMaps.Core.Exceptions;

namespace AarxMaps.Places.Services.Details;

/// <summary>
/// Provides a fluent API for constructing <see cref="PhotoOptions"/> instances.
/// </summary>
/// <remarks>
/// Defaults: 10 photos at up to 480x480 pixels.
/// </remarks>
public sealed class PhotoOptionsBuilder {
    // -------------------- INIT --------------------
    private const int MaxPhotosLimit = 10;
    private const int MaxPixels = 4800;
    private const int DefaultPixels = 480;

    private int _maxPhotos = MaxPhotosLimit;
    private int _maxHeightPx = DefaultPixels;
    private int _maxWidthPx = DefaultPixels;

    // -------------------- BUILD --------------------
    /// <summary>
    /// Builds a new <see cref="PhotoOptions"/> instance.
    /// </summary>
    /// <returns>
    /// A fully configured <see cref="PhotoOptions"/>.
    /// </returns>
    public PhotoOptions Build()
        => new () {
            MaxPhotos = _maxPhotos,
            MaxHeightPx = _maxHeightPx,
            MaxWidthPx = _maxWidthPx
        };

    // -------------------- CONFIG --------------------
    /// <summary>
    /// Sets the maximum number of photos to retrieve.
    /// </summary>
    /// <param name="maxPhotos">
    /// The number of photos, from 1 to 10. Google returns at most 10 photos per place.
    /// </param>
    /// <returns>
    /// The current <see cref="PhotoOptionsBuilder"/> instance.
    /// </returns>
    /// <remarks>
    /// <para><b>Billing:</b> every photo is a separate, billed Place Photos request.</para>
    /// </remarks>
    /// <exception cref="MapsInvalidRequestException">
    /// Thrown when the value is outside the 1 to 10 range.
    /// </exception>
    public PhotoOptionsBuilder WithMaxPhotos(int maxPhotos) {
        if (maxPhotos is <= 0 or > MaxPhotosLimit)
            throw new MapsInvalidRequestException($"MaxPhotos must be between 1 and {MaxPhotosLimit}.");

        _maxPhotos = maxPhotos;
        return this;
    }

    /// <summary>
    /// Sets the maximum size of the returned photos. Google scales images down to fit, keeping the aspect ratio.
    /// </summary>
    /// <param name="maxHeightPx">
    /// Maximum height in pixels, from 1 to 4800.
    /// </param>
    /// <param name="maxWidthPx">
    /// Maximum width in pixels, from 1 to 4800.
    /// </param>
    /// <returns>
    /// The current <see cref="PhotoOptionsBuilder"/> instance.
    /// </returns>
    /// <exception cref="MapsInvalidRequestException">
    /// Thrown when a dimension is outside the 1 to 4800 range.
    /// </exception>
    public PhotoOptionsBuilder WithResolution(int maxHeightPx, int maxWidthPx) {
        if (maxHeightPx is <= 0 or > MaxPixels || maxWidthPx is <= 0 or > MaxPixels)
            throw new MapsInvalidRequestException($"Resolution must be between 1 and {MaxPixels} pixels.");

        _maxHeightPx = maxHeightPx;
        _maxWidthPx = maxWidthPx;
        return this;
    }
}
