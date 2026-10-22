namespace AarxMaps.Places.Services.Details;

/// <summary>
/// Represents a collection of Google Maps links related to a place.
/// </summary>
/// <remarks>
/// Each link is <c>null</c> when Google doesn't return it.
/// </remarks>
public sealed class GoogleMapsLinks {
    /// <summary>
    /// Gets the Google Maps URI for the place.
    /// </summary>
    public Uri? GoogleMapsUri { get; internal set; }

    /// <summary>
    /// Gets the URI that opens Google Maps directions to the place.
    /// </summary>
    public Uri? DirectionsUri { get; internal set; }

    /// <summary>
    /// Gets the direct URI to the place page.
    /// </summary>
    public Uri? PlaceUri { get; internal set; }

    /// <summary>
    /// Gets the URI to write a review for the place.
    /// </summary>
    public Uri? WriteAReviewUri { get; internal set; }

    /// <summary>
    /// Gets the URI to view reviews.
    /// </summary>
    public Uri? ReviewsUri { get; internal set; }

    /// <summary>
    /// Gets the URI to view photos.
    /// </summary>
    public Uri? PhotosUri { get; internal set; }
}
