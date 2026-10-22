using AarxMaps.Core.Enums;
using AarxMaps.Places.Internal.Services.Autocomplete;
using AarxMaps.Places.Services.Autocomplete;
using FluentAssertions;

namespace AarxMaps.Tests.Places.Services.Autocomplete;

[Trait("Package", "Places")]
[Trait("Module", "Services")]
[Trait("Feature", "Autocomplete")]
[Trait("Type", "AutocompleteFactory")]
public class AutocompleteFactoryTests {
    private const string FieldMask =
        "suggestions.placePrediction.placeId,suggestions.placePrediction.text,suggestions.placePrediction.types";

    private static readonly AutocompleteRequest _request = new AutocompleteRequestBuilder().WithInput("Bogota").Build();

    [Fact]
    public void CreateRequest_Always_PostsToTheAutocompleteEndpoint() {
        var request = AutocompleteFactory.CreateRequest(_request);

        request.Method.Should().Be(HttpMethod.Post);
        request.BaseUrl.Should().Be(new Uri("https://places.googleapis.com/v1/"));
        request.Endpoint.Should().Be("places:autocomplete");
        request.Query.Should().BeNull();
    }

    [Fact]
    public void CreateRequest_Always_SendsOnlyTheFieldMaskHeader() {
        var request = AutocompleteFactory.CreateRequest(_request);

        request.Headers.Should().ContainSingle()
            .Which.Should().Be(new KeyValuePair<string, string>("X-Goog-FieldMask", FieldMask));
    }

    [Fact]
    public void CreateRequest_Always_UsesThePublicRequestAsThePayload()
        => AutocompleteFactory.CreateRequest(_request).Payload.Should().BeSameAs(_request);

    [Fact]
    public void CreateRequest_Always_IsCacheableWithTheKeyInTheHeader() {
        var request = AutocompleteFactory.CreateRequest(_request);

        request.Cacheable.Should().BeTrue();
        request.ApiKeyLocation.Should().Be(ApiKeyLocation.Header);
    }
}
