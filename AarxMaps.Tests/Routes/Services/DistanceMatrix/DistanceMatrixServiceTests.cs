using AarxMaps.Core.Models;
using AarxMaps.Routes.Common.Models;
using AarxMaps.Routes.Internal.Services.DistanceMatrix;
using AarxMaps.Routes.Internal.Services.DistanceMatrix.DTOs;
using AarxMaps.Routes.Services.DistanceMatrix;
using AarxMaps.Tests.Core.Helpers;
using AarxMaps.Tests.Routes.Helpers;
using FluentAssertions;

namespace AarxMaps.Tests.Routes.Services.DistanceMatrix;

[Trait("Package", "Routes")]
[Trait("Module", "Services")]
[Trait("Feature", "DistanceMatrix")]
[Trait("Type", "DistanceMatrixService")]
public class DistanceMatrixServiceTests {
    private static readonly DistanceRequest _request = new DistanceRequestBuilder()
        .WithOrigins([RoutesSamples.At(RoutesSamples.Bogota), RoutesSamples.At(RoutesSamples.Chapinero)])
        .WithDestinations([RoutesSamples.At(RoutesSamples.Usaquen), RoutesSamples.Place("ChIJ-usaquen")])
        .Build();

    private static (DistanceMatrixService Service, ApiClientCapture Capture) Create(string json = RoutesFixtures.Matrix2x2) {
        var capture = new ApiClientCapture().RespondStream(json);
        return (new DistanceMatrixService(capture.Client), capture);
    }

    [Fact]
    public async Task GetDistancesAsync_NullRequest_ThrowsArgumentNullExceptionWithoutCallingGoogle() {
        var (service, capture) = Create();

        var act = () => service.GetDistancesAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("distanceRequest");
        capture.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDistancesAsync_DefaultLevel_StreamsOneComputeRouteMatrixRequestWithTheStandardMask() {
        var (service, capture) = Create();

        await service.GetDistancesAsync(_request);

        var sent = capture.Calls.Should().ContainSingle().Subject.Request;
        sent.Method.Should().Be(HttpMethod.Post);
        sent.Endpoint.Should().Be("distanceMatrix/v2:computeRouteMatrix");
        sent.Headers!["X-Goog-FieldMask"].Should().EndWith(",staticDuration,localizedValues");
        sent.Payload.Should().BeOfType<ComputeRouteMatrixPayload>();
    }

    [Fact]
    public async Task GetDistancesAsync_CallerToken_ReachesTheApiClient() {
        using var cts = new CancellationTokenSource();
        var (service, capture) = Create();

        await service.GetDistancesAsync(_request, RouteDetailLevel.Summary, cts.Token);

        capture.Calls.Single().Ct.Should().Be(cts.Token);
    }

    [Fact]
    public async Task GetDistancesAsync_ElementsStreamedOutOfOrder_AreReturnedByOriginThenDestination() {
        var (service, _) = Create();

        var elements = await service.GetDistancesAsync(_request);

        elements.Select(e => (e.OriginIndex, e.DestinationIndex)).Should().Equal((0, 0), (0, 1), (1, 0), (1, 1));
        elements[0].DistanceMeters.Should().Be(822);
        elements[1].Condition.Should().Be(RouteMatrixElementCondition.RouteNotFound);
        elements[2].Status!.Code.Should().Be(3);
        elements[3].Duration.Should().Be(TimeSpan.FromSeconds(400));
    }

    [Fact]
    public async Task GetDistancesAsync_EmptyArray_ReturnsNoElements() {
        var (service, _) = Create("[]");
        (await service.GetDistancesAsync(_request)).Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(CoreErrors.Names), MemberType = typeof(CoreErrors))]
    public async Task GetDistancesAsync_CoreException_PropagatesUnchanged(string error) {
        var expected = CoreErrors.Create(error);
        var service = new DistanceMatrixService(new ApiClientCapture().FailStream(expected).Client);

        var act = () => service.GetDistancesAsync(_request);

        (await act.Should().ThrowAsync<Exception>()).Which.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task GetDistancesAsync_CancelledToken_ThrowsOperationCanceledException() {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var capture = new ApiClientCapture().HandleStream((_, ct) => Task.FromCanceled<StreamResponse>(ct));
        var service = new DistanceMatrixService(capture.Client);

        var act = () => service.GetDistancesAsync(_request, ct: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
