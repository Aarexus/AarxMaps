using System.Net;
using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Extensions;
using AarxMaps.Places.Services.Details;
using AarxMaps.Tests.Core.Helpers;
using AarxMaps.Tests.Places.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AarxMaps.Tests.Places.Services.Details;

/// <summary>
/// Real MapsApiClient over MockHttp. Rules match loosely and URLs are asserted from the recording,
/// so escaping or a leaked API key can't be hidden by the matcher.
/// </summary>
[Trait("Package", "Places")]
[Trait("Module", "Services")]
[Trait("Feature", "Details")]
[Trait("Type", "DetailsWire")]
public class DetailsWireTests {
    private const string PlaceUrl = "https://places.googleapis.com/v1/places/ChIJ-museo";

    private static DetailsRequest Request(string placeId = "ChIJ-museo", string? sessionToken = null) {
        var builder = new DetailsRequestBuilder().WithPlaceId(placeId);
        if (sessionToken is not null) builder.WithSessionToken(sessionToken);
        return builder.Build();
    }

    private static (ServiceProvider Provider, RecordingHttp Http) Build(
        string json = PlacesFixtures.DetailsFull, bool memoryCache = false, HttpStatusCode status = HttpStatusCode.OK
    ) {
        var http = new RecordingHttp().Respond(HttpMethod.Get, "*", json, status);
        var provider = PlacesTestServices.BuildWire(http.Mock, memoryCache
            ? s => s.AddAarxMapsMemoryCache(TestServices.ValidCaching)
            : null);
        return (provider, http);
    }

    // ===================== transport =====================
    [Fact]
    public async Task Details_Always_GetsThePlaceUrlWithHeadersOnlyAndNoBody() {
        var (provider, http) = Build();
        using var _ = provider;

        await provider.GetRequiredService<IDetailsService>().GetDetailsAsync(Request());

        var sent = http.Requests.Should().ContainSingle().Subject;
        sent.Method.Should().Be(HttpMethod.Get);
        sent.Uri.AbsoluteUri.Should().Be(PlaceUrl);
        sent.Uri.Query.Should().BeEmpty("the API key must never land in the URL");
        sent.Headers.Should().Contain("X-Goog-Api-Key", TestRequests.DefaultApiKey);
        sent.Headers.Should().ContainKey("X-Goog-FieldMask").WhoseValue.Should().StartWith("displayName,types,");
        sent.Body.Should().BeNull();
    }

    [Fact]
    public async Task Details_PlaceIdWithReservedCharacters_IsSentEscapedExactlyOnce() {
        var (provider, http) = Build();
        using var _ = provider;

        await provider.GetRequiredService<IDetailsService>().GetDetailsAsync(Request("a/b?c d%"));

        http.Requests.Single().Uri.AbsoluteUri.Should().Be("https://places.googleapis.com/v1/places/a%2Fb%3Fc%20d%25");
    }

    [Fact]
    public async Task Details_WithSessionToken_SendsItAsTheOnlyQueryParameter() {
        var (provider, http) = Build();
        using var _ = provider;

        await provider.GetRequiredService<IDetailsService>().GetDetailsAsync(Request(sessionToken: "tok-1"));

        http.Requests.Single().Uri.Query.Should().Be("?sessionToken=tok-1");
    }

    [Fact]
    public async Task Reviews_Always_GetsThePlaceUrlWithTheReviewsMask() {
        var (provider, http) = Build(PlacesFixtures.ReviewsFull);
        using var _ = provider;

        await provider.GetRequiredService<IDetailsService>().GetReviewsAsync(Request());

        var sent = http.Requests.Should().ContainSingle().Subject;
        sent.Uri.AbsoluteUri.Should().Be(PlaceUrl);
        sent.Headers.Should().Contain("X-Goog-Api-Key", TestRequests.DefaultApiKey);
        sent.Headers.Should().Contain("X-Goog-FieldMask", "displayName,reviewSummary,reviews,googleMapsLinks");
    }

    [Fact]
    public async Task Raw_Always_SendsTheNormalizedFieldMask() {
        var (provider, http) = Build();
        using var _ = provider;

        using var document = await provider.GetRequiredService<IDetailsService>()
            .GetDetailsRawAsync(Request(), [" displayName ", "rating", "displayName"]);

        var sent = http.Requests.Should().ContainSingle().Subject;
        sent.Uri.AbsoluteUri.Should().Be(PlaceUrl);
        sent.Headers.Should().Contain("X-Goog-FieldMask", "displayName,rating");
    }

    // ===================== response =====================
    [Fact]
    public async Task Details_GoogleFixture_IsMappedToPlaceDetails() {
        var (provider, _) = Build();
        using var _p = provider;

        var details = await provider.GetRequiredService<IDetailsService>().GetDetailsAsync(Request());

        details.PlaceId.Should().Be("ChIJ-museo");
        details.DisplayName.Should().Be("Museo del Oro");
        details.TimeZone.Should().Be("America/Bogota");
        details.PriceLevel.Should().Be(PriceLevel.Inexpensive);
        details.GoogleMapsLinks.ReviewsUri.Should().Be(new Uri("https://www.google.com/maps/reviews?cid=123"));
    }

    [Fact]
    public async Task Reviews_GoogleFixture_IsMappedToPlaceReviews() {
        var (provider, _) = Build(PlacesFixtures.ReviewsFull);
        using var _p = provider;

        var reviews = await provider.GetRequiredService<IDetailsService>().GetReviewsAsync(Request());

        reviews.Reviews.Select(r => r.AuthorName).Should().Equal("Ana", "Luis");
        reviews.ReviewSummary!.DisclosureText.Should().Be("Summarized with Gemini");
    }

    [Fact]
    public async Task Raw_GoogleResponse_IsReturnedAsAJsonDocument() {
        var (provider, _) = Build();
        using var _p = provider;

        using var document = await provider.GetRequiredService<IDetailsService>()
            .GetDetailsRawAsync(Request(), ["displayName", "timeZone"]);

        document.RootElement.GetProperty("displayName").GetProperty("text").GetString().Should().Be("Museo del Oro");
        document.RootElement.GetProperty("timeZone").GetProperty("id").GetString().Should().Be("America/Bogota");
    }

    [Fact]
    public async Task Details_NotFound_ThrowsMapsNotFoundException() {
        var (provider, _) = Build(
            """{ "error": { "code": 404, "message": "Place not found.", "status": "NOT_FOUND" } }""",
            status: HttpStatusCode.NotFound);
        using var _p = provider;

        var act = () => provider.GetRequiredService<IDetailsService>().GetDetailsAsync(Request());

        (await act.Should().ThrowAsync<MapsNotFoundException>()).Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ===================== caching =====================
    [Fact]
    public async Task Details_MemoryCache_SecondIdenticalCallIsServedFromTheCache() {
        var (provider, http) = Build(memoryCache: true);
        using var _ = provider;
        var service = provider.GetRequiredService<IDetailsService>();

        await service.GetDetailsAsync(Request());
        var second = await service.GetDetailsAsync(Request());

        http.Requests.Should().HaveCount(1);
        second.DisplayName.Should().Be("Museo del Oro");
    }

    [Fact]
    public async Task Reviews_MemoryCache_SecondIdenticalCallIsServedFromTheCache() {
        var (provider, http) = Build(PlacesFixtures.ReviewsFull, memoryCache: true);
        using var _ = provider;
        var service = provider.GetRequiredService<IDetailsService>();

        await service.GetReviewsAsync(Request());
        await service.GetReviewsAsync(Request());

        http.Requests.Should().HaveCount(1);
    }

    [Fact]
    public async Task Details_MemoryCache_DifferentSessionTokensAreCachedSeparately() {
        var (provider, http) = Build(memoryCache: true);
        using var _ = provider;
        var service = provider.GetRequiredService<IDetailsService>();

        await service.GetDetailsAsync(Request(sessionToken: "tok-1"));
        await service.GetDetailsAsync(Request(sessionToken: "tok-2"));

        http.Requests.Should().HaveCount(2);
    }

    [Theory]
    [InlineData(new[] { "displayName" }, 1)]
    [InlineData(new[] { "displayName", "photos" }, 2)]
    [InlineData(new[] { "*" }, 2)]
    public async Task Raw_MemoryCache_IsCachedUnlessTheFieldsRequestPhotos(string[] fields, int expectedHits) {
        var (provider, http) = Build(memoryCache: true);
        using var _ = provider;
        var service = provider.GetRequiredService<IDetailsService>();

        using (await service.GetDetailsRawAsync(Request(), fields)) { }
        using var second = await service.GetDetailsRawAsync(Request(), fields);

        http.Requests.Should().HaveCount(expectedHits);
        second.RootElement.GetProperty("displayName").GetProperty("text").GetString().Should().Be("Museo del Oro");
    }

    // ===================== photos =====================
    private const string MediaUrlPattern = "https://places.googleapis.com/v1/places/ChIJ-museo/photos/*";

    private static (ServiceProvider Provider, RecordingHttp Http) BuildPhotos(
        bool memoryCache = false, HttpStatusCode mediaStatus = HttpStatusCode.OK, string mediaJson = PlacesFixtures.PhotoMedia
    ) {
        // First match wins: media calls hit the photos rule, the names call falls through to "*"
        var http = new RecordingHttp()
            .Respond(HttpMethod.Get, MediaUrlPattern, mediaJson, mediaStatus)
            .Respond(HttpMethod.Get, "*", PlacesFixtures.PhotosFull);
        var provider = PlacesTestServices.BuildWire(http.Mock, memoryCache
            ? s => s.AddAarxMapsMemoryCache(TestServices.ValidCaching)
            : null);
        return (provider, http);
    }

    private static List<RecordedRequest> MediaRequests(RecordingHttp http)
        => http.Requests.Where(r => r.Uri.AbsolutePath.EndsWith("/media", StringComparison.Ordinal)).ToList();

    [Fact]
    public async Task Photos_EveryRequest_SendsTheApiKeyInTheHeaderAndNeverInTheUrl() {
        var (provider, http) = BuildPhotos();
        using var _ = provider;

        await provider.GetRequiredService<IDetailsService>().GetPhotosAsync(Request());

        http.Requests.Should().HaveCount(3, "one names call plus one media call per photo");
        http.Requests.Should().AllSatisfy(r => {
            r.Headers.Should().Contain("X-Goog-Api-Key", TestRequests.DefaultApiKey);
            r.Uri.AbsoluteUri.Should().NotContain(TestRequests.DefaultApiKey);
            TestRequests.QueryPairs(r.Uri).Should().NotContain(pair => pair.StartsWith("key=", StringComparison.Ordinal));
        });
    }

    [Fact]
    public async Task Photos_NamesRequest_AsksForPhotosOnlyAndCarriesTheSessionToken() {
        var (provider, http) = BuildPhotos();
        using var _ = provider;

        await provider.GetRequiredService<IDetailsService>().GetPhotosAsync(Request(sessionToken: "tok-1"));

        var names = http.Requests.Should().ContainSingle(r => r.Uri.AbsolutePath == "/v1/places/ChIJ-museo").Subject;
        names.Uri.AbsoluteUri.Should().Be($"{PlaceUrl}?sessionToken=tok-1");
        names.Headers.Should().Contain("X-Goog-FieldMask", "photos");
        MediaRequests(http).Should().AllSatisfy(r => r.Uri.Query.Should().NotContain("sessionToken"));
    }

    [Fact]
    public async Task Photos_MediaRequests_AskForJsonAtTheConfiguredResolution() {
        var (provider, http) = BuildPhotos();
        using var _ = provider;
        var options = new PhotoOptionsBuilder().WithResolution(1200, 800).Build();

        await provider.GetRequiredService<IDetailsService>().GetPhotosAsync(Request(), options);

        // Media calls run in parallel, so their arrival order isn't fixed
        MediaRequests(http).Select(r => r.Uri.AbsoluteUri).Should().BeEquivalentTo(
            "https://places.googleapis.com/v1/places/ChIJ-museo/photos/p1/media?skipHttpRedirect=true&maxHeightPx=1200&maxWidthPx=800",
            "https://places.googleapis.com/v1/places/ChIJ-museo/photos/p2/media?skipHttpRedirect=true&maxHeightPx=1200&maxWidthPx=800");
        MediaRequests(http).Should().AllSatisfy(r => r.Headers.Should().NotContainKey("X-Goog-FieldMask"));
    }

    [Fact]
    public async Task Photos_GoogleFixtures_AreMappedInGoogleOrder() {
        var (provider, _) = BuildPhotos();
        using var _p = provider;

        var result = await provider.GetRequiredService<IDetailsService>().GetPhotosAsync(Request());

        result.PlaceId.Should().Be("ChIJ-museo");
        result.Photos.Should().BeEquivalentTo(new[] {
            new {
                Uri = new Uri("https://lh3.googleusercontent.com/place-photo"), Name = "places/ChIJ-museo/photos/p1",
                AuthorName = (string?)"Ana", AuthorUri = (Uri?)new Uri("https://www.google.com/maps/contrib/1"),
                HeightPx = 3024, WidthPx = 4032
            },
            new {
                Uri = new Uri("https://lh3.googleusercontent.com/place-photo"), Name = "places/ChIJ-museo/photos/p2",
                AuthorName = (string?)null, AuthorUri = (Uri?)null,
                HeightPx = 600, WidthPx = 800
            }
        }, o => o.WithStrictOrdering());
    }

    [Fact]
    public async Task Photos_MemoryCache_EveryCallStillReachesGoogle() {
        var (provider, http) = BuildPhotos(memoryCache: true);
        using var _ = provider;
        var service = provider.GetRequiredService<IDetailsService>();

        await service.GetPhotosAsync(Request());
        await service.GetPhotosAsync(Request());

        http.Requests.Should().HaveCount(6, "neither the names call nor the media calls may be cached");
    }

    [Fact]
    public async Task Photos_ExpiredPhoto_ThrowsMapsNotFoundException() {
        var (provider, _) = BuildPhotos(
            mediaStatus: HttpStatusCode.NotFound,
            mediaJson: """{ "error": { "code": 404, "message": "Photo not found.", "status": "NOT_FOUND" } }""");
        using var _p = provider;

        var act = () => provider.GetRequiredService<IDetailsService>().GetPhotosAsync(Request());

        (await act.Should().ThrowAsync<MapsNotFoundException>()).Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
