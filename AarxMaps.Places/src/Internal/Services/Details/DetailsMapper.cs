using System.Globalization;
using AarxMaps.Core.Models.Geometry;
using AarxMaps.Places.Internal.Services.Details.DTOs;
using AarxMaps.Places.Services.Details;

namespace AarxMaps.Places.Internal.Services.Details;

internal static class DetailsMapper {
    // -------------------- METHS --------------------
    internal static PlaceDetails ToPlaceDetails(string placeId, PlaceDetailsDto dto)
        => new () {
            PlaceId = placeId,
            DisplayName = dto.DisplayName?.Text,
            Types = [.. dto.Types],
            FormattedAddress = dto.FormattedAddress,
            GlobalCode = dto.PlusCode?.GlobalCode,
            Location = ToLatLng(dto.Location),
            PostalAddress = ToPostalAddress(dto.PostalAddress),
            TimeZone = dto.TimeZone?.Id,
            NationalPhoneNumber = dto.NationalPhoneNumber,
            InternationalPhoneNumber = dto.InternationalPhoneNumber,
            PriceLevel = ToPriceLevel(dto.PriceLevel),
            Rating = dto.Rating,
            UserRatingCount = dto.UserRatingCount,
            OpeningHours = [.. dto.RegularOpeningHours?.WeekdayDescriptions ?? []],
            WebsiteUri = ToUri(dto.WebsiteUri),
            GoogleMapsLinks = ToGoogleMapsLinks(dto.GoogleMapsUri, dto.GoogleMapsLinks),
            PaymentOptions = ToPaymentOptions(dto.PaymentOptions),
            ParkingOptions = ToParkingOptions(dto.ParkingOptions)
        };

    internal static PlaceReviews ToPlaceReviews(string placeId, PlaceDetailsDto dto)
        => new () {
            PlaceId = placeId,
            DisplayName = dto.DisplayName?.Text,
            Reviews = ToReviews(dto.Reviews),
            ReviewSummary = ToReviewSummary(dto.ReviewSummary),
            ReviewsUri = ToUri(dto.GoogleMapsLinks?.ReviewsUri)
        };

    internal static PlacePhotoInfo? ToPlacePhotoInfo(string photoName, PhotoDto photo, PhotoMediaDto media) {
        var uri = ToUri(media.PhotoUri);
        if (uri is null) return null;

        var author = photo.AuthorAttributions.Count > 0 ? photo.AuthorAttributions[0] : null;
        return new PlacePhotoInfo() {
            Uri = uri,
            Name = photoName,
            AuthorName = author?.DisplayName,
            AuthorUri = ToUri(author?.Uri),
            HeightPx = photo.HeightPx,
            WidthPx = photo.WidthPx
        };
    }

    // -------------------- INNER METHS --------------------
    private static LatLng? ToLatLng(LatLngDto? dto)
        => dto?.Latitude is double latitude && dto.Longitude is double longitude
            ? new LatLng(latitude, longitude) : null;

    private static PostalAddress? ToPostalAddress(PostalAddressDto? dto)
        => dto is null ? null
            : new PostalAddress() {
                RegionCode = dto.RegionCode,
                PostalCode = dto.PostalCode,
                AdministrativeArea = dto.AdministrativeArea,
                Locality = dto.Locality,
                AddressLines = [.. dto.AddressLines]
            };

    private static PriceLevel? ToPriceLevel(string? priceLevel)
        => priceLevel switch {
            "PRICE_LEVEL_FREE" => PriceLevel.Free,
            "PRICE_LEVEL_INEXPENSIVE" => PriceLevel.Inexpensive,
            "PRICE_LEVEL_MODERATE" => PriceLevel.Moderate,
            "PRICE_LEVEL_EXPENSIVE" => PriceLevel.Expensive,
            "PRICE_LEVEL_VERY_EXPENSIVE" => PriceLevel.VeryExpensive,
            _ => null
        };

    private static GoogleMapsLinks ToGoogleMapsLinks(string? googleMapsUri, GoogleMapsLinksDto? dto)
        => new () {
            GoogleMapsUri = ToUri(googleMapsUri),
            DirectionsUri = ToUri(dto?.DirectionsUri),
            PlaceUri = ToUri(dto?.PlaceUri),
            WriteAReviewUri = ToUri(dto?.WriteAReviewUri),
            ReviewsUri = ToUri(dto?.ReviewsUri),
            PhotosUri = ToUri(dto?.PhotosUri)
        };

    private static PaymentOptions? ToPaymentOptions(PaymentOptionsDto? dto)
        => dto is null ? null
            : new PaymentOptions() {
                AcceptsCreditCards = dto.AcceptsCreditCards,
                AcceptsDebitCards = dto.AcceptsDebitCards,
                AcceptsCashOnly = dto.AcceptsCashOnly,
                AcceptsNfc = dto.AcceptsNfc
            };

    private static ParkingOptions? ToParkingOptions(ParkingOptionsDto? dto)
        => dto is null ? null
            : new ParkingOptions() {
                FreeParkingLot = dto.FreeParkingLot,
                FreeStreetParking = dto.FreeStreetParking
            };

    private static List<ReviewInfo> ToReviews(List<ReviewDto> dtos) {
        var reviews = new List<ReviewInfo>(dtos.Count);

        foreach (var review in dtos) {
            var text = string.IsNullOrWhiteSpace(review.Text?.Text) ? null : review.Text.Text;
            // Entries with neither text nor rating carry nothing to show
            if (text is null && review.Rating is null)
                continue;

            reviews.Add(new ReviewInfo() {
                Text = text,
                LanguageCode = review.Text?.LanguageCode,
                Rating = review.Rating,
                AuthorName = review.AuthorAttribution?.DisplayName,
                AuthorUri = ToUri(review.AuthorAttribution?.Uri),
                PublishTime = DateTimeOffset.TryParse(
                    review.PublishTime, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var publishTime
                ) ? publishTime : null
            });
        }

        return reviews;
    }

    private static ReviewSummary? ToReviewSummary(ReviewSummaryDto? dto) {
        var text = dto?.Text?.Text;
        if (dto is null || string.IsNullOrWhiteSpace(text))
            return null;

        return new ReviewSummary() {
            Text = text,
            LanguageCode = dto.Text?.LanguageCode,
            DisclosureText = dto.DisclosureText?.Text,
            FlagContentUri = ToUri(dto.FlagContentUri)
        };
    }

    private static Uri? ToUri(string? value)
        => Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri : null;
}
