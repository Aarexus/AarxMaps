using AarxMaps.Core.Models.Geometry;

namespace AarxMaps.Places.Services.Details;

/// <summary>
/// Represents detailed information about a place.
/// </summary>
/// <remarks>
/// This model aggregates metadata returned by the Google Places Details API,
/// including location, contact information, ratings, and additional attributes.
/// Any value Google doesn't return for the place is <c>null</c> (or empty for collections).
/// </remarks>
public sealed class PlaceDetails {
    /// <summary>
    /// Gets the unique identifier of the place.
    /// </summary>
    public string PlaceId { get; internal set; } = null!;

    /// <summary>
    /// Gets the name of the place.
    /// </summary>
    public string? DisplayName { get; internal set; }

    /// <summary>
    /// Gets the list of place types associated with the place.
    /// </summary>
    public IReadOnlyList<string> Types { get; internal set; } = [];

    /// <summary>
    /// Gets the formatted address of the place.
    /// </summary>
    public string? FormattedAddress { get; internal set; }

    /// <summary>
    /// Gets the global plus code of the place.
    /// </summary>
    public string? GlobalCode { get; internal set; }

    /// <summary>
    /// Gets the geographic location of the place.
    /// </summary>
    public LatLng? Location { get; internal set; }

    /// <summary>
    /// Gets the structured postal address of the place.
    /// </summary>
    public PostalAddress? PostalAddress { get; internal set; }

    /// <summary>
    /// Gets the IANA time zone of the place (e.g., "America/New_York").
    /// </summary>
    public string? TimeZone { get; internal set; }

    /// <summary>
    /// Gets the national phone number of the place.
    /// </summary>
    public string? NationalPhoneNumber { get; internal set; }

    /// <summary>
    /// Gets the international phone number of the place.
    /// </summary>
    public string? InternationalPhoneNumber { get; internal set; }

    /// <summary>
    /// Gets the price level of the place.
    /// </summary>
    public PriceLevel? PriceLevel { get; internal set; }

    /// <summary>
    /// Gets the average rating of the place.
    /// </summary>
    public float? Rating { get; internal set; }

    /// <summary>
    /// Gets the total number of user ratings.
    /// </summary>
    public int? UserRatingCount { get; internal set; }

    /// <summary>
    /// Gets the regular opening hours, one localized description per day of the week.
    /// </summary>
    public IReadOnlyList<string> OpeningHours { get; internal set; } = [];

    /// <summary>
    /// Gets the website of the place.
    /// </summary>
    public Uri? WebsiteUri { get; internal set; }

    /// <summary>
    /// Gets links to the place in Google Maps.
    /// </summary>
    public GoogleMapsLinks GoogleMapsLinks { get; internal set; } = null!;

    /// <summary>
    /// Gets the supported payment options for the place.
    /// </summary>
    public PaymentOptions? PaymentOptions { get; internal set; }

    /// <summary>
    /// Gets parking-related information for the place.
    /// </summary>
    public ParkingOptions? ParkingOptions { get; internal set; }
}
