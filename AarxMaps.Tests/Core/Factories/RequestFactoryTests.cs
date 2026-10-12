using AarxMaps.Core.Enums;
using AarxMaps.Core.Internal.Factories;
using AarxMaps.Tests.Core.Helpers;
using FluentAssertions;

namespace AarxMaps.Tests.Core.Factories;

[Trait("Package", "Core")]
[Trait("Module", "Factories")]
[Trait("Type", "RequestFactory")]
public class RequestFactoryTests {
    private const string ApiKey = TestRequests.DefaultApiKey;

    private static RequestFactory NewFactory(string apiKey = ApiKey, string language = "en", string region = "US")
        => new(TestRequests.ConfigOptions(apiKey, language, region));

    private static Dictionary<string, string> Dict(params (string Key, string Value)[] pairs)
        => pairs.ToDictionary(p => p.Key, p => p.Value);

    // -------------------- BASICS --------------------
    [Fact]
    public void CreateRequest_MinimalRequest_ComposesMethodAndUri_WithoutContent() {
        var request = TestRequests.Request(baseUrl: new Uri("https://maps.test/v1/"), endpoint: "/things");

        using var message = NewFactory().CreateRequest(request);

        message.Method.Should().Be(HttpMethod.Get);
        message.RequestUri.Should().Be(new Uri("https://maps.test/v1/things"));
        message.Content.Should().BeNull();
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public void CreateRequest_Method_IsPassedThrough(string method) {
        using var message = NewFactory().CreateRequest(TestRequests.Request(method: new HttpMethod(method)));
        message.Method.Should().Be(new HttpMethod(method));
    }

    [Fact]
    public void CreateRequest_CalledTwice_ReturnsDistinctMessages() {
        var factory = NewFactory();
        var request = TestRequests.Request(payload: new { A = 1 }, method: HttpMethod.Post);

        using var first = factory.CreateRequest(request);
        using var second = factory.CreateRequest(request);

        second.Should().NotBeSameAs(first);
        second.Content.Should().NotBeSameAs(first.Content);
    }

    // -------------------- DEFAULT HEADERS (Accept-Language) --------------------
    [Theory]
    [InlineData("en", "US", "en-US")]
    [InlineData("es", "CO", "es-CO")]
    [InlineData("pt", "BR", "pt-BR")]
    public void CreateRequest_Always_AddsAcceptLanguageFromConfig(string language, string region, string expected) {
        using var message = NewFactory(language: language, region: region).CreateRequest(TestRequests.Request());

        message.Headers.GetValues("Accept-Language").Should().Equal(expected);
    }

    [Fact]
    public void CreateRequest_RequestDefinesAcceptLanguage_OverridesConfigDefault() {
        var request = TestRequests.Request(headers: Dict(("Accept-Language", "fr-FR")));

        using var message = NewFactory().CreateRequest(request);

        message.Headers.GetValues("Accept-Language").Should().Equal("fr-FR");
    }

    [Fact]
    public void CreateRequest_RequestDefinesAcceptLanguageWithDifferentCasing_OverridesConfigDefault() {
        var request = TestRequests.Request(headers: Dict(("accept-language", "fr-FR")));

        using var message = NewFactory().CreateRequest(request);

        message.Headers.GetValues("Accept-Language").Should().Equal("fr-FR");
    }

    // -------------------- REQUEST HEADERS / QUERY / PAYLOAD --------------------
    [Fact]
    public void CreateRequest_RequestHeaders_AreAddedAlongsideDefaults() {
        var request = TestRequests.Request(headers: Dict(("X-Goog-FieldMask", "routes.duration")));

        using var message = NewFactory().CreateRequest(request);

        message.Headers.GetValues("X-Goog-FieldMask").Should().Equal("routes.duration");
        message.Headers.GetValues("Accept-Language").Should().Equal("en-US");
        message.Headers.GetValues("X-Goog-Api-Key").Should().Equal(ApiKey);
    }

    [Fact]
    public void CreateRequest_NullOrEmptyHeadersAndQuery_AddsOnlyDefaultAndKeyHeader() {
        var factory = NewFactory();

        foreach (var request in new[]
            { 
                TestRequests.Request(headers: null, query: null),
                TestRequests.Request(headers: new Dictionary<string, string>(), query: new Dictionary<string, string>())
            }
        ) {
            using var message = factory.CreateRequest(request);

            message.Headers.Select(h => h.Key).Should().BeEquivalentTo("Accept-Language", "X-Goog-Api-Key");
            message.RequestUri!.Query.Should().BeEmpty();
        }
    }

    [Fact]
    public void CreateRequest_RequestQuery_IsAppendedToUri() {
        var request = TestRequests.Request(query: Dict(("languageCode", "es"), ("q", "a b")));

        using var message = NewFactory().CreateRequest(request);

        TestRequests.QueryPairs(message.RequestUri!).Should().BeEquivalentTo("languageCode=es", "q=a%20b");
    }

    [Fact]
    public void CreateRequest_NullPayload_HasNoContent() {
        using var message = NewFactory().CreateRequest(TestRequests.Request(method: HttpMethod.Post, payload: null));

        message.Content.Should().BeNull();
    }

    [Fact]
    public async Task CreateRequest_Payload_IsSerializedAsJsonContent() {
        var request = TestRequests.Request(method: HttpMethod.Post, payload: new { Origins = new[] { "a", "b" } });

        using var message = NewFactory().CreateRequest(request);

        message.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
        (await message.Content.ReadAsStringAsync()).Should().Be("""{"origins":["a","b"]}""");
    }

    // -------------------- API KEY PLACEMENT --------------------
    [Fact]
    public void CreateRequest_HeaderLocation_PutsKeyInHeaderOnly() {
        var request = TestRequests.Request(apiKeyLocation: ApiKeyLocation.Header, query: Dict(("a", "1")));

        using var message = NewFactory().CreateRequest(request);

        message.Headers.GetValues("X-Goog-Api-Key").Should().Equal(ApiKey);
        TestRequests.QueryPairs(message.RequestUri!).Should().BeEquivalentTo("a=1");
    }

    [Fact]
    public void CreateRequest_QueryLocation_PutsKeyInQueryOnly() {
        var request = TestRequests.Request(apiKeyLocation: ApiKeyLocation.Query, query: Dict(("a", "1")));

        using var message = NewFactory().CreateRequest(request);

        message.Headers.Contains("X-Goog-Api-Key").Should().BeFalse();
        TestRequests.QueryPairs(message.RequestUri!).Should().BeEquivalentTo("a=1", $"key={ApiKey}");
    }

    [Fact]
    public void CreateRequest_UndefinedApiKeyLocation_FallsBackToHeader() {
        var request = TestRequests.Request(apiKeyLocation: (ApiKeyLocation)999);

        using var message = NewFactory().CreateRequest(request);

        message.Headers.GetValues("X-Goog-Api-Key").Should().Equal(ApiKey);
        message.RequestUri!.Query.Should().BeEmpty();
    }

    [Fact]
    public void CreateRequest_QueryLocation_ApiKeyIsUrlEscaped() {
        const string awkwardKey = "a b&c=d+/é";

        using var message = NewFactory(apiKey: awkwardKey).CreateRequest(
            TestRequests.Request(apiKeyLocation: ApiKeyLocation.Query));

        message.RequestUri!.Query.Should().Be("?key=" + Uri.EscapeDataString(awkwardKey));
    }

    [Fact]
    public void CreateRequest_QueryLocation_CallerSuppliedKeyParam_IsOverriddenByConfiguredKey() {
        var request = TestRequests.Request(
            apiKeyLocation: ApiKeyLocation.Query,
            query: Dict(("key", "caller-key"), ("a", "1")));

        using var message = NewFactory().CreateRequest(request);

        TestRequests.QueryPairs(message.RequestUri!).Should().BeEquivalentTo("a=1", $"key={ApiKey}");
    }

    [Fact]
    public void CreateRequest_HeaderLocation_CallerSuppliedKeyHeader_IsOverriddenByConfiguredKey() {
        var request = TestRequests.Request(
            apiKeyLocation: ApiKeyLocation.Header,
            headers: Dict(("X-Goog-Api-Key", "caller-key")));

        using var message = NewFactory().CreateRequest(request);

        message.Headers.GetValues("X-Goog-Api-Key").Should().Equal(ApiKey);
    }

    [Fact]
    public void CreateRequest_HeaderLocation_CallerKeyHeaderWithDifferentCasing_IsOverriddenByConfiguredKey() {
        var request = TestRequests.Request(
            apiKeyLocation: ApiKeyLocation.Header,
            headers: Dict(("x-goog-api-key", "caller-key")));

        using var message = NewFactory().CreateRequest(request);

        message.Headers.GetValues("X-Goog-Api-Key").Should().Equal(ApiKey);
    }

    // -------------------- STATE ISOLATION --------------------
    [Fact]
    public void CreateRequest_PerRequestOverrides_DoNotLeakIntoLaterRequests() {
        var factory = NewFactory();
        var noisy = TestRequests.Request(
            method: HttpMethod.Post,
            headers: Dict(("Accept-Language", "fr-FR"), ("X-Extra", "1")),
            query: Dict(("extra", "1")),
            payload: new { A = 1 },
            apiKeyLocation: ApiKeyLocation.Query);

        using var first = factory.CreateRequest(noisy);
        using var second = factory.CreateRequest(TestRequests.Request());

        second.Method.Should().Be(HttpMethod.Get);
        second.Content.Should().BeNull();
        second.RequestUri!.Query.Should().BeEmpty();
        second.Headers.Select(h => h.Key).Should().BeEquivalentTo("Accept-Language", "X-Goog-Api-Key");
        second.Headers.GetValues("Accept-Language").Should().Equal("en-US");
    }

    [Fact]
    public void CreateRequest_ConcurrentCalls_DoNotShareStateBetweenRequests() {
        var factory = NewFactory();

        Parallel.For(0, 200, i => {
            var useQuery = i % 2 == 0;
            var id = i.ToString();
            var request = TestRequests.Request(
                headers: Dict(("X-Id", id)),
                query: Dict(("id", id)),
                apiKeyLocation: useQuery ? ApiKeyLocation.Query : ApiKeyLocation.Header);

            using var message = factory.CreateRequest(request);

            message.Headers.GetValues("X-Id").Should().Equal(id);
            message.Headers.Contains("X-Goog-Api-Key").Should().Be(!useQuery);
            TestRequests.QueryPairs(message.RequestUri!).Should().Contain($"id={id}");
            TestRequests.QueryPairs(message.RequestUri!).Contains($"key={ApiKey}").Should().Be(useQuery);
        });
    }
}
