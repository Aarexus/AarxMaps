using AarxMaps.Core.Internal.Json;
using AarxMaps.Core.Models.Geometry;
using AarxMaps.Places.Internal.Services.Searching;
using AarxMaps.Places.Internal.Services.Searching.DTOs;
using AarxMaps.Places.Services.Searching;
using AarxMaps.Tests.Places.Helpers;
using FluentAssertions;

namespace AarxMaps.Tests.Places.Services.Searching;

[Trait("Package", "Places")]
[Trait("Module", "Services")]
[Trait("Feature", "Searching")]
[Trait("Type", "SearchService")]
public class SearchServiceTests {
    private static readonly TextRequest _text = new TextRequestBuilder().WithTextQuery("pizza").Build();
    private static readonly NearbyRequest _nearby =
        new NearbyRequestBuilder().WithLocationRestriction(new LatLng(4.6097, -74.0817), 1500).Build();

    private static SearchResponse Response(string json) => MapsJsonSerializer.Deserialize<SearchResponse>(json)!;

    private static (SearchService Service, ApiClientCapture Capture) Create(string json = PlacesFixtures.SearchTextPage) {
        var capture = new ApiClientCapture().Respond(Response(json));
        return (new SearchService(capture.Client), capture);
    }

    // ===================== text =====================
    [Fact]
    public async Task SearchByTextAsync_NullRequest_ThrowsArgumentNullExceptionWithoutCallingGoogle() {
        var (service, capture) = Create();

        var act = () => service.SearchByTextAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("textRequest");
        capture.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchByTextAsync_ValidRequest_SendsOneSearchTextRequestCarryingThePayload() {
        var (service, capture) = Create();

        await service.SearchByTextAsync(_text);

        var sent = capture.Calls.Should().ContainSingle().Subject.Request;
        sent.Method.Should().Be(HttpMethod.Post);
        sent.Endpoint.Should().Be("places:searchText");
        sent.Payload.Should().BeSameAs(_text);
    }

    [Fact]
    public async Task SearchByTextAsync_CallerToken_ReachesTheApiClient() {
        using var cts = new CancellationTokenSource();
        var (service, capture) = Create();

        await service.SearchByTextAsync(_text, cts.Token);

        capture.Calls.Single().Ct.Should().Be(cts.Token);
    }

    [Fact]
    public async Task SearchByTextAsync_ApiResponse_IsMappedWithTheNextPageToken() {
        var (service, _) = Create();

        var result = await service.SearchByTextAsync(_text);

        result.Places.Select(p => p.PlaceId).Should().Equal("ChIJ-pizza-1", "ChIJ-pizza-2");
        result.NextPageToken.Should().Be("page-2-token");
    }

    [Theory]
    [MemberData(nameof(CoreErrors.Names), MemberType = typeof(CoreErrors))]
    public async Task SearchByTextAsync_CoreException_PropagatesUnchanged(string error) {
        var expected = CoreErrors.Create(error);
        var service = new SearchService(new ApiClientCapture().Fail<SearchResponse>(expected).Client);

        var act = () => service.SearchByTextAsync(_text);

        (await act.Should().ThrowAsync<Exception>()).Which.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task SearchByTextAsync_CancelledToken_ThrowsOperationCanceledException() {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var capture = new ApiClientCapture().Handle((_, ct) => Task.FromCanceled<SearchResponse>(ct));
        var service = new SearchService(capture.Client);

        var act = () => service.SearchByTextAsync(_text, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    // ===================== nearby =====================
    [Fact]
    public async Task SearchNearbyAsync_NullRequest_ThrowsArgumentNullExceptionWithoutCallingGoogle() {
        var (service, capture) = Create(PlacesFixtures.SearchNearbyPage);

        var act = () => service.SearchNearbyAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("nearbyRequest");
        capture.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchNearbyAsync_ValidRequest_SendsOneSearchNearbyRequestCarryingThePayload() {
        var (service, capture) = Create(PlacesFixtures.SearchNearbyPage);

        await service.SearchNearbyAsync(_nearby);

        var sent = capture.Calls.Should().ContainSingle().Subject.Request;
        sent.Method.Should().Be(HttpMethod.Post);
        sent.Endpoint.Should().Be("places:searchNearby");
        sent.Payload.Should().BeSameAs(_nearby);
    }

    [Fact]
    public async Task SearchNearbyAsync_CallerToken_ReachesTheApiClient() {
        using var cts = new CancellationTokenSource();
        var (service, capture) = Create(PlacesFixtures.SearchNearbyPage);

        await service.SearchNearbyAsync(_nearby, cts.Token);

        capture.Calls.Single().Ct.Should().Be(cts.Token);
    }

    [Fact]
    public async Task SearchNearbyAsync_ApiResponse_IsMapped() {
        var (service, _) = Create(PlacesFixtures.SearchNearbyPage);

        var result = await service.SearchNearbyAsync(_nearby);

        result.Places.Should().ContainSingle().Which.PlaceId.Should().Be("ChIJ-cafe-1");
    }

    [Theory]
    [MemberData(nameof(CoreErrors.Names), MemberType = typeof(CoreErrors))]
    public async Task SearchNearbyAsync_CoreException_PropagatesUnchanged(string error) {
        var expected = CoreErrors.Create(error);
        var service = new SearchService(new ApiClientCapture().Fail<SearchResponse>(expected).Client);

        var act = () => service.SearchNearbyAsync(_nearby);

        (await act.Should().ThrowAsync<Exception>()).Which.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task SearchNearbyAsync_CancelledToken_ThrowsOperationCanceledException() {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var capture = new ApiClientCapture().Handle((_, ct) => Task.FromCanceled<SearchResponse>(ct));
        var service = new SearchService(capture.Client);

        var act = () => service.SearchNearbyAsync(_nearby, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
