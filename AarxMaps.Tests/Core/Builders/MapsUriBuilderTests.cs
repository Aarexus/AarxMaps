using System.Web;
using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Internal.Builders;
using FluentAssertions;

namespace AarxMaps.Tests.Core.Builders;

[Trait("Package", "Core")]
[Trait("Module", "Builders")]
[Trait("Type", "MapsUriBuilder")]
public class MapsUriBuilderTests {
    private static readonly Uri Base = new("https://api.test/");
    private static MapsUriBuilder WithDefaultPath() => new MapsUriBuilder().WithPath(Base, "v1/things");
    private static string[] QueryPairs(Uri uri)
        => uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);

    // -------------------- PATH --------------------
    [Fact]
    public void Build_PathNotSet_ThrowsMapsInvalidRequestException() {
        var builder = new MapsUriBuilder();
        var act = () => builder.Build();
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("*path*");
    }

    [Fact]
    public void Build_OnlyQuerySet_ThrowsMapsInvalidRequestException() {
        var builder = new MapsUriBuilder().AddQuery(new Dictionary<string, string> { ["a"] = "1" });
        var act = () => builder.Build();
        act.Should().Throw<MapsInvalidRequestException>();
    }

    [Theory]
    [InlineData("https://api.test", "v1/foo", "https://api.test/v1/foo")]
    [InlineData("https://api.test/", "v1/foo", "https://api.test/v1/foo")]
    [InlineData("https://api.test", "/v1/foo", "https://api.test/v1/foo")]
    [InlineData("https://api.test/", "/v1/foo", "https://api.test/v1/foo")]
    [InlineData("https://api.test/", "//v1/foo", "https://api.test/v1/foo")]
    [InlineData("https://api.test/v1", "foo", "https://api.test/v1/foo")]
    [InlineData("https://api.test/v1/", "/foo", "https://api.test/v1/foo")]
    [InlineData("https://api.test/", "v1/foo/", "https://api.test/v1/foo/")]
    [InlineData("https://api.test", "", "https://api.test/")]
    [InlineData("https://api.test/", "/", "https://api.test/")]
    public void Build_BaseAndEndpoint_JoinsWithExactlyOneSlash(string baseUrl, string endpoint, string expected) {
        var uri = new MapsUriBuilder().WithPath(new Uri(baseUrl), endpoint).Build();
        uri.AbsoluteUri.Should().Be(expected);
    }

    [Theory]
    [InlineData("https://routes.googleapis.com/", "directions/v2:computeRoutes", "https://routes.googleapis.com/directions/v2:computeRoutes")]
    [InlineData("https://routes.googleapis.com/", "distanceMatrix/v2:computeRouteMatrix", "https://routes.googleapis.com/distanceMatrix/v2:computeRouteMatrix")]
    [InlineData("https://places.googleapis.com/", "v1/places:searchText", "https://places.googleapis.com/v1/places:searchText")]
    public void Build_GoogleStyleCustomMethodEndpoints_ArePreservedVerbatim(string baseUrl, string endpoint, string expected) {
        var uri = new MapsUriBuilder().WithPath(new Uri(baseUrl), endpoint).Build();
        uri.AbsoluteUri.Should().Be(expected);
    }

    [Theory]
    [InlineData("http://localhost:5000/",  "v1/x", "http://localhost:5000/v1/x")]
    [InlineData("https://api.test:443/",   "v1/x", "https://api.test/v1/x")]
    public void Build_Port_IsHandledByScheme(string baseUrl, string endpoint, string expected) {
        var uri = new MapsUriBuilder().WithPath(new Uri(baseUrl), endpoint).Build();
        uri.AbsoluteUri.Should().Be(expected);
    }

    [Fact]
    public void WithPath_CalledTwice_LastCallWins() {
        var builder = new MapsUriBuilder()
            .WithPath(new Uri("https://first.test/"), "a")
            .WithPath(new Uri("https://second.test/"), "b");

        builder.Build().AbsoluteUri.Should().Be("https://second.test/b");
    }

    // -------------------- QUERY --------------------
    [Fact]
    public void Build_NoQuery_HasNoQuestionMark() {
        var uri = WithDefaultPath().Build();
        uri.Query.Should().BeEmpty();
        uri.AbsoluteUri.Should().NotContain("?");
    }

    [Fact]
    public void Build_SingleQueryParam_AppendsKeyValue() {
        var uri = WithDefaultPath().AddQuery(new Dictionary<string, string> { ["a"] = "1" }).Build();
        uri.AbsoluteUri.Should().Be("https://api.test/v1/things?a=1");
    }

    [Fact]
    public void Build_MultipleQueryParams_AllPresentSeparatedByAmpersand() {
        var uri = WithDefaultPath()
            .AddQuery(new Dictionary<string, string> { ["a"] = "1", ["b"] = "2", ["c"] = "3" })
            .Build();

        QueryPairs(uri).Should().BeEquivalentTo(new[] { "a=1", "b=2", "c=3" });
    }

    [Fact]
    public void Build_QueryValueWithReservedAndUnicodeChars_IsEscapedAndRoundTrips() {
        const string value = "a b&c=d+é/?#%";

        var uri = WithDefaultPath().AddQuery(new Dictionary<string, string> { ["q"] = value }).Build();

        uri.Query.Should().Be("?q=" + Uri.EscapeDataString(value));
        HttpUtility.ParseQueryString(uri.Query)["q"].Should().Be(value);
    }

    [Fact]
    public void Build_QueryKeyWithSpecialChars_IsEscaped() {
        var uri = WithDefaultPath().AddQuery(new Dictionary<string, string> { ["a b&c"] = "1" }).Build();
        uri.Query.Should().Be("?a%20b%26c=1");
    }

    [Fact]
    public void Build_EmptyQueryValue_EmitsKeyWithEmptyValue() {
        var uri = WithDefaultPath().AddQuery(new Dictionary<string, string> { ["k"] = "" }).Build();
        uri.Query.Should().Be("?k=");
    }

    [Fact]
    public void AddQuery_SameKeyAcrossCalls_LastValueWins_OtherKeysAccumulate() {
        var uri = WithDefaultPath()
            .AddQuery(new Dictionary<string, string> { ["a"] = "1" })
            .AddQuery(new Dictionary<string, string> { ["a"] = "2", ["b"] = "3" })
            .Build();

        QueryPairs(uri).Should().BeEquivalentTo([ "a=2", "b=3" ]);
    }

    [Fact]
    public void AddQuery_KeysDifferingOnlyByCase_AreDistinct() {
        var uri = WithDefaultPath()
            .AddQuery(new Dictionary<string, string> { ["key"] = "1", ["Key"] = "2" })
            .Build();

        QueryPairs(uri).Should().BeEquivalentTo([ "key=1", "Key=2" ]);
    }

    [Fact]
    public void AddQuery_EmptyDictionary_LeavesQueryEmpty() {
        var uri = WithDefaultPath().AddQuery(new Dictionary<string, string>()).Build();
        uri.Query.Should().BeEmpty();
    }

    // -------------------- BUILDER BEHAVIOUR --------------------
    [Fact]
    public void Build_CalledTwice_ReturnsEqualUris() {
        var builder = WithDefaultPath().AddQuery(new Dictionary<string, string> { ["a"] = "1" });
        builder.Build().Should().Be(builder.Build());
    }

    [Fact]
    public void Build_ReturnsAbsoluteUri() {
        WithDefaultPath().Build().IsAbsoluteUri.Should().BeTrue();
    }

    [Fact]
    public void ConfigMethods_ReturnSameInstance_ForFluentChaining() {
        var builder = new MapsUriBuilder();

        builder.WithPath(Base, "x").Should().BeSameAs(builder);
        builder.AddQuery(new Dictionary<string, string>()).Should().BeSameAs(builder);
    }
}