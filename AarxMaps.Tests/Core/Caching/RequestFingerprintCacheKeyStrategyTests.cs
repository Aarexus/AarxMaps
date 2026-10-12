using System.Security.Cryptography;
using System.Text;
using AarxMaps.Core.Configuration;
using AarxMaps.Core.Enums;
using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Internal.Caching.KeyStrategy;
using AarxMaps.Core.Models;
using AarxMaps.Tests.Core.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace AarxMaps.Tests.Core.Caching;

[Trait("Package", "Core")]
[Trait("Module", "Caching")]
[Trait("Type", "RequestFingerprintCacheKeyStrategy")]
public class RequestFingerprintCacheKeyStrategyTests {
    private const string Prefix = "aarx";

    // ---- payload types for failure cases ----
    public sealed class ThrowingPayload { public int Boom => throw new InvalidOperationException("boom"); }

    /// <summary>
    /// Failure injection that does NOT depend on MapsJsonOptions: enumerating the dictionary throws.
    /// WriteMap enumerates Headers and Query, so any exception type can be injected deterministically.
    /// </summary>
    private sealed class ThrowingDictionary(Exception toThrow) : IDictionary<string, string> {
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => throw toThrow;
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => throw toThrow;
        public string this[string key] { get => throw toThrow; set => throw toThrow; }
        public ICollection<string> Keys => throw toThrow;
        public ICollection<string> Values => throw toThrow;
        public int Count => throw toThrow;
        public bool IsReadOnly => true;
        public void Add(string key, string value) => throw toThrow;
        public void Add(KeyValuePair<string, string> item) => throw toThrow;
        public void Clear() => throw toThrow;
        public bool Contains(KeyValuePair<string, string> item) => throw toThrow;
        public bool ContainsKey(string key) => throw toThrow;
        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => throw toThrow;
        public bool Remove(string key) => throw toThrow;
        public bool Remove(KeyValuePair<string, string> item) => throw toThrow;
        public bool TryGetValue(string key, out string value) => throw toThrow;
    }

    // ---- helpers ----
    private static RequestFingerprintCacheKeyStrategy NewStrategy(string prefix = Prefix)
        => new(Options.Create(new MapsCachingOptions { Prefix = prefix }));
    private static string Key<T>(MapsApiRequest request, string prefix = Prefix)
        => NewStrategy(prefix).GenerateCacheKey<T>(request);
    private static string Key(MapsApiRequest request) => Key<string>(request);
    private static Dictionary<string, string> Dict(params (string Key, string Value)[] pairs)
        => pairs.ToDictionary(p => p.Key, p => p.Value);
    private static string HashPart(string key) => key[(key.LastIndexOf(':') + 1)..];

    private static MapsApiRequest Req(
        HttpMethod? method = null, Uri? baseUrl = null, string endpoint = "v1/things",
        IDictionary<string, string>? headers = null, IDictionary<string, string>? query = null,
        object? payload = null, ApiKeyLocation location = ApiKeyLocation.Header
    ) => TestRequests.Request(method, baseUrl, endpoint, headers, query, payload, location);

    // ===================== FORMAT & CANONICAL FORM =====================
    [Fact]
    public void GenerateCacheKey_Always_MatchesPrefixVersionHashFormat()
        => Key(Req()).Should().MatchRegex("^aarx:v1:[0-9A-F]{64}$");

    [Theory]
    [InlineData("aarx")]
    [InlineData("my-app:prod")]
    public void GenerateCacheKey_Prefix_IsUsedVerbatim_AndDoesNotAffectTheHash(string prefix) {
        var withPrefix = Key<string>(Req(), prefix);
        var baseline = Key(Req());

        withPrefix.Should().StartWith($"{prefix}:v1:");
        HashPart(withPrefix).Should().Be(HashPart(baseline));
    }

    [Fact]
    public void GenerateCacheKey_FullRequest_HashesTheExpectedCanonicalJson() {
        var request = Req(
            method: HttpMethod.Post,
            baseUrl: new Uri("https://maps.test/"),
            endpoint: "  v1/things  ",
            headers: Dict(("Accept-Language", "en-US"), ("X-Goog-FieldMask", " routes.duration "), ("Authorization", "ignored")),
            query: Dict(("b", "2"), ("a", "1"), ("key", "SECRET")),
            payload: new Dictionary<string, object> { ["b"] = 2, ["a"] = new[] { 3, 1 } });

        const string canonical =
            """{"type":"System.String","base":"https://maps.test/","endpoint":"v1/things","method":"POST","headers":{"accept-language":"en-US","x-goog-fieldmask":"routes.duration"},"query":{"a":"1","b":"2"},"payload":{"a":[3,1],"b":2}}""";
        var expected = $"{Prefix}:v1:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)))}";

        Key(request).Should().Be(expected);
    }

    [Fact]
    public void GenerateCacheKey_MinimalRequest_HashesTheExpectedCanonicalJson() {
        var request = Req(baseUrl: new Uri("https://maps.test/"), endpoint: "v1/things");

        const string canonical =
            """{"type":"System.String","base":"https://maps.test/","endpoint":"v1/things","method":"GET","headers":{},"query":{},"payload":null}""";
        var expected = $"{Prefix}:v1:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)))}";

        Key(request).Should().Be(expected);
    }

    // ===================== DETERMINISM =====================
    [Fact]
    public void GenerateCacheKey_SameRequestTwiceOrFromDifferentInstances_ReturnsSameKey() {
        var request = Req(headers: Dict(("X-Goog-FieldMask", "a")), query: Dict(("q", "1")), payload: new { A = 1 });

        var first = NewStrategy().GenerateCacheKey<string>(request);

        NewStrategy().GenerateCacheKey<string>(request).Should().Be(first);
        NewStrategy().GenerateCacheKey<string>(request).Should().Be(first);
    }

    [Fact]
    public void GenerateCacheKey_ConcurrentCalls_AllReturnTheSameKey() {
        var strategy = NewStrategy();
        var request = Req(query: Dict(("q", "1")), payload: new { A = 1, B = new[] { 1, 2, 3 } });
        var expected = strategy.GenerateCacheKey<string>(request);

        Parallel.For(0, 200, _ => strategy.GenerateCacheKey<string>(request).Should().Be(expected));
    }

    // ===================== TYPE / URL / METHOD / ENDPOINT =====================
    [Fact]
    public void GenerateCacheKey_DifferentResponseType_ReturnsDifferentKey() {
        var request = Req();

        Key<string>(request).Should().NotBe(Key<int>(request));
        Key<List<int>>(request).Should().NotBe(Key<List<long>>(request));
    }

    [Fact]
    public void GenerateCacheKey_DifferentBaseUrl_ReturnsDifferentKey()
        => Key(Req(baseUrl: new Uri("https://a.test/")))
            .Should().NotBe(Key(Req(baseUrl: new Uri("https://b.test/"))));

    [Fact]
    public void GenerateCacheKey_BaseUrlWithAndWithoutTrailingSlashOnHost_ReturnsSameKey()
        => Key(Req(baseUrl: new Uri("https://a.test")))
            .Should().Be(Key(Req(baseUrl: new Uri("https://a.test/"))));

    [Fact]
    public void GenerateCacheKey_DifferentEndpoint_ReturnsDifferentKey()
        => Key(Req(endpoint: "v1/a"))
            .Should().NotBe(Key(Req(endpoint: "v1/b")));

    [Fact]
    public void GenerateCacheKey_EndpointWithSurroundingWhitespace_ReturnsSameKeyAsTrimmed()
        => Key(Req(endpoint: "  v1/a \t"))
            .Should().Be(Key(Req(endpoint: "v1/a")));

    [Fact]
    public void GenerateCacheKey_DifferentMethod_ReturnsDifferentKey()
        => Key(Req(method: HttpMethod.Get))
            .Should().NotBe(Key(Req(method: HttpMethod.Post)));

    [Fact]
    public void GenerateCacheKey_MethodCasing_DoesNotMatter()
        => Key(Req(method: new HttpMethod("post")))
            .Should().Be(Key(Req(method: HttpMethod.Post)));

    [Fact]
    public void GenerateCacheKey_ApiKeyLocation_DoesNotAffectKey()
        => Key(Req(location: ApiKeyLocation.Header))
            .Should().Be(Key(Req(location: ApiKeyLocation.Query)));

    // ===================== HEADERS =====================
    [Fact]
    public void GenerateCacheKey_HeadersInDifferentOrder_ReturnsSameKey() {
        var a = Req(headers: Dict(("Accept-Language", "es-CO"), ("X-Goog-FieldMask", "x"), ("Content-Type", "application/json")));
        var b = Req(headers: Dict(("Content-Type", "application/json"), ("X-Goog-FieldMask", "x"), ("Accept-Language", "es-CO")));

        Key(a).Should().Be(Key(b));
    }

    [Fact]
    public void GenerateCacheKey_HeaderNamesDifferingOnlyByCase_ReturnSameKey() {
        var a = Req(headers: Dict(("Accept-Language", "es-CO"), ("X-Goog-FieldMask", "x")));
        var b = Req(headers: Dict(("accept-language", "es-CO"), ("X-GOOG-FIELDMASK", "x")));

        Key(a).Should().Be(Key(b));
    }

    [Theory]
    [InlineData("Authorization", "Bearer token")]
    [InlineData("X-Goog-Api-Key", "secret")]
    [InlineData("Accept", "application/json")]
    [InlineData("X-Custom", "1")]
    public void GenerateCacheKey_NonAllowListedHeader_DoesNotAffectKey(string name, string value) {
        var baseline = Key(Req(headers: Dict(("X-Goog-FieldMask", "x"))));

        Key(Req(headers: Dict(("X-Goog-FieldMask", "x"), (name, value)))).Should().Be(baseline);
    }

    [Theory]
    [InlineData("Accept-Language", "es-CO")]
    [InlineData("Content-Type", "application/json")]
    [InlineData("X-Goog-FieldMask", "routes.duration")]
    public void GenerateCacheKey_AllowListedHeaderPresent_ChangesKey(string name, string value)
        => Key(Req(headers: Dict((name, value)))).Should().NotBe(Key(Req()));

    [Theory]
    [InlineData("Accept-Language", "es-CO", "en-US")]
    [InlineData("Content-Type", "application/json", "application/xml")]
    [InlineData("X-Goog-FieldMask", "routes.duration", "routes.distanceMeters")]
    public void GenerateCacheKey_AllowListedHeaderValueDiffers_ChangesKey(string name, string v1, string v2)
        => Key(Req(headers: Dict((name, v1))))
            .Should().NotBe(Key(Req(headers: Dict((name, v2)))));

    [Fact]
    public void GenerateCacheKey_HeaderValueCaseDiffers_ChangesKey()
        => Key(Req(headers: Dict(("X-Goog-FieldMask", "Routes.Duration"))))
            .Should().NotBe(Key(Req(headers: Dict(("X-Goog-FieldMask", "routes.duration")))));

    [Fact]
    public void GenerateCacheKey_HeaderValueWithSurroundingWhitespace_ReturnsSameKey()
        => Key(Req(headers: Dict(("X-Goog-FieldMask", "  a,b \t"))))
            .Should().Be(Key(Req(headers: Dict(("X-Goog-FieldMask", "a,b")))));

    [Fact]
    public void GenerateCacheKey_NullEmptyAndWhitespaceHeaderValue_AreEquivalent() {
        var empty = Key(Req(headers: Dict(("X-Goog-FieldMask", ""))));

        Key(Req(headers: Dict(("X-Goog-FieldMask", null!)))).Should().Be(empty);
        Key(Req(headers: Dict(("X-Goog-FieldMask", "   ")))).Should().Be(empty);
    }

    // ===================== QUERY =====================
    [Fact]
    public void GenerateCacheKey_QueryInDifferentOrder_ReturnsSameKey()
        => Key(Req(query: Dict(("a", "1"), ("b", "2"), ("c", "3"))))
            .Should().Be(Key(Req(query: Dict(("c", "3"), ("a", "1"), ("b", "2")))));

    [Theory]
    [InlineData("key")]
    [InlineData("KEY")]
    [InlineData("Key")]
    public void GenerateCacheKey_ApiKeyQueryParameter_DoesNotAffectKey(string name) {
        var baseline = Key(Req(query: Dict(("q", "1"))));

        Key(Req(query: Dict(("q", "1"), (name, "secret-1")))).Should().Be(baseline);
        Key(Req(query: Dict(("q", "1"), (name, "secret-2")))).Should().Be(baseline);
    }

    [Theory]
    [InlineData("keys")]
    [InlineData("apikey")]
    [InlineData("key2")]
    public void GenerateCacheKey_QueryParameterMerelyContainingKey_StillAffectsKey(string name)
        => Key(Req(query: Dict((name, "1")))).Should().NotBe(Key(Req()));

    [Fact]
    public void GenerateCacheKey_QueryNamesDifferingByCase_ReturnDifferentKeys()
        => Key(Req(query: Dict(("q", "1"))))
            .Should().NotBe(Key(Req(query: Dict(("Q", "1")))));

    [Fact]
    public void GenerateCacheKey_QueryValuesDifferingByCase_ReturnDifferentKeys()
        => Key(Req(query: Dict(("q", "Pizza"))))
            .Should().NotBe(Key(Req(query: Dict(("q", "pizza")))));

    [Fact]
    public void GenerateCacheKey_DifferentQueryValue_ReturnsDifferentKey()
        => Key(Req(query: Dict(("q", "1"))))
            .Should().NotBe(Key(Req(query: Dict(("q", "2")))));

    [Fact]
    public void GenerateCacheKey_QueryValuesDifferingOnlyByWhitespace_ReturnDifferentKeys()
        => Key(Req(query: Dict(("input", "pizza"))))
            .Should().NotBe(Key(Req(query: Dict(("input", "pizza ")))));

    [Fact]
    public void GenerateCacheKey_QueryNamesDifferingOnlyByWhitespace_ReturnDifferentKeys()
        => Key(Req(query: Dict(("q", "1"))))
            .Should().NotBe(Key(Req(query: Dict((" q", "1")))));

    // ===================== NULL vs EMPTY =====================
    [Fact]
    public void GenerateCacheKey_NullAndEmptyHeaders_ReturnSameKey()
        => Key(Req(headers: null)).Should().Be(Key(Req(headers: new Dictionary<string, string>())));

    [Fact]
    public void GenerateCacheKey_NullAndEmptyQuery_ReturnSameKey()
        => Key(Req(query: null)).Should().Be(Key(Req(query: new Dictionary<string, string>())));

    [Fact]
    public void GenerateCacheKey_OnlyExcludedHeadersAndQuery_EqualsNoHeadersNoQuery() {
        var noisy = Req(headers: Dict(("Authorization", "x")), query: Dict(("key", "secret")));

        Key(noisy).Should().Be(Key(Req()));
    }

    // ===================== PAYLOAD =====================
    [Fact]
    public void GenerateCacheKey_PayloadObjectPropertiesInDifferentOrder_ReturnSameKey() {
        var a = Req(payload: new { A = 1, B = new { X = 1, Y = 2 }, C = "z" });
        var b = Req(payload: new { C = "z", B = new { Y = 2, X = 1 }, A = 1 });

        Key(a).Should().Be(Key(b));
    }

    [Fact]
    public void GenerateCacheKey_PayloadDictionaryInsertionOrderDiffers_ReturnsSameKey() {
        var a = Req(payload: new Dictionary<string, object> { ["a"] = 1, ["b"] = 2, ["c"] = new Dictionary<string, object> { ["x"] = 1, ["y"] = 2 } });
        var b = Req(payload: new Dictionary<string, object> { ["c"] = new Dictionary<string, object> { ["y"] = 2, ["x"] = 1 }, ["b"] = 2, ["a"] = 1 });

        Key(a).Should().Be(Key(b));
    }

    [Fact]
    public void GenerateCacheKey_PayloadArrayOrderDiffers_ReturnsDifferentKey()
        => Key(Req(payload: new { Ids = new[] { 1, 2, 3 } }))
            .Should().NotBe(Key(Req(payload: new { Ids = new[] { 3, 2, 1 } })));

    [Fact]
    public void GenerateCacheKey_PayloadArrayOfObjects_KeepsElementOrderButSortsInsideElements() {
        var a = Req(payload: new { Items = new object[] { new { P = 1, Q = 2 }, new { P = 3, Q = 4 } } });
        var same = Req(payload: new { Items = new object[] { new { Q = 2, P = 1 }, new { Q = 4, P = 3 } } });
        var swapped = Req(payload: new { Items = new object[] { new { P = 3, Q = 4 }, new { P = 1, Q = 2 } } });

        Key(a).Should().Be(Key(same));
        Key(a).Should().NotBe(Key(swapped));
    }

    [Fact]
    public void GenerateCacheKey_PayloadValueDiffers_ReturnsDifferentKey() {
        Key(Req(payload: new { A = 1 })).Should().NotBe(Key(Req(payload: new { A = 2 })));
        Key(Req(payload: new { A = "x" })).Should().NotBe(Key(Req(payload: new { A = "X" })));
        Key(Req(payload: new { A = true })).Should().NotBe(Key(Req(payload: new { A = false })));
    }

    [Fact]
    public void GenerateCacheKey_NullPayloadVersusEmptyObject_ReturnDifferentKeys()
        => Key(Req(payload: null)).Should().NotBe(Key(Req(payload: new Dictionary<string, object>())));

    [Fact]
    public void GenerateCacheKey_PayloadWithNonAsciiText_IsDeterministicAndDistinct() {
        var a = Key(Req(payload: new { Text = "Medellín 東京 😀" }));

        a.Should().Be(Key(Req(payload: new { Text = "Medellín 東京 😀" })));
        a.Should().NotBe(Key(Req(payload: new { Text = "Medellin 東京 😀" })));
    }

    [Fact]
    public void GenerateCacheKey_LargePayload_StillProducesWellFormedKey() {
        var payload = new { Items = Enumerable.Range(0, 20_000).Select(i => new { Id = i, Name = "n" + i }).ToArray() };

        Key(Req(payload: payload)).Should().MatchRegex("^aarx:v1:[0-9A-F]{64}$");
    }

    // ===================== EVERYTHING TOGETHER =====================
    [Fact]
    public void GenerateCacheKey_EachRequestComponentChange_ProducesAPairwiseDistinctKey() {
        var baseline = Req(
            headers: Dict(("X-Goog-FieldMask", "a")), query: Dict(("q", "1")), payload: new { A = 1 });

        var variants = new[] {
            baseline,
            Req(method: HttpMethod.Put, headers: Dict(("X-Goog-FieldMask", "a")), query: Dict(("q", "1")), payload: new { A = 1 }),
            Req(baseUrl: new Uri("https://other.test/"), headers: Dict(("X-Goog-FieldMask", "a")), query: Dict(("q", "1")), payload: new { A = 1 }),
            Req(endpoint: "v1/other", headers: Dict(("X-Goog-FieldMask", "a")), query: Dict(("q", "1")), payload: new { A = 1 }),
            Req(headers: Dict(("X-Goog-FieldMask", "b")), query: Dict(("q", "1")), payload: new { A = 1 }),
            Req(headers: Dict(("X-Goog-FieldMask", "a")), query: Dict(("q", "2")), payload: new { A = 1 }),
            Req(headers: Dict(("X-Goog-FieldMask", "a")), query: Dict(("q", "1")), payload: new { A = 2 }),
        };

        variants.Select(v => Key(v)).Distinct().Should().HaveCount(variants.Length);
    }

    // ===================== FAILURES =====================
    [Fact]
    public void GenerateCacheKey_PayloadSerializationThrows_ThrowsMapsCacheExceptionWithInner() {
        var strategy = NewStrategy();
        var act = () => strategy.GenerateCacheKey<string>(Req(payload: new ThrowingPayload()));

        act.Should().ThrowExactly<MapsCacheException>()
            .WithMessage("Cache key generation failed.")
            .Which.InnerException.Should().NotBeNull();
    }

    [Fact]
    public void GenerateCacheKey_HeadersEnumerationThrows_ThrowsMapsCacheExceptionWithInner() {
        var boom = new InvalidOperationException("boom");
        var strategy = NewStrategy();

        var act = () => strategy.GenerateCacheKey<string>(Req(headers: new ThrowingDictionary(boom)));

        act.Should().ThrowExactly<MapsCacheException>()
            .WithMessage("Cache key generation failed.")
            .Which.InnerException.Should().BeSameAs(boom);
    }

    [Fact]
    public void GenerateCacheKey_QueryEnumerationThrows_ThrowsMapsCacheExceptionWithInner() {
        var boom = new FormatException("boom");
        var strategy = NewStrategy();

        var act = () => strategy.GenerateCacheKey<string>(Req(query: new ThrowingDictionary(boom)));

        act.Should().ThrowExactly<MapsCacheException>()
            .Which.InnerException.Should().BeSameAs(boom);
    }

    [Fact]
    public void GenerateCacheKey_OperationCanceledException_IsNotWrapped() {
        var cancelled = new OperationCanceledException("cancelled");
        var strategy = NewStrategy();

        var act = () => strategy.GenerateCacheKey<string>(Req(headers: new ThrowingDictionary(cancelled)));

        act.Should().Throw<OperationCanceledException>().Which.Should().BeSameAs(cancelled);
    }

    [Fact]
    public void GenerateCacheKey_MapsCacheExceptionRaisedInside_IsRethrownAsIsNotDoubleWrapped() {
        var original = new MapsCacheException("original failure");
        var strategy = NewStrategy();

        var act = () => strategy.GenerateCacheKey<string>(Req(query: new ThrowingDictionary(original)));

        act.Should().ThrowExactly<MapsCacheException>().Which.Should().BeSameAs(original);
    }
}