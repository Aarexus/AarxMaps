using System.Net;
using System.Text.Json;
using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Internal.Json;
using AarxMaps.Core.Models;
using AarxMaps.Places.Internal.Services.Details;
using AarxMaps.Places.Internal.Services.Details.DTOs;
using AarxMaps.Places.Services.Details;
using AarxMaps.Tests.Core.Helpers;
using AarxMaps.Tests.Places.Helpers;
using FluentAssertions;

namespace AarxMaps.Tests.Places.Services.Details;

[Trait("Package", "Places")]
[Trait("Module", "Services")]
[Trait("Feature", "Details")]
[Trait("Type", "DetailsService")]
public class DetailsServiceTests {
    private static readonly DetailsRequest _request = new DetailsRequestBuilder().WithPlaceId("ChIJ-museo").Build();

    private static PlaceDetailsDto Dto(string json) => MapsJsonSerializer.Deserialize<PlaceDetailsDto>(json)!;

    private static (DetailsService Service, ApiClientCapture Capture) Create(string json = PlacesFixtures.DetailsFull) {
        var capture = new ApiClientCapture().Respond(Dto(json));
        return (new DetailsService(capture.Client), capture);
    }

    // ===================== details =====================
    [Fact]
    public async Task GetDetailsAsync_NullRequest_ThrowsArgumentNullExceptionWithoutCallingGoogle() {
        var (service, capture) = Create();

        var act = () => service.GetDetailsAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("detRequest");
        capture.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDetailsAsync_ValidRequest_SendsOneCacheableDetailsRequest() {
        var (service, capture) = Create();

        await service.GetDetailsAsync(_request);

        var sent = capture.Calls.Should().ContainSingle().Subject.Request;
        sent.Method.Should().Be(HttpMethod.Get);
        sent.Endpoint.Should().Be("places/ChIJ-museo");
        sent.Headers!["X-Goog-FieldMask"].Should().StartWith("displayName,types,");
        sent.Cacheable.Should().BeTrue();
    }

    [Fact]
    public async Task GetDetailsAsync_CallerToken_ReachesTheApiClient() {
        using var cts = new CancellationTokenSource();
        var (service, capture) = Create();

        await service.GetDetailsAsync(_request, cts.Token);

        capture.Calls.Single().Ct.Should().Be(cts.Token);
    }

    [Fact]
    public async Task GetDetailsAsync_ApiResponse_IsMappedWithThePlaceIdFromTheRequest() {
        var (service, _) = Create();

        var details = await service.GetDetailsAsync(_request);

        details.PlaceId.Should().Be("ChIJ-museo");
        details.DisplayName.Should().Be("Museo del Oro");
    }

    [Theory]
    [MemberData(nameof(CoreErrors.Names), MemberType = typeof(CoreErrors))]
    public async Task GetDetailsAsync_CoreException_PropagatesUnchanged(string error) {
        var expected = CoreErrors.Create(error);
        var service = new DetailsService(new ApiClientCapture().Fail<PlaceDetailsDto>(expected).Client);

        var act = () => service.GetDetailsAsync(_request);

        (await act.Should().ThrowAsync<Exception>()).Which.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task GetDetailsAsync_CancelledToken_ThrowsOperationCanceledException() {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var capture = new ApiClientCapture().Handle((_, ct) => Task.FromCanceled<PlaceDetailsDto>(ct));
        var service = new DetailsService(capture.Client);

        var act = () => service.GetDetailsAsync(_request, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    // ===================== reviews =====================
    [Fact]
    public async Task GetReviewsAsync_NullRequest_ThrowsArgumentNullExceptionWithoutCallingGoogle() {
        var (service, capture) = Create(PlacesFixtures.ReviewsFull);

        var act = () => service.GetReviewsAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("detRequest");
        capture.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task GetReviewsAsync_ValidRequest_SendsOneCacheableReviewsRequest() {
        var (service, capture) = Create(PlacesFixtures.ReviewsFull);

        await service.GetReviewsAsync(_request);

        var sent = capture.Calls.Should().ContainSingle().Subject.Request;
        sent.Method.Should().Be(HttpMethod.Get);
        sent.Endpoint.Should().Be("places/ChIJ-museo");
        sent.Headers!["X-Goog-FieldMask"].Should().Be("displayName,reviewSummary,reviews,googleMapsLinks");
        sent.Cacheable.Should().BeTrue();
    }

    [Fact]
    public async Task GetReviewsAsync_CallerToken_ReachesTheApiClient() {
        using var cts = new CancellationTokenSource();
        var (service, capture) = Create(PlacesFixtures.ReviewsFull);

        await service.GetReviewsAsync(_request, cts.Token);

        capture.Calls.Single().Ct.Should().Be(cts.Token);
    }

    [Fact]
    public async Task GetReviewsAsync_ApiResponse_IsMappedWithThePlaceIdFromTheRequest() {
        var (service, _) = Create(PlacesFixtures.ReviewsFull);

        var reviews = await service.GetReviewsAsync(_request);

        reviews.PlaceId.Should().Be("ChIJ-museo");
        reviews.Reviews.Should().HaveCount(2);
    }

    [Theory]
    [MemberData(nameof(CoreErrors.Names), MemberType = typeof(CoreErrors))]
    public async Task GetReviewsAsync_CoreException_PropagatesUnchanged(string error) {
        var expected = CoreErrors.Create(error);
        var service = new DetailsService(new ApiClientCapture().Fail<PlaceDetailsDto>(expected).Client);

        var act = () => service.GetReviewsAsync(_request);

        (await act.Should().ThrowAsync<Exception>()).Which.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task GetReviewsAsync_CancelledToken_ThrowsOperationCanceledException() {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var capture = new ApiClientCapture().Handle((_, ct) => Task.FromCanceled<PlaceDetailsDto>(ct));
        var service = new DetailsService(capture.Client);

        var act = () => service.GetReviewsAsync(_request, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    // ===================== raw =====================
    [Fact]
    public async Task GetDetailsRawAsync_NullRequest_ThrowsArgumentNullExceptionWithoutCallingGoogle() {
        using var document = JsonDocument.Parse("{}");
        var capture = new ApiClientCapture().Respond(document);
        var service = new DetailsService(capture.Client);

        var act = () => service.GetDetailsRawAsync(null!, ["displayName"]);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("detRequest");
        capture.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDetailsRawAsync_InvalidFields_ThrowsMapsInvalidRequestExceptionWithoutCallingGoogle() {
        using var document = JsonDocument.Parse("{}");
        var capture = new ApiClientCapture().Respond(document);
        var service = new DetailsService(capture.Client);

        var act = () => service.GetDetailsRawAsync(_request, ["displayName", " "]);

        await act.Should().ThrowAsync<MapsInvalidRequestException>();
        capture.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDetailsRawAsync_ValidRequest_SendsTheNormalizedFieldMask() {
        using var document = JsonDocument.Parse("{}");
        var capture = new ApiClientCapture().Respond(document);
        var service = new DetailsService(capture.Client);

        await service.GetDetailsRawAsync(_request, [" displayName ", "rating", "displayName"]);

        var sent = capture.Calls.Should().ContainSingle().Subject.Request;
        sent.Method.Should().Be(HttpMethod.Get);
        sent.Endpoint.Should().Be("places/ChIJ-museo");
        sent.Headers!["X-Goog-FieldMask"].Should().Be("displayName,rating");
    }

    [Fact]
    public async Task GetDetailsRawAsync_ApiResponse_IsReturnedAsTheSameDocumentForTheCallerToOwn() {
        using var document = JsonDocument.Parse("""{ "displayName": { "text": "Museo del Oro" } }""");
        var service = new DetailsService(new ApiClientCapture().Respond(document).Client);

        var result = await service.GetDetailsRawAsync(_request, ["displayName"]);

        result.Should().BeSameAs(document);
    }

    [Fact]
    public async Task GetDetailsRawAsync_CallerToken_ReachesTheApiClient() {
        using var cts = new CancellationTokenSource();
        using var document = JsonDocument.Parse("{}");
        var capture = new ApiClientCapture().Respond(document);
        var service = new DetailsService(capture.Client);

        await service.GetDetailsRawAsync(_request, ["displayName"], cts.Token);

        capture.Calls.Single().Ct.Should().Be(cts.Token);
    }

    [Theory]
    [MemberData(nameof(CoreErrors.Names), MemberType = typeof(CoreErrors))]
    public async Task GetDetailsRawAsync_CoreException_PropagatesUnchanged(string error) {
        var expected = CoreErrors.Create(error);
        var service = new DetailsService(new ApiClientCapture().Fail<JsonDocument>(expected).Client);

        var act = () => service.GetDetailsRawAsync(_request, ["displayName"]);

        (await act.Should().ThrowAsync<Exception>()).Which.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task GetDetailsRawAsync_CancelledToken_ThrowsOperationCanceledException() {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var capture = new ApiClientCapture().Handle((_, ct) => Task.FromCanceled<JsonDocument>(ct));
        var service = new DetailsService(capture.Client);

        var act = () => service.GetDetailsRawAsync(_request, ["displayName"], cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    // ===================== photos =====================
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);

    private static string PhotoName(int i) => $"places/ChIJ-museo/photos/p{i}";

    private static PlaceDetailsDto WithPhotos(params string?[] names)
        => new() { Photos = [.. names.Select(n => new PhotoDto { Name = n, WidthPx = 800, HeightPx = 600 })] };

    private static Task<PhotoMediaDto> MediaFor(MapsApiRequest request)
        => Task.FromResult(new PhotoMediaDto { PhotoUri = $"https://lh3.example/{request.Endpoint}" });

    private static List<(MapsApiRequest Request, CancellationToken Ct)> MediaCalls(ApiClientCapture capture)
        => capture.Calls.Where(c => c.Request.Endpoint.EndsWith("/media", StringComparison.Ordinal)).ToList();

    private static (DetailsService Service, ApiClientCapture Capture) CreatePhotos(
        PlaceDetailsDto names, Func<MapsApiRequest, CancellationToken, Task<PhotoMediaDto>>? media = null
    ) {
        var capture = new ApiClientCapture()
            .Respond(names)
            .Handle(media ?? ((request, _) => MediaFor(request)));
        return (new DetailsService(capture.Client), capture);
    }

    [Fact]
    public async Task GetPhotosAsync_NullRequest_ThrowsArgumentNullExceptionWithoutCallingGoogle() {
        var (service, capture) = CreatePhotos(WithPhotos(PhotoName(1)));

        var act = () => service.GetPhotosAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("detRequest");
        capture.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPhotosAsync_ValidRequest_FirstAsksForThePhotoNamesUncachedWithTheCallerToken() {
        using var cts = new CancellationTokenSource();
        var (service, capture) = CreatePhotos(WithPhotos(PhotoName(1)));

        await service.GetPhotosAsync(_request, ct: cts.Token);

        var (names, ct) = capture.Calls[0];
        names.Method.Should().Be(HttpMethod.Get);
        names.Endpoint.Should().Be("places/ChIJ-museo");
        names.Headers!["X-Goog-FieldMask"].Should().Be("photos");
        names.Cacheable.Should().BeFalse();
        ct.Should().Be(cts.Token);
    }

    [Fact]
    public async Task GetPhotosAsync_SessionToken_GoesOnTheNamesCallOnly() {
        var (service, capture) = CreatePhotos(WithPhotos(PhotoName(1), PhotoName(2)));
        var request = new DetailsRequestBuilder().WithPlaceId("ChIJ-museo").WithSessionToken("tok-1").Build();

        await service.GetPhotosAsync(request);

        capture.Calls[0].Request.Query.Should().Contain("sessionToken", "tok-1");
        MediaCalls(capture).Should().AllSatisfy(c => c.Request.Query.Should().NotContainKey("sessionToken"));
    }

    [Fact]
    public async Task GetPhotosAsync_NamedPhotos_GetOneUncachedMediaCallEachAndKeepGoogleOrder() {
        var (service, capture) = CreatePhotos(WithPhotos(PhotoName(1), PhotoName(2), PhotoName(3)));

        var result = await service.GetPhotosAsync(_request);

        var media = MediaCalls(capture);
        media.Select(c => c.Request.Endpoint).Should().Equal(
            $"{PhotoName(1)}/media", $"{PhotoName(2)}/media", $"{PhotoName(3)}/media");
        media.Should().AllSatisfy(c => c.Request.Cacheable.Should().BeFalse());
        result.PlaceId.Should().Be("ChIJ-museo");
        result.Photos.Select(p => p.Name).Should().Equal(PhotoName(1), PhotoName(2), PhotoName(3));
        result.Photos[0].Uri.Should().Be(new Uri($"https://lh3.example/{PhotoName(1)}/media"));
    }

    [Fact]
    public async Task GetPhotosAsync_NullOptions_UsesTenPhotosAt480By480() {
        var (service, capture) = CreatePhotos(WithPhotos([.. Enumerable.Range(1, 12).Select(PhotoName)]));

        await service.GetPhotosAsync(_request, photoOptions: null);

        var media = MediaCalls(capture);
        media.Should().HaveCount(10);
        media.Should().AllSatisfy(c => c.Request.Query.Should().Contain("maxHeightPx", "480").And.Contain("maxWidthPx", "480"));
    }

    [Fact]
    public async Task GetPhotosAsync_MaxPhotos_CapsTheMediaCallsToTheFirstPhotosAtTheGivenResolution() {
        var (service, capture) = CreatePhotos(WithPhotos([.. Enumerable.Range(1, 5).Select(PhotoName)]));
        var options = new PhotoOptionsBuilder().WithMaxPhotos(2).WithResolution(1200, 800).Build();

        await service.GetPhotosAsync(_request, options);

        var media = MediaCalls(capture);
        media.Select(c => c.Request.Endpoint).Should().Equal($"{PhotoName(1)}/media", $"{PhotoName(2)}/media");
        media.Should().AllSatisfy(c => c.Request.Query.Should().Contain("maxHeightPx", "1200").And.Contain("maxWidthPx", "800"));
    }

    [Fact]
    public async Task GetPhotosAsync_NamelessPhotos_AreSkippedAndDoNotCountTowardsMaxPhotos() {
        var (service, capture) = CreatePhotos(WithPhotos(null, PhotoName(1), "  ", PhotoName(2), PhotoName(3)));

        await service.GetPhotosAsync(_request, new PhotoOptionsBuilder().WithMaxPhotos(2).Build());

        MediaCalls(capture).Select(c => c.Request.Endpoint).Should().Equal($"{PhotoName(1)}/media", $"{PhotoName(2)}/media");
    }

    [Fact]
    public async Task GetPhotosAsync_NoPhotos_ReturnsAnEmptyListWithoutMediaCalls() {
        var (service, capture) = CreatePhotos(WithPhotos());

        var result = await service.GetPhotosAsync(_request);

        result.PlaceId.Should().Be("ChIJ-museo");
        result.Photos.Should().BeEmpty();
        capture.Calls.Should().ContainSingle();
    }

    [Fact]
    public async Task GetPhotosAsync_MediaWithoutAValidUri_IsDroppedAndTheRestKeepTheirOrder() {
        var (service, _) = CreatePhotos(WithPhotos(PhotoName(1), PhotoName(2), PhotoName(3)),
            (request, _) => request.Endpoint.StartsWith(PhotoName(2), StringComparison.Ordinal)
                ? Task.FromResult(new PhotoMediaDto { PhotoUri = "javascript:alert(1)" })
                : MediaFor(request));

        var result = await service.GetPhotosAsync(_request);

        result.Photos.Select(p => p.Name).Should().Equal(PhotoName(1), PhotoName(3));
    }

    [Fact]
    public async Task GetPhotosAsync_MediaCalls_GetALinkedTokenNotTheCallerToken() {
        using var cts = new CancellationTokenSource();
        var (service, capture) = CreatePhotos(WithPhotos(PhotoName(1), PhotoName(2)));

        await service.GetPhotosAsync(_request, ct: cts.Token);

        MediaCalls(capture).Should().AllSatisfy(c => {
            c.Ct.Should().NotBe(cts.Token);
            c.Ct.CanBeCanceled.Should().BeTrue();
        });
    }

    [Fact]
    public async Task GetPhotosAsync_CallerCancelsWhileMediaCallsAreInFlight_CancelsThemAllAndThrowsOperationCanceledException() {
        using var cts = new CancellationTokenSource();
        var started = 0;
        var allStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var (service, capture) = CreatePhotos(WithPhotos(PhotoName(1), PhotoName(2), PhotoName(3)), async (_, ct) => {
            if (Interlocked.Increment(ref started) == 3) allStarted.TrySetResult();
            await Task.Delay(Timeout.Infinite, ct);
            return null!;
        });

        var call = service.GetPhotosAsync(_request, ct: cts.Token);
        await allStarted.Task.WaitAsync(_timeout);
        await cts.CancelAsync();

        await FluentActions.Awaiting(() => call.WaitAsync(_timeout)).Should().ThrowAsync<OperationCanceledException>();
        MediaCalls(capture).Should().HaveCount(3).And.AllSatisfy(c => c.Ct.IsCancellationRequested.Should().BeTrue());
    }

    [Fact]
    public async Task GetPhotosAsync_OneMediaCallFails_CancelsTheOthersAndRethrowsTheOriginalException() {
        // One failed photo fails the whole call, so the remaining (billed) requests must stop
        using var cts = new CancellationTokenSource();
        var expected = new MapsNotFoundException(status: HttpStatusCode.NotFound); // e.g. an expired photo name
        var started = 0;
        var allStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var (service, capture) = CreatePhotos(WithPhotos(PhotoName(1), PhotoName(2), PhotoName(3)), async (request, ct) => {
            if (Interlocked.Increment(ref started) == 3) allStarted.TrySetResult();
            if (request.Endpoint.StartsWith(PhotoName(2), StringComparison.Ordinal)) {
                await allStarted.Task;
                throw expected;
            }
            await Task.Delay(Timeout.Infinite, ct);
            return null!;
        });

        var act = () => service.GetPhotosAsync(_request, ct: cts.Token).WaitAsync(_timeout);

        (await act.Should().ThrowAsync<MapsNotFoundException>()).Which.Should().BeSameAs(expected);
        MediaCalls(capture).Where(c => !c.Request.Endpoint.StartsWith(PhotoName(2), StringComparison.Ordinal))
            .Should().HaveCount(2).And.AllSatisfy(c => c.Ct.IsCancellationRequested.Should().BeTrue());
        cts.IsCancellationRequested.Should().BeFalse("only the internal linked token is cancelled, never the caller's");
    }

    [Theory]
    [MemberData(nameof(CoreErrors.Names), MemberType = typeof(CoreErrors))]
    public async Task GetPhotosAsync_NamesCallFails_PropagatesUnchangedWithoutMediaCalls(string error) {
        var expected = CoreErrors.Create(error);
        var capture = new ApiClientCapture().Fail<PlaceDetailsDto>(expected).Handle((request, _) => MediaFor(request));
        var service = new DetailsService(capture.Client);

        var act = () => service.GetPhotosAsync(_request);

        (await act.Should().ThrowAsync<Exception>()).Which.Should().BeSameAs(expected);
        MediaCalls(capture).Should().BeEmpty();
    }

    [Fact]
    public async Task GetPhotosAsync_CancelledToken_ThrowsOperationCanceledExceptionWithoutMediaCalls() {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var capture = new ApiClientCapture()
            .Handle((_, ct) => Task.FromCanceled<PlaceDetailsDto>(ct))
            .Handle((request, _) => MediaFor(request));
        var service = new DetailsService(capture.Client);

        var act = () => service.GetPhotosAsync(_request, ct: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        MediaCalls(capture).Should().BeEmpty();
    }
}
