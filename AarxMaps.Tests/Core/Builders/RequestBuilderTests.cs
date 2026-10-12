using System.Text.Json;
using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Internal.Builders;
using FluentAssertions;

namespace AarxMaps.Tests.Core.Builders;

[Trait("Package", "Core")]
[Trait("Module", "Builders")]
[Trait("Type", "RequestBuilder")]
public class RequestBuilderTests {
    private static readonly Uri Base = new("https://api.test/");

    private static RequestBuilder NewBuilder() => new RequestBuilder().SetPath(Base, "v1/things");

    private static Dictionary<string, string> Headers(params (string Key, string Value)[] pairs)
        => pairs.ToDictionary(p => p.Key, p => p.Value);

    // -------------------- DEFAULTS --------------------
    [Fact]
    public void Build_PathNotSet_ThrowsMapsInvalidRequestException() {
        var builder = new RequestBuilder().SetMethod(HttpMethod.Post);
        var act = () => builder.Build();
        act.Should().Throw<MapsInvalidRequestException>();
    }

    [Fact]
    public void Build_OnlyPathSet_ProducesPlainGetWithoutContentOrHeaders() {
        using var request = NewBuilder().Build();

        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri.Should().Be(new Uri("https://api.test/v1/things"));
        request.Content.Should().BeNull();
        request.Headers.Should().BeEmpty();
    }

    // -------------------- METHOD --------------------
    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public void SetMethod_AnyMethod_IsReflectedOnRequest(string method) {
        using var request = NewBuilder().SetMethod(new HttpMethod(method)).Build();
        request.Method.Should().Be(new HttpMethod(method));
    }

    [Fact]
    public void SetMethod_CalledTwice_LastCallWins() {
        using var request = NewBuilder().SetMethod(HttpMethod.Post).SetMethod(HttpMethod.Put).Build();
        request.Method.Should().Be(HttpMethod.Put);
    }

    // -------------------- URI --------------------
    [Fact]
    public void Build_PathAndQuery_AreCombinedIntoRequestUri() {
        using var request = new RequestBuilder()
            .SetPath(new Uri("https://api.test/v1/"), "/things")
            .AddQuery(new Dictionary<string, string> { ["q"] = "a b" })
            .Build();

        request.RequestUri!.AbsoluteUri.Should().Be("https://api.test/v1/things?q=a%20b");
    }

    // -------------------- HEADERS --------------------
    [Fact]
    public void AddHeaders_MultipleHeaders_AreAllAddedToRequest() {
        using var request = NewBuilder()
            .AddHeaders(Headers(("X-Goog-FieldMask", "routes.duration,routes.distanceMeters"), ("Accept-Language", "en-US")))
            .Build();

        request.Headers.GetValues("X-Goog-FieldMask").Should().Equal("routes.duration,routes.distanceMeters");
        request.Headers.GetValues("Accept-Language").Should().Equal("en-US");
    }

    [Fact]
    public void AddHeaders_AcrossCalls_Accumulates() {
        using var request = NewBuilder()
            .AddHeaders(Headers(("X-One", "1")))
            .AddHeaders(Headers(("X-Two", "2")))
            .Build();

        request.Headers.GetValues("X-One").Should().Equal("1");
        request.Headers.GetValues("X-Two").Should().Equal("2");
    }

    [Fact]
    public void AddHeaders_SameKeyAcrossCalls_LastValueWins() {
        using var request = NewBuilder()
            .AddHeaders(Headers(("X-Test", "a")))
            .AddHeaders(Headers(("X-Test", "b")))
            .Build();

        request.Headers.GetValues("X-Test").Should().Equal("b");
    }
    
    [Fact]
    public void AddHeaders_SameKeyDifferentCase_LastValueWins() {
        using var request = NewBuilder()
            .AddHeaders(Headers(("x-test", "a")))
            .AddHeaders(Headers(("X-Test", "b")))
            .Build();

        request.Headers.GetValues("X-Test").Should().Equal("b");
    }

    [Fact]
    public async Task Build_ContentTypeHeaderWithPayload_JsonContentTypeStillWins() {
        using var request = NewBuilder()
            .AddHeaders(Headers(("Content-Type", "text/plain")))
            .SetJsonPayload(new { A = 1 })
            .Build();

        request.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
        (await request.Content.ReadAsStringAsync()).Should().Be("""{"a":1}""");
    }

    // -------------------- PAYLOAD --------------------
    [Fact]
    public void Build_WithPayload_SetsJsonUtf8Content() {
        using var request = NewBuilder().SetMethod(HttpMethod.Post).SetJsonPayload(new { Name = "x" }).Build();

        request.Content.Should().NotBeNull();
        request.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
        request.Content.Headers.ContentType.CharSet.Should().Be("utf-8");
    }

    /// <summary>
    /// Pins the IMPLICIT serializer options: JsonContent.Create(object) uses System.Net.Http.Json's own
    /// Web defaults (camelCase, compact) — NOT MapsJsonSerializer. See the note in the review: if
    /// MapsJsonSerializer configures different options (enum converters, null handling), request bodies
    /// and the cache-key payload can diverge. Verify this assertion on first run.
    /// </summary>
    [Fact]
    public async Task Build_WithPayload_SerializesRuntimeTypeWithCamelCaseNames() {
        object payload = new { Name = "x", Count = 2, Nested = new { InnerValue = true } };

        using var request = NewBuilder().SetJsonPayload(payload).Build();

        (await request.Content!.ReadAsStringAsync())
            .Should().Be("""{"name":"x","count":2,"nested":{"innerValue":true}}""");
    }

    [Fact]
    public async Task Build_WithPayload_PreservesNonAsciiAsValidJson() {
        using var request = NewBuilder().SetJsonPayload(new { Text = "Medellín ñ 東京" }).Build();

        var json = await request.Content!.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("text").GetString().Should().Be("Medellín ñ 東京");
    }

    [Fact]
    public async Task SetJsonPayload_CalledTwice_LastCallWins() {
        using var request = NewBuilder().SetJsonPayload(new { A = 1 }).SetJsonPayload(new { B = 2 }).Build();

        (await request.Content!.ReadAsStringAsync()).Should().Be("""{"b":2}""");
    }

    // -------------------- BUILDER BEHAVIOUR --------------------
    [Fact]
    public async Task Build_CalledTwice_ReturnsIndependentRequests() {
        var builder = NewBuilder()
            .SetMethod(HttpMethod.Post)
            .AddHeaders(Headers(("X-Test", "a")))
            .SetJsonPayload(new { A = 1 });

        var first = builder.Build();
        var second = builder.Build();
        first.Dispose();

        second.Should().NotBeSameAs(first);
        second.Content.Should().NotBeSameAs(first.Content);
        (await second.Content!.ReadAsStringAsync()).Should().Be("""{"a":1}""");
        second.Headers.GetValues("X-Test").Should().Equal("a");
        second.Dispose();
    }

    [Fact]
    public void ConfigMethods_ReturnSameInstance_ForFluentChaining() {
        var builder = new RequestBuilder();
        var empty = new Dictionary<string, string>();

        builder.SetMethod(HttpMethod.Get).Should().BeSameAs(builder);
        builder.SetPath(Base, "x").Should().BeSameAs(builder);
        builder.AddHeaders(empty).Should().BeSameAs(builder);
        builder.AddQuery(empty).Should().BeSameAs(builder);
        builder.SetJsonPayload(new { }).Should().BeSameAs(builder);
    }
}