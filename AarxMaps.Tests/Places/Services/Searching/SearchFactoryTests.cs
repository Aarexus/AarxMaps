using AarxMaps.Core.Enums;
using AarxMaps.Core.Models;
using AarxMaps.Core.Models.Geometry;
using AarxMaps.Places.Internal.Services.Searching;
using AarxMaps.Places.Services.Searching;
using FluentAssertions;

namespace AarxMaps.Tests.Places.Services.Searching;

[Trait("Package", "Places")]
[Trait("Module", "Services")]
[Trait("Feature", "Searching")]
[Trait("Type", "SearchFactory")]
public class SearchFactoryTests {
    private const string TextFieldMask = "nextPageToken,places.id,places.displayName,places.types";
    private const string NearbyFieldMask = "places.id,places.displayName,places.types";

    private static readonly TextRequest _text = new TextRequestBuilder().WithTextQuery("pizza").Build();
    private static readonly NearbyRequest _nearby =
        new NearbyRequestBuilder().WithLocationRestriction(new LatLng(4.6097, -74.0817), 1500).Build();

    private static void ShouldBeCacheablePostWithHeaderKey(MapsApiRequest request) {
        request.Method.Should().Be(HttpMethod.Post);
        request.BaseUrl.Should().Be(new Uri("https://places.googleapis.com/v1/"));
        request.Query.Should().BeNull();
        request.Cacheable.Should().BeTrue();
        request.ApiKeyLocation.Should().Be(ApiKeyLocation.Header);
    }

    // ===================== text =====================
    [Fact]
    public void CreateTextRequest_Always_PostsToSearchTextAsACacheableRequest() {
        var request = SearchFactory.CreateTextRequest(_text);

        request.Endpoint.Should().Be("places:searchText");
        ShouldBeCacheablePostWithHeaderKey(request);
    }

    [Fact]
    public void CreateTextRequest_Always_SendsOnlyTheFieldMaskIncludingNextPageToken()
        => SearchFactory.CreateTextRequest(_text).Headers.Should().ContainSingle()
            .Which.Should().Be(new KeyValuePair<string, string>("X-Goog-FieldMask", TextFieldMask));

    [Fact]
    public void CreateTextRequest_Always_UsesThePublicRequestAsThePayload()
        => SearchFactory.CreateTextRequest(_text).Payload.Should().BeSameAs(_text);

    // ===================== nearby =====================
    [Fact]
    public void CreateNearbyRequest_Always_PostsToSearchNearbyAsACacheableRequest() {
        var request = SearchFactory.CreateNearbyRequest(_nearby);

        request.Endpoint.Should().Be("places:searchNearby");
        ShouldBeCacheablePostWithHeaderKey(request);
    }

    [Fact]
    public void CreateNearbyRequest_Always_SendsOnlyTheFieldMaskWithoutNextPageToken()
        => SearchFactory.CreateNearbyRequest(_nearby).Headers.Should().ContainSingle()
            .Which.Should().Be(new KeyValuePair<string, string>("X-Goog-FieldMask", NearbyFieldMask));

    [Fact]
    public void CreateNearbyRequest_Always_UsesThePublicRequestAsThePayload()
        => SearchFactory.CreateNearbyRequest(_nearby).Payload.Should().BeSameAs(_nearby);
}
