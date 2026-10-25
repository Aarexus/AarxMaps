using AarxMaps.Core.Internal.Json;
using AarxMaps.Places.Internal.Services.Autocomplete;
using AarxMaps.Places.Internal.Services.Autocomplete.DTOs;
using AarxMaps.Places.Services.Autocomplete;
using AarxMaps.Tests.Core.Helpers;
using AarxMaps.Tests.Places.Helpers;
using FluentAssertions;

namespace AarxMaps.Tests.Places.Services.Autocomplete;

[Trait("Package", "Places")]
[Trait("Module", "Services")]
[Trait("Feature", "Autocomplete")]
[Trait("Type", "AutocompleteService")]
public class AutocompleteServiceTests {
    private static readonly AutocompleteRequest _request = new AutocompleteRequestBuilder().WithInput("Bogota").Build();

    private static AutocompleteResponse SingleResponse()
        => MapsJsonSerializer.Deserialize<AutocompleteResponse>(PlacesFixtures.AutocompleteSingle)!;

    [Fact]
    public async Task SuggestPlacesAsync_NullRequest_ThrowsArgumentNullExceptionWithoutCallingGoogle() {
        var capture = new ApiClientCapture().Respond(SingleResponse());
        var service = new AutocompleteService(capture.Client);

        var act = () => service.SuggestPlacesAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("autocompleteRequest");
        capture.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task SuggestPlacesAsync_ValidRequest_SendsOneAutocompleteRequestCarryingThePayload() {
        var capture = new ApiClientCapture().Respond(SingleResponse());
        var service = new AutocompleteService(capture.Client);

        await service.SuggestPlacesAsync(_request);

        var sent = capture.Calls.Should().ContainSingle().Subject.Request;
        sent.Method.Should().Be(HttpMethod.Post);
        sent.Endpoint.Should().Be("places:autocomplete");
        sent.Payload.Should().BeSameAs(_request);
    }

    [Fact]
    public async Task SuggestPlacesAsync_CallerToken_ReachesTheApiClient() {
        using var cts = new CancellationTokenSource();
        var capture = new ApiClientCapture().Respond(SingleResponse());
        var service = new AutocompleteService(capture.Client);

        await service.SuggestPlacesAsync(_request, cts.Token);

        capture.Calls.Single().Ct.Should().Be(cts.Token);
    }

    [Fact]
    public async Task SuggestPlacesAsync_ApiResponse_IsMappedToSuggestions() {
        var capture = new ApiClientCapture().Respond(SingleResponse());
        var service = new AutocompleteService(capture.Client);

        var suggestions = await service.SuggestPlacesAsync(_request);

        suggestions.Should().ContainSingle().Which.PlaceId.Should().Be("ChIJ-place-1");
    }

    [Theory]
    [MemberData(nameof(CoreErrors.Names), MemberType = typeof(CoreErrors))]
    public async Task SuggestPlacesAsync_CoreException_PropagatesUnchanged(string error) {
        var expected = CoreErrors.Create(error);
        var capture = new ApiClientCapture().Fail<AutocompleteResponse>(expected);
        var service = new AutocompleteService(capture.Client);

        var act = () => service.SuggestPlacesAsync(_request);

        (await act.Should().ThrowAsync<Exception>()).Which.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task SuggestPlacesAsync_CancelledToken_ThrowsOperationCanceledException() {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var capture = new ApiClientCapture().Handle((_, ct) => Task.FromCanceled<AutocompleteResponse>(ct));
        var service = new AutocompleteService(capture.Client);

        var act = () => service.SuggestPlacesAsync(_request, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
