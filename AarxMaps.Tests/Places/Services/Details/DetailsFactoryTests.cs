using AarxMaps.Core.Enums;
using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Models;
using AarxMaps.Places.Internal.Services.Details;
using AarxMaps.Places.Services.Details;
using FluentAssertions;

namespace AarxMaps.Tests.Places.Services.Details;

[Trait("Package", "Places")]
[Trait("Module", "Services")]
[Trait("Feature", "Details")]
[Trait("Type", "DetailsFactory")]
public class DetailsFactoryTests {
    private const string DetailsMask =
        "displayName,types,formattedAddress,plusCode,location,postalAddress,timeZone,"
        + "nationalPhoneNumber,internationalPhoneNumber,priceLevel,rating,userRatingCount,"
        + "googleMapsUri,websiteUri,googleMapsLinks,paymentOptions,parkingOptions,regularOpeningHours";
    private const string ReviewsMask = "displayName,reviewSummary,reviews,googleMapsLinks";

    private static readonly DetailsRequest _request = Request();

    private static DetailsRequest Request(string placeId = "ChIJ-museo", string? sessionToken = null) {
        var builder = new DetailsRequestBuilder().WithPlaceId(placeId);
        if (sessionToken is not null) builder.WithSessionToken(sessionToken);
        return builder.Build();
    }

    private static string FieldMask(MapsApiRequest request) => request.Headers!["X-Goog-FieldMask"];

    // Every place-resource request (details, reviews, raw) shares the endpoint and query rules
    public static TheoryData<string> Kinds => new() { "details", "reviews", "raw" };

    private static MapsApiRequest Create(string kind, DetailsRequest request) => kind switch {
        "details" => DetailsFactory.CreateDetailsRequest(request),
        "reviews" => DetailsFactory.CreateReviewsRequest(request),
        "raw" => DetailsFactory.CreateRawDetailsRequest(request, ["displayName"]),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    // ===================== endpoint & query =====================
    [Theory]
    [MemberData(nameof(Kinds))]
    public void Create_Always_GetsThePlaceResourceWithTheKeyInTheHeaderAndNoBody(string kind) {
        var request = Create(kind, _request);

        request.Method.Should().Be(HttpMethod.Get);
        request.BaseUrl.Should().Be(new Uri("https://places.googleapis.com/v1/"));
        request.Endpoint.Should().Be("places/ChIJ-museo");
        request.ApiKeyLocation.Should().Be(ApiKeyLocation.Header);
        request.Payload.Should().BeNull();
        request.Headers.Should().ContainSingle().Which.Key.Should().Be("X-Goog-FieldMask");
    }

    [Theory]
    [InlineData("details", "a/b", "places/a%2Fb")]
    [InlineData("details", "a?b", "places/a%3Fb")]
    [InlineData("details", "a b", "places/a%20b")]
    [InlineData("details", "100%", "places/100%25")]
    [InlineData("details", "ChIJN1t_tDeuEmsRUsoyG83frY4", "places/ChIJN1t_tDeuEmsRUsoyG83frY4")]
    [InlineData("reviews", "a/b", "places/a%2Fb")]
    [InlineData("raw", "a/b", "places/a%2Fb")]
    public void Create_PlaceId_IsEscapedAsOnePathSegment(string kind, string placeId, string expectedEndpoint)
        => Create(kind, Request(placeId)).Endpoint.Should().Be(expectedEndpoint);

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Create_WithoutSessionToken_SendsNoQuery(string kind)
        => Create(kind, _request).Query.Should().BeNull();

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Create_WithSessionToken_SendsItAsTheOnlyQueryParameter(string kind)
        => Create(kind, Request(sessionToken: "tok-1")).Query.Should().BeEquivalentTo(
            new Dictionary<string, string> { ["sessionToken"] = "tok-1" });

    // ===================== details & reviews =====================
    [Fact]
    public void CreateDetailsRequest_Always_UsesTheDetailsMaskAndIsCacheable() {
        var request = DetailsFactory.CreateDetailsRequest(_request);

        FieldMask(request).Should().Be(DetailsMask);
        request.Cacheable.Should().BeTrue();
    }

    [Fact]
    public void CreateReviewsRequest_Always_UsesTheReviewsMaskAndIsCacheable() {
        var request = DetailsFactory.CreateReviewsRequest(_request);

        FieldMask(request).Should().Be(ReviewsMask);
        request.Cacheable.Should().BeTrue();
    }

    // ===================== raw fields =====================
    [Fact]
    public void CreateRawDetailsRequest_Fields_AreTrimmedDeduplicatedAndKeepTheirOrder()
        => FieldMask(DetailsFactory.CreateRawDetailsRequest(_request, [" types ", "displayName", "types", "\tdisplayName", "rating"]))
            .Should().Be("types,displayName,rating");

    [Fact]
    public void CreateRawDetailsRequest_FieldsDifferingOnlyInCase_AreKeptApart()
        => FieldMask(DetailsFactory.CreateRawDetailsRequest(_request, ["rating", "Rating"])).Should().Be("rating,Rating");

    [Fact]
    public void CreateRawDetailsRequest_NullFields_ThrowsMapsInvalidRequestException() {
        var act = () => DetailsFactory.CreateRawDetailsRequest(_request, null!);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Fields cannot be null.");
    }

    [Fact]
    public void CreateRawDetailsRequest_NoFields_ThrowsMapsInvalidRequestException() {
        var act = () => DetailsFactory.CreateRawDetailsRequest(_request, []);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("At least one field is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void CreateRawDetailsRequest_BlankField_ThrowsMapsInvalidRequestException(string? field) {
        var act = () => DetailsFactory.CreateRawDetailsRequest(_request, ["displayName", field!]);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Empty fields are not allowed.");
    }

    [Theory]
    [InlineData(new[] { "photos" }, false)]
    [InlineData(new[] { "photos.name" }, false)]
    [InlineData(new[] { "photos.authorAttributions.displayName" }, false)]
    [InlineData(new[] { "*" }, false)]
    [InlineData(new[] { "displayName", " photos " }, false)]
    [InlineData(new[] { "displayName", "rating" }, true)]
    [InlineData(new[] { "googleMapsLinks.photosUri" }, true)]
    [InlineData(new[] { "photosUri" }, true)]
    public void CreateRawDetailsRequest_Fields_AreCacheableUnlessTheyRequestPhotos(string[] fields, bool cacheable)
        => DetailsFactory.CreateRawDetailsRequest(_request, fields).Cacheable.Should().Be(cacheable);

    // ===================== photos =====================
    [Fact]
    public void CreatePhotosRequest_Always_GetsOnlyThePhotosFieldAndIsNeverCached() {
        // Photo names expire and Google doesn't allow caching them
        var request = DetailsFactory.CreatePhotosRequest(_request);

        request.Method.Should().Be(HttpMethod.Get);
        request.Endpoint.Should().Be("places/ChIJ-museo");
        FieldMask(request).Should().Be("photos");
        request.Cacheable.Should().BeFalse();
        request.Query.Should().BeNull();
    }

    [Fact]
    public void CreatePhotosRequest_WithSessionToken_SendsItAsTheOnlyQueryParameter()
        => DetailsFactory.CreatePhotosRequest(Request(sessionToken: "tok-1")).Query.Should().BeEquivalentTo(
            new Dictionary<string, string> { ["sessionToken"] = "tok-1" });

    [Fact]
    public void CreatePhotoMediaRequest_Always_GetsTheMediaJsonWithTheResolutionAndIsNeverCached() {
        var options = new PhotoOptionsBuilder().WithResolution(1200, 800).Build();

        var request = DetailsFactory.CreatePhotoMediaRequest("places/ChIJ-museo/photos/p1", options);

        request.Method.Should().Be(HttpMethod.Get);
        request.BaseUrl.Should().Be(new Uri("https://places.googleapis.com/v1/"));
        request.Endpoint.Should().Be("places/ChIJ-museo/photos/p1/media", "the photo name is a resource path, not one segment");
        request.Query.Should().BeEquivalentTo(new Dictionary<string, string> {
            ["skipHttpRedirect"] = "true",
            ["maxHeightPx"] = "1200",
            ["maxWidthPx"] = "800"
        });
        request.Cacheable.Should().BeFalse();
        request.ApiKeyLocation.Should().Be(ApiKeyLocation.Header, "the key must never land in a URL that may be logged");
        request.Headers.Should().BeNull();
        request.Payload.Should().BeNull();
    }
}
