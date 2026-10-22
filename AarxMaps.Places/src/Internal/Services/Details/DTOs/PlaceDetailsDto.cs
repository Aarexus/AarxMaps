namespace AarxMaps.Places.Internal.Services.Details.DTOs;

// MAIN MODELS
// One Place resource for every Details call; each field mask fills a different subset
internal sealed class PlaceDetailsDto {
    public LocalizedTextDto? DisplayName { get; set; }
    public List<string> Types { get; set; } = [];
    public string? FormattedAddress { get; set; }
    public PlusCodeDto? PlusCode { get; set; }
    public LatLngDto? Location { get; set; }
    public PostalAddressDto? PostalAddress { get; set; }
    public TimeZoneDto? TimeZone { get; set; }
    public string? NationalPhoneNumber { get; set; }
    public string? InternationalPhoneNumber { get; set; }
    public string? PriceLevel { get; set; }
    public float? Rating { get; set; }
    public int? UserRatingCount { get; set; }
    public OpeningHoursDto? RegularOpeningHours { get; set; }
    public string? GoogleMapsUri { get; set; }
    public string? WebsiteUri { get; set; }
    public GoogleMapsLinksDto? GoogleMapsLinks { get; set; }
    public PaymentOptionsDto? PaymentOptions { get; set; }
    public ParkingOptionsDto? ParkingOptions { get; set; }
    public List<ReviewDto> Reviews { get; set; } = [];
    public ReviewSummaryDto? ReviewSummary { get; set; }
    public List<PhotoDto> Photos { get; set; } = [];
}

internal sealed class PhotoMediaDto {
    public string? Name { get; set; }
    public string? PhotoUri { get; set; }
}

// ASIDE MODELS
internal sealed class LocalizedTextDto {
    public string? Text { get; set; }
    public string? LanguageCode { get; set; }
}

internal sealed class PlusCodeDto {
    public string? GlobalCode { get; set; }
}

internal sealed class LatLngDto {
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

internal sealed class PostalAddressDto {
    public string? RegionCode { get; set; }
    public string? PostalCode { get; set; }
    public string? AdministrativeArea { get; set; }
    public string? Locality { get; set; }
    public List<string> AddressLines { get; set; } = [];
}

internal sealed class TimeZoneDto {
    public string? Id { get; set; }
}

internal sealed class OpeningHoursDto {
    public List<string> WeekdayDescriptions { get; set; } = [];
}

internal sealed class GoogleMapsLinksDto {
    public string? DirectionsUri { get; set; }
    public string? PlaceUri { get; set; }
    public string? WriteAReviewUri { get; set; }
    public string? ReviewsUri { get; set; }
    public string? PhotosUri { get; set; }
}

internal sealed class PaymentOptionsDto {
    public bool? AcceptsCreditCards { get; set; }
    public bool? AcceptsDebitCards { get; set; }
    public bool? AcceptsCashOnly { get; set; }
    public bool? AcceptsNfc { get; set; }
}

internal sealed class ParkingOptionsDto {
    public bool? FreeParkingLot { get; set; }
    public bool? FreeStreetParking { get; set; }
}

internal sealed class AuthorAttributionDto {
    public string? DisplayName { get; set; }
    public string? Uri { get; set; }
}

internal sealed class ReviewDto {
    public float? Rating { get; set; }
    public LocalizedTextDto? Text { get; set; }
    // Kept as text: parsed leniently in the mapper so one odd timestamp can't fail the whole response
    public string? PublishTime { get; set; }
    public AuthorAttributionDto? AuthorAttribution { get; set; }
}

internal sealed class ReviewSummaryDto {
    public LocalizedTextDto? Text { get; set; }
    public LocalizedTextDto? DisclosureText { get; set; }
    public string? FlagContentUri { get; set; }
}

internal sealed class PhotoDto {
    public string? Name { get; set; }
    public int? WidthPx { get; set; }
    public int? HeightPx { get; set; }
    public List<AuthorAttributionDto> AuthorAttributions { get; set; } = [];
}
