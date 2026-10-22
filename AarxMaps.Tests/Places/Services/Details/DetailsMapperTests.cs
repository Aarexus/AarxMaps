using System.Globalization;
using AarxMaps.Core.Internal.Json;
using AarxMaps.Places.Internal.Services.Details;
using AarxMaps.Places.Internal.Services.Details.DTOs;
using AarxMaps.Places.Services.Details;
using AarxMaps.Tests.Places.Helpers;
using FluentAssertions;

namespace AarxMaps.Tests.Places.Services.Details;

[Trait("Package", "Places")]
[Trait("Module", "Services")]
[Trait("Feature", "Details")]
[Trait("Type", "DetailsMapper")]
public class DetailsMapperTests {
    private const string PlaceId = "ChIJ-museo";

    // Through the real serializer, so the DTOs are also checked against Google's camelCase shape
    private static PlaceDetailsDto Parse(string json) => MapsJsonSerializer.Deserialize<PlaceDetailsDto>(json)!;

    private static PlaceDetails Details(string json) => DetailsMapper.ToPlaceDetails(PlaceId, Parse(json));
    private static PlaceReviews Reviews(string json) => DetailsMapper.ToPlaceReviews(PlaceId, Parse(json));

    private static IReadOnlyList<ReviewInfo> MapReviews(params string[] reviews)
        => Reviews($$"""{ "reviews": [ {{string.Join(",", reviews)}} ] }""").Reviews;

    // ===================== details =====================
    [Fact]
    public void ToPlaceDetails_FullResponse_MapsEveryField() {
        var details = Details(PlacesFixtures.DetailsFull);

        details.PlaceId.Should().Be(PlaceId);
        details.DisplayName.Should().Be("Museo del Oro");
        details.Types.Should().Equal("museum", "tourist_attraction");
        details.FormattedAddress.Should().Be("Cra. 6 #15-88, Bogota, Colombia");
        details.GlobalCode.Should().Be("67P7MV5R+QH");
        details.Location!.Latitude.Should().Be(4.6019);
        details.Location.Longitude.Should().Be(-74.0721);
        details.TimeZone.Should().Be("America/Bogota");
        details.NationalPhoneNumber.Should().Be("(601) 3431212");
        details.InternationalPhoneNumber.Should().Be("+57 601 3431212");
        details.PriceLevel.Should().Be(PriceLevel.Inexpensive);
        details.Rating.Should().Be(4.8f);
        details.UserRatingCount.Should().Be(41234);
        details.OpeningHours.Should().Equal("Monday: Closed", "Tuesday: 9:00 AM - 6:00 PM");
        details.WebsiteUri.Should().Be(new Uri("https://www.banrepcultural.org/museo-del-oro"));
    }

    [Fact]
    public void ToPlaceDetails_FullResponse_MapsTheNestedObjects() {
        var details = Details(PlacesFixtures.DetailsFull);

        details.PostalAddress.Should().BeEquivalentTo(new {
            RegionCode = "CO", PostalCode = "110321", AdministrativeArea = "Bogota",
            Locality = "Bogota", AddressLines = new[] { "Cra. 6 #15-88" }
        });
        details.GoogleMapsLinks.Should().BeEquivalentTo(new {
            GoogleMapsUri = new Uri("https://maps.google.com/?cid=123"),
            DirectionsUri = new Uri("https://www.google.com/maps/dir/?cid=123"),
            PlaceUri = new Uri("https://maps.google.com/?cid=123&place"),
            WriteAReviewUri = new Uri("https://www.google.com/maps/write-review?cid=123"),
            ReviewsUri = new Uri("https://www.google.com/maps/reviews?cid=123"),
            PhotosUri = new Uri("https://www.google.com/maps/photos?cid=123")
        });
        details.PaymentOptions.Should().BeEquivalentTo(new {
            AcceptsCreditCards = true, AcceptsDebitCards = true, AcceptsCashOnly = false, AcceptsNfc = true
        });
        details.ParkingOptions.Should().BeEquivalentTo(new { FreeParkingLot = false, FreeStreetParking = true });
    }

    [Fact]
    public void ToPlaceDetails_EmptyResponse_AbsentArraysAreEmptyAbsentObjectsAreNullAndLinksExist() {
        var details = Details("{}");

        details.PlaceId.Should().Be(PlaceId);
        details.DisplayName.Should().BeNull();
        details.Types.Should().BeEmpty();
        details.OpeningHours.Should().BeEmpty();
        details.GlobalCode.Should().BeNull();
        details.Location.Should().BeNull();
        details.PostalAddress.Should().BeNull();
        details.TimeZone.Should().BeNull();
        details.PriceLevel.Should().BeNull();
        details.Rating.Should().BeNull();
        details.UserRatingCount.Should().BeNull();
        details.WebsiteUri.Should().BeNull();
        details.PaymentOptions.Should().BeNull();
        details.ParkingOptions.Should().BeNull();
        details.GoogleMapsLinks.Should().NotBeNull();
        details.GoogleMapsLinks.Should().BeEquivalentTo(new GoogleMapsLinks());
    }

    [Fact]
    public void ToPlaceDetails_GoogleMapsUriWithoutLinksObject_IsStillExposedOnTheLinks() {
        var links = Details("""{ "googleMapsUri": "https://maps.google.com/?cid=1" }""").GoogleMapsLinks;

        links.GoogleMapsUri.Should().Be(new Uri("https://maps.google.com/?cid=1"));
        links.DirectionsUri.Should().BeNull();
    }

    [Theory]
    [InlineData("PRICE_LEVEL_FREE", PriceLevel.Free)]
    [InlineData("PRICE_LEVEL_INEXPENSIVE", PriceLevel.Inexpensive)]
    [InlineData("PRICE_LEVEL_MODERATE", PriceLevel.Moderate)]
    [InlineData("PRICE_LEVEL_EXPENSIVE", PriceLevel.Expensive)]
    [InlineData("PRICE_LEVEL_VERY_EXPENSIVE", PriceLevel.VeryExpensive)]
    public void ToPlaceDetails_KnownPriceLevel_IsMapped(string wire, PriceLevel expected)
        => Details($$"""{ "priceLevel": "{{wire}}" }""").PriceLevel.Should().Be(expected);

    [Theory]
    [InlineData("PRICE_LEVEL_UNSPECIFIED")]
    [InlineData("PRICE_LEVEL_LUXURY")]
    [InlineData("price_level_free")]
    [InlineData("")]
    public void ToPlaceDetails_UnspecifiedOrUnknownPriceLevel_IsNull(string wire)
        => Details($$"""{ "priceLevel": "{{wire}}" }""").PriceLevel.Should().BeNull();

    [Theory]
    [InlineData("not a uri")]
    [InlineData("/relative/path")]      // Unix: Uri.TryCreate(Absolute) reads it as file:///relative/path
    [InlineData("javascript:alert(1)")] // a website a consumer may render as a link
    [InlineData("")]
    public void ToPlaceDetails_InvalidRelativeOrNonWebUri_IsNullWithoutThrowing(string uri) {
        var details = Details($$"""
            { "websiteUri": "{{uri}}", "googleMapsUri": "{{uri}}", "googleMapsLinks": { "directionsUri": "{{uri}}" } }
            """);

        details.WebsiteUri.Should().BeNull();
        details.GoogleMapsLinks.GoogleMapsUri.Should().BeNull();
        details.GoogleMapsLinks.DirectionsUri.Should().BeNull();
    }

    [Theory]
    [InlineData("http://museo.example/oro", "http://museo.example/oro")]
    [InlineData("HTTPS://Museo.Example/Oro", "https://museo.example/Oro")] // scheme and host are case-insensitive
    public void ToPlaceDetails_HttpOrHttpsUri_IsKept(string wire, string expected)
        => Details($$"""{ "websiteUri": "{{wire}}" }""").WebsiteUri!.AbsoluteUri.Should().Be(expected);

    [Theory]
    [InlineData("""{ "location": { "latitude": 4.6 } }""")]
    [InlineData("""{ "location": { "longitude": -74.0 } }""")]
    [InlineData("""{ "location": {} }""")]
    public void ToPlaceDetails_LocationMissingACoordinate_IsNull(string json)
        => Details(json).Location.Should().BeNull();

    [Fact]
    public void ToPlaceDetails_Collections_AreCopiesOfTheDtoLists() {
        // Cached DTOs are shared between callers, so the public model must not alias their lists
        var dto = Parse(PlacesFixtures.DetailsFull);

        var details = DetailsMapper.ToPlaceDetails(PlaceId, dto);
        dto.Types.Add("extra");
        dto.RegularOpeningHours!.WeekdayDescriptions.Add("extra");
        dto.PostalAddress!.AddressLines.Add("extra");

        details.Types.Should().NotContain("extra");
        details.OpeningHours.Should().NotContain("extra");
        details.PostalAddress!.AddressLines.Should().NotContain("extra");
    }

    // ===================== reviews =====================
    [Fact]
    public void ToPlaceReviews_FullResponse_MapsThePlaceAndBothReviews() {
        var reviews = Reviews(PlacesFixtures.ReviewsFull);

        reviews.PlaceId.Should().Be(PlaceId);
        reviews.DisplayName.Should().Be("Museo del Oro");
        reviews.ReviewsUri.Should().Be(new Uri("https://www.google.com/maps/reviews?cid=123"));
        reviews.Reviews.Should().HaveCount(2);
        reviews.Reviews[0].Should().BeEquivalentTo(new {
            Text = "Amazing collection!",
            LanguageCode = "en",
            Rating = 5f,
            AuthorName = "Ana",
            AuthorUri = new Uri("https://www.google.com/maps/contrib/1"),
            PublishTime = DateTimeOffset.Parse("2025-01-04T20:12:37.9374585+00:00", CultureInfo.InvariantCulture)
        });
    }

    [Fact]
    public void ToPlaceReviews_RatingOnlyReview_IsKeptWithNullText() {
        var review = Reviews(PlacesFixtures.ReviewsFull).Reviews[1];

        review.Text.Should().BeNull();
        review.LanguageCode.Should().BeNull();
        review.Rating.Should().Be(4f);
        review.AuthorName.Should().Be("Luis");
        review.AuthorUri.Should().BeNull();
    }

    [Fact]
    public void ToPlaceReviews_TextOnlyReview_IsKeptWithNullRating() {
        var review = MapReviews("""{ "text": { "text": "Nice" } }""").Should().ContainSingle().Subject;

        review.Text.Should().Be("Nice");
        review.Rating.Should().BeNull();
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{ "text": { "text": "   " } }""")]
    [InlineData("""{ "text": {} }""")]
    [InlineData("""{ "authorAttribution": { "displayName": "Ghost" }, "publishTime": "2024-03-08T17:31:41Z" }""")]
    public void ToPlaceReviews_ReviewWithNeitherTextNorRating_IsSkippedAndTheRestAreKept(string review)
        => MapReviews(review, """{ "rating": 3 }""").Should().ContainSingle().Which.Rating.Should().Be(3f);

    [Fact]
    public void ToPlaceReviews_BlankTextWithRating_IsKeptWithNullText()
        => MapReviews("""{ "rating": 2, "text": { "text": "  " } }""").Single().Text.Should().BeNull();

    [Theory]
    [InlineData("2024-03-08T17:31:41Z", "2024-03-08T17:31:41.0000000+00:00")]
    [InlineData("2024-03-08T17:31:41.718981Z", "2024-03-08T17:31:41.7189810+00:00")]
    [InlineData("2025-01-04T20:12:37.937458542Z", "2025-01-04T20:12:37.9374585+00:00")] // nanoseconds, truncated to ticks
    [InlineData("2024-03-08T17:31:41+05:00", "2024-03-08T17:31:41.0000000+05:00")]
    public void ToPlaceReviews_PublishTime_IsParsedWithItsOffset(string wire, string expected)
        => MapReviews($$"""{ "rating": 5, "publishTime": "{{wire}}" }""").Single().PublishTime
            .Should().BeExactly(DateTimeOffset.Parse(expected, CultureInfo.InvariantCulture)); // BeExactly also compares the offset

    [Theory]
    [InlineData("""{ "rating": 5, "publishTime": "yesterday" }""")]
    [InlineData("""{ "rating": 5, "publishTime": "" }""")]
    [InlineData("""{ "rating": 5 }""")]
    public void ToPlaceReviews_InvalidOrMissingPublishTime_IsNullWithoutFailingTheReview(string review)
        => MapReviews(review).Should().ContainSingle().Which.PublishTime.Should().BeNull();

    [Fact]
    public void ToPlaceReviews_Summary_MapsTextDisclosureAndFlagUri()
        => Reviews(PlacesFixtures.ReviewsFull).ReviewSummary.Should().BeEquivalentTo(new {
            Text = "Visitors praise the gold collection.",
            LanguageCode = "en-US",
            DisclosureText = "Summarized with Gemini",
            FlagContentUri = new Uri("https://www.google.com/local/review/rap/report?postId=summary")
        });

    [Fact]
    public void ToPlaceReviews_SummaryWithoutDisclosureOrValidFlagUri_KeepsTheTextOnly() {
        var summary = Reviews("""{ "reviewSummary": { "text": { "text": "Good" }, "flagContentUri": "nope" } }""").ReviewSummary;

        summary!.Text.Should().Be("Good");
        summary.DisclosureText.Should().BeNull();
        summary.FlagContentUri.Should().BeNull();
    }

    [Theory]
    [InlineData("""{ "reviewSummary": { "text": { "text": "  " } } }""")]
    [InlineData("""{ "reviewSummary": { "text": {} } }""")]
    [InlineData("""{ "reviewSummary": { "disclosureText": { "text": "Summarized with Gemini" } } }""")]
    [InlineData("{}")]
    public void ToPlaceReviews_SummaryWithoutText_IsNull(string json)
        => Reviews(json).ReviewSummary.Should().BeNull();

    [Fact]
    public void ToPlaceReviews_EmptyResponse_HasNoReviewsSummaryOrLinks() {
        var reviews = Reviews("{}");

        reviews.PlaceId.Should().Be(PlaceId);
        reviews.DisplayName.Should().BeNull();
        reviews.Reviews.Should().BeEmpty();
        reviews.ReviewSummary.Should().BeNull();
        reviews.ReviewsUri.Should().BeNull();
    }

    // ===================== photos =====================
    private static PlacePhotoInfo? Photo(string photoJson, string mediaJson = PlacesFixtures.PhotoMedia)
        => DetailsMapper.ToPlacePhotoInfo(
            "places/ChIJ-museo/photos/p1",
            MapsJsonSerializer.Deserialize<PhotoDto>(photoJson)!,
            MapsJsonSerializer.Deserialize<PhotoMediaDto>(mediaJson)!);

    [Fact]
    public void ToPlacePhotoInfo_FullPhoto_MapsEveryFieldWithTheFirstAuthor() {
        var firstPhoto = Parse(PlacesFixtures.PhotosFull).Photos[0];
        var media = MapsJsonSerializer.Deserialize<PhotoMediaDto>(PlacesFixtures.PhotoMedia)!;

        var photo = DetailsMapper.ToPlacePhotoInfo(firstPhoto.Name!, firstPhoto, media);

        photo.Should().BeEquivalentTo(new {
            Uri = new Uri("https://lh3.googleusercontent.com/place-photo"),
            Name = "places/ChIJ-museo/photos/p1",
            AuthorName = "Ana",
            AuthorUri = new Uri("https://www.google.com/maps/contrib/1"),
            HeightPx = 3024,
            WidthPx = 4032
        });
    }

    [Fact]
    public void ToPlacePhotoInfo_NoAuthorsOrSize_LeavesThemNull() {
        var photo = Photo("""{ "name": "places/ChIJ-museo/photos/p1" }""");

        photo!.AuthorName.Should().BeNull();
        photo.AuthorUri.Should().BeNull();
        photo.HeightPx.Should().BeNull();
        photo.WidthPx.Should().BeNull();
    }

    [Theory]
    [InlineData("""{ "name": "places/ChIJ-museo/photos/p1/media" }""")]
    [InlineData("""{ "photoUri": "" }""")]
    [InlineData("""{ "photoUri": "not a uri" }""")]
    [InlineData("""{ "photoUri": "/relative/photo.jpg" }""")]
    [InlineData("""{ "photoUri": "javascript:alert(1)" }""")]
    public void ToPlacePhotoInfo_MissingOrNonWebPhotoUri_ReturnsNull(string mediaJson)
        => Photo("""{ "name": "places/ChIJ-museo/photos/p1" }""", mediaJson).Should().BeNull();
}
