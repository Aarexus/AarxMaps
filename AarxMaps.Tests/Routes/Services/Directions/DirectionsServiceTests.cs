using System.Text.Json;
using AarxMaps.Core.Internal.Json;
using AarxMaps.Routes.Common.Models;
using AarxMaps.Routes.Internal.Services.Directions;
using AarxMaps.Routes.Internal.Services.Directions.DTOs;
using AarxMaps.Routes.Services.Directions;
using AarxMaps.Tests.Core.Helpers;
using AarxMaps.Tests.Routes.Helpers;
using FluentAssertions;

namespace AarxMaps.Tests.Routes.Services.Directions;

[Trait("Package", "Routes")]
[Trait("Module", "Services")]
[Trait("Feature", "Directions")]
[Trait("Type", "DirectionsService")]
public class DirectionsServiceTests {
    private static readonly RouteRequest _request = new RouteRequestBuilder()
        .From(RoutesSamples.At(RoutesSamples.Bogota)).To(RoutesSamples.At(RoutesSamples.Usaquen)).Build();

    private static (DirectionsService Service, ApiClientCapture Capture) Create() {
        var capture = new ApiClientCapture()
            .Respond(MapsJsonSerializer.Deserialize<ComputeRoutesResponse>(RoutesFixtures.RoutesDrive)!);
        return (new DirectionsService(capture.Client), capture);
    }

    // ===================== typed =====================
    [Fact]
    public async Task GetRoutesAsync_NullRequest_ThrowsArgumentNullExceptionWithoutCallingGoogle() {
        var (service, capture) = Create();

        var act = () => service.GetRoutesAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("routeRequest");
        capture.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRoutesAsync_DefaultLevel_SendsOneComputeRoutesRequestWithTheStandardMask() {
        var (service, capture) = Create();

        await service.GetRoutesAsync(_request);

        var sent = capture.Calls.Should().ContainSingle().Subject.Request;
        sent.Method.Should().Be(HttpMethod.Post);
        sent.Endpoint.Should().Be("directions/v2:computeRoutes");
        sent.Headers!["X-Goog-FieldMask"].Should().Be(
            DirectionsFactory.CreateRoutesRequest(_request, RouteDetailLevel.Standard).Headers!["X-Goog-FieldMask"]);
        sent.Payload.Should().BeOfType<ComputeRoutesPayload>();
    }

    [Fact]
    public async Task GetRoutesAsync_FullLevel_SendsTheFullMask() {
        var (service, capture) = Create();

        await service.GetRoutesAsync(_request, RouteDetailLevel.Full);

        capture.Calls.Single().Request.Headers!["X-Goog-FieldMask"].Should().EndWith(",routes.legs");
    }

    [Fact]
    public async Task GetRoutesAsync_CallerToken_ReachesTheApiClient() {
        using var cts = new CancellationTokenSource();
        var (service, capture) = Create();

        await service.GetRoutesAsync(_request, ct: cts.Token);

        capture.Calls.Single().Ct.Should().Be(cts.Token);
    }

    [Fact]
    public async Task GetRoutesAsync_ApiResponse_IsMapped() {
        var (service, _) = Create();

        var result = await service.GetRoutesAsync(_request);

        result.Routes.Select(r => r.DistanceMeters).Should().Equal(1200, 1500);
        result.FallbackInfo.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(CoreErrors.Names), MemberType = typeof(CoreErrors))]
    public async Task GetRoutesAsync_CoreException_PropagatesUnchanged(string error) {
        var expected = CoreErrors.Create(error);
        var service = new DirectionsService(new ApiClientCapture().Fail<ComputeRoutesResponse>(expected).Client);

        var act = () => service.GetRoutesAsync(_request);

        (await act.Should().ThrowAsync<Exception>()).Which.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task GetRoutesAsync_CancelledToken_ThrowsOperationCanceledException() {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var capture = new ApiClientCapture().Handle((_, ct) => Task.FromCanceled<ComputeRoutesResponse>(ct));
        var service = new DirectionsService(capture.Client);

        var act = () => service.GetRoutesAsync(_request, ct: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    // ===================== raw =====================
    [Fact]
    public async Task GetRoutesRawAsync_NullRequest_ThrowsArgumentNullExceptionWithoutCallingGoogle() {
        var capture = new ApiClientCapture();
        var service = new DirectionsService(capture.Client);

        var act = () => service.GetRoutesRawAsync(null!, ["routes.duration"]);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("routeRequest");
        capture.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRoutesRawAsync_Fields_AreTheMaskAndTheDocumentIsReturnedAsIs() {
        using var cts = new CancellationTokenSource();
        var document = JsonDocument.Parse("""{ "routes": [] }""");
        var capture = new ApiClientCapture().Respond(document);
        var service = new DirectionsService(capture.Client);

        using var result = await service.GetRoutesRawAsync(_request, ["routes.duration", "routes.legs.steps"], cts.Token);

        result.Should().BeSameAs(document);
        var (sent, ct) = capture.Calls.Should().ContainSingle().Subject;
        sent.Headers!["X-Goog-FieldMask"].Should().Be("routes.duration,routes.legs.steps");
        ct.Should().Be(cts.Token);
    }
}
