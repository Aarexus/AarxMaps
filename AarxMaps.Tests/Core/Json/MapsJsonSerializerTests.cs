using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Internal.Builders;
using AarxMaps.Core.Internal.Json;
using AarxMaps.Tests.Core.Helpers;
using FluentAssertions;

namespace AarxMaps.Tests.Core.Json;

[Trait("Package", "Core")]
[Trait("Module", "Json")]
[Trait("Type", "MapsJsonSerializer")]
public class MapsJsonSerializerTests {
    public sealed class Sample {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
    }

    public enum TravelMode { Drive, Walk }

    public sealed class RichSample {
        public int Id { get; set; }
        public string? Name { get; set; }
        public List<string> Tags { get; set; } = [];
        public TravelMode Mode { get; set; }
        public DateTimeOffset Created { get; set; }
        public double? Distance { get; set; }
        public Dictionary<string, int> Counts { get; set; } = [];
    }

    public class BaseDto { [JsonPropertyName("base")] public int Base { get; set; } }
    public sealed class DerivedDto : BaseDto { [JsonPropertyName("extra")] public string Extra { get; set; } = ""; }

    public sealed class Payload {
        public TravelMode Mode { get; set; }
        public string? Note { get; set; }
        public int OriginCount { get; set; }
    }

    private static MemoryStream Utf8(string json) => new(Encoding.UTF8.GetBytes(json));

    private static string SampleArray(int count)
        => "[" + string.Join(",", Enumerable.Range(1, count).Select(i => $$"""{"id":{{i}},"name":"item {{i}}"}""")) + "]";

    // ===================== Serialize =====================
    [Fact]
    public void Serialize_DerivedInstanceTypedAsObject_UsesRuntimeType() {
        object value = new DerivedDto { Base = 1, Extra = "x" };

        using var doc = JsonDocument.Parse(MapsJsonSerializer.Serialize(value));

        doc.RootElement.GetProperty("base").GetInt32().Should().Be(1);
        doc.RootElement.GetProperty("extra").GetString().Should().Be("x");
    }

    [Fact]
    public void Serialize_Value_ProducesParsableJson() {
        var json = MapsJsonSerializer.Serialize(new Sample { Id = 5, Name = "é 東京 \"quoted\"" });

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("name").GetString().Should().Be("é 東京 \"quoted\"");
    }

    [Fact]
    public void Serialize_ThenDeserialize_RoundTripsRichObject() {
        var original = new RichSample {
            Id = 7,
            Name = "route",
            Tags = ["a", "b"],
            Mode = TravelMode.Walk,
            Created = new DateTimeOffset(2026, 3, 4, 5, 6, 7, TimeSpan.FromHours(-5)),
            Distance = 12.5,
            Counts = new() { ["x"] = 1, ["y"] = 2 }
        };

        var copy = MapsJsonSerializer.Deserialize<RichSample>(MapsJsonSerializer.Serialize(original));

        copy.Should().BeEquivalentTo(original);
    }

    [Fact]
    public async Task Serialize_Payload_MatchesBodyProducedByRequestBuilder() {
        var payload = new Payload { Mode = TravelMode.Walk, Note = null, OriginCount = 3 };

        using var request = new RequestBuilder()
            .SetPath(new Uri("https://maps.test/"), "v1/x")
            .SetJsonPayload(payload)
            .Build();

        var body = await request.Content!.ReadAsStringAsync();

        body.Should().Be(MapsJsonSerializer.Serialize(payload));
    }

    // ===================== Deserialize<T> =====================
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\r\n\t  ")]
    public void Deserialize_NullEmptyOrWhitespace_ReturnsDefault(string? json) {
        MapsJsonSerializer.Deserialize<Sample>(json).Should().BeNull();
        MapsJsonSerializer.Deserialize<int>(json).Should().Be(0);
    }

    [Fact]
    public void Deserialize_ValidJson_ReturnsObject() {
        var result = MapsJsonSerializer.Deserialize<Sample>("""{"id":3,"name":"x"}""");
        result.Should().BeEquivalentTo(new Sample { Id = 3, Name = "x" });
    }

    [Fact]
    public void Deserialize_JsonNullLiteral_ReturnsNullForReferenceType() {
        MapsJsonSerializer.Deserialize<Sample>("null").Should().BeNull();
    }

    [Fact]
    public void Deserialize_JsonNullLiteralForValueType_ThrowsMapsApiException() {
        var act = () => MapsJsonSerializer.Deserialize<int>("null");
        act.Should().Throw<MapsApiException>().WithInnerException<JsonException>();
    }

    [Theory]
    [InlineData("{")]
    [InlineData("""{"id":1""")]
    [InlineData("""{"id":1,}""")]
    [InlineData("not json")]
    [InlineData("""{"id":1} trailing""")]
    [InlineData("""{"id":"abc"}""")]   // type mismatch
    [InlineData("[1,2,3]")]            // wrong shape for Sample
    public void Deserialize_MalformedOrMismatchedJson_ThrowsMapsApiExceptionWrappingJsonException(string json) {
        var act = () => MapsJsonSerializer.Deserialize<Sample>(json);

        act.Should().ThrowExactly<MapsApiException>()
            .WithMessage("Response could not be deserialized to Sample.")
            .WithInnerException<JsonException>();
    }

    // ===================== DeserializeStreamAsync<T> =====================
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(50)]
    [InlineData(10_000)] // ≈ 300 KB: crosses STJ's internal buffer boundaries
    public async Task DeserializeStreamAsync_ArrayOfNElements_YieldsAllInOrder(int count) {
        await using var stream = Utf8(SampleArray(count));

        var items = await MapsJsonSerializer.DeserializeStreamAsync<Sample>(stream, default).CollectAsync();

        items.Select(i => i.Id).Should().Equal(Enumerable.Range(1, count));
        items.Select(i => i.Name).Should().Equal(Enumerable.Range(1, count).Select(i => $"item {i}"));
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("  [ \n ]  ")]
    public async Task DeserializeStreamAsync_EmptyArray_YieldsNothing(string json) {
        await using var stream = Utf8(json);
        var items = await MapsJsonSerializer.DeserializeStreamAsync<Sample>(stream, default).CollectAsync();
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task DeserializeStreamAsync_NullElements_AreSkipped() {
        await using var stream = Utf8("""[null,{"id":1},null,null,{"id":2},null]""");
        var items = await MapsJsonSerializer.DeserializeStreamAsync<Sample>(stream, default).CollectAsync();
        items.Select(i => i.Id).Should().Equal(1, 2);
    }

    [Fact]
    public async Task DeserializeStreamAsync_ArrayOfOnlyNulls_YieldsNothing() {
        await using var stream = Utf8("[null,null]");
        var items = await MapsJsonSerializer.DeserializeStreamAsync<string>(stream, default).CollectAsync();
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task DeserializeStreamAsync_ValueTypeElementWithNull_ThrowsMapsApiException() {
        await using var stream = Utf8("[1,null,2]"); // null can't become an int; skipping only applies to nullable T
        var act = () => MapsJsonSerializer.DeserializeStreamAsync<int>(stream, default).CollectAsync();
        await act.Should().ThrowAsync<MapsApiException>();
    }

    [Fact]
    public async Task DeserializeStreamAsync_ElementsSplitAcrossTinyReads_AreReassembled() {
        await using var stream = ChunkedStream.Split(SampleArray(20), chunkSize: 7);
        var items = await MapsJsonSerializer.DeserializeStreamAsync<Sample>(stream, default).CollectAsync();
        items.Select(i => i.Id).Should().Equal(Enumerable.Range(1, 20));
    }

    // -------------------- errors --------------------
    [Theory]
    [InlineData("[1,2")]                 // truncated
    [InlineData("[1,,2]")]
    [InlineData("[1 2]")]
    [InlineData("""[{"id":1},{"id":]""")]
    [InlineData("""[{"id":"abc"}]""")]   // element type mismatch
    [InlineData("""{"id":1}""")]         // top level is an object, not an array
    [InlineData("\"abc\"")]
    [InlineData("42")]
    [InlineData("")]                     // empty stream (verify: STJ reports this as JsonException)
    public async Task DeserializeStreamAsync_MalformedOrNonArrayPayload_ThrowsMapsApiExceptionWrappingJsonException(string json) {
        await using var stream = Utf8(json);

        var act = () => MapsJsonSerializer.DeserializeStreamAsync<Sample>(stream, default).CollectAsync();

        (await act.Should().ThrowExactlyAsync<MapsApiException>())
            .WithMessage("Stream could not be deserialized to Sample.")
            .WithInnerException<JsonException>();
    }

    [Fact]
    public async Task DeserializeStreamAsync_MalformedDataInLaterChunk_DeliversOnlyAValidPrefixThenThrows() {
        await using var stream = new ChunkedStream(["""[{"id":1},{"id":2},""", "oops]"]);
        var delivered = new List<int>();

        var act = async () =>
        {
            await foreach (var item in MapsJsonSerializer.DeserializeStreamAsync<Sample>(stream, default))
                delivered.Add(item.Id);
        };

        await act.Should().ThrowExactlyAsync<MapsApiException>();
        // STJ may or may not surface the complete elements of the first chunk before the failure;
        // the contract is only "never anything that isn't a correct prefix".
        delivered.Should().BeEquivalentTo(new[] { 1, 2 }.Take(delivered.Count), o => o.WithStrictOrdering());
    }

    [Fact]
    public async Task DeserializeStreamAsync_StreamThrowsIOException_PropagatesIOExceptionUnwrapped() {
        // Layering: only JsonException is mapped here; IOException→MapsApiException happens in SendAndEnumerateAsync.
        await using var stream = new ChunkedStream(["[1,2,"], thenThrow: new IOException("connection reset"));
        var delivered = new List<int>();

        var act = async () =>
        {
            await foreach (var item in MapsJsonSerializer.DeserializeStreamAsync<int>(stream, default))
                delivered.Add(item);
        };

        await act.Should().ThrowAsync<IOException>().WithMessage("connection reset");
        delivered.Should().BeEquivalentTo(new[] { 1, 2 }.Take(delivered.Count), o => o.WithStrictOrdering());
    }

    // -------------------- laziness & ownership --------------------
    [Fact]
    public async Task DeserializeStreamAsync_NotEnumerated_NeverTouchesTheStream() {
        await using var stream = FaultyStream.FromString("[1]", failAfterBytes: 0); // any read would throw

        var create = () => MapsJsonSerializer.DeserializeStreamAsync<int>(stream, default);

        create.Should().NotThrow();
        var enumerate = () => create().CollectAsync();
        await enumerate.Should().ThrowAsync<IOException>(); // proves the stream really is hostile
    }

    [Fact]
    public async Task DeserializeStreamAsync_NullStream_ThrowsArgumentNullExceptionWhenEnumerated() {
        // ArgumentNullException.ThrowIfNull sits inside the iterator, so validation is deferred to the first
        // MoveNextAsync. The assertion enumerates, so it also holds if you later split into eager validation.
        var act = () => MapsJsonSerializer.DeserializeStreamAsync<int>(null!, default).CollectAsync();
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DeserializeStreamAsync_AfterFullEnumeration_DoesNotDisposeCallersStream() {
        var stream = FaultyStream.FromString("[1,2,3]");

        var items = await MapsJsonSerializer.DeserializeStreamAsync<int>(stream, default).CollectAsync();

        items.Should().Equal(1, 2, 3);
        stream.IsDisposed.Should().BeFalse();
    }

    [Fact]
    public async Task DeserializeStreamAsync_AbandonedWithEarlyBreak_DoesNotDisposeCallersStream() {
        var stream = FaultyStream.FromString("[1,2,3]");

        await foreach (var _ in MapsJsonSerializer.DeserializeStreamAsync<int>(stream, default))
            break;

        stream.IsDisposed.Should().BeFalse();
    }

    [Fact]
    public async Task DeserializeStreamAsync_OnError_DoesNotDisposeCallersStream() {
        var stream = FaultyStream.FromString("[1,,2]");

        var act = () => MapsJsonSerializer.DeserializeStreamAsync<int>(stream, default).CollectAsync();

        await act.Should().ThrowAsync<MapsApiException>();
        stream.IsDisposed.Should().BeFalse();
    }

    // -------------------- cancellation --------------------
    [Fact]
    public async Task DeserializeStreamAsync_TokenAlreadyCancelled_ThrowsOperationCanceledException() {
        await using var stream = new BlockingStream();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => MapsJsonSerializer.DeserializeStreamAsync<int>(stream, cts.Token).CollectAsync();

        var thrown = (await act.Should().ThrowAsync<OperationCanceledException>()).Which;
        thrown.Should().NotBeOfType<MapsApiException>();
    }

    [Fact]
    public async Task DeserializeStreamAsync_CancelledDuringRead_ThrowsOperationCanceledException() {
        await using var stream = new BlockingStream();
        using var cts = new CancellationTokenSource();

        var pending = MapsJsonSerializer.DeserializeStreamAsync<int>(stream, cts.Token).CollectAsync();
        await stream.ReadStarted;
        cts.Cancel();

        var act = () => pending;
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task DeserializeStreamAsync_CancelledViaWithCancellation_ThrowsOperationCanceledException() {
        // Verifies [EnumeratorCancellation]: the token given to WithCancellation must reach the stream read.
        await using var stream = new BlockingStream();
        using var cts = new CancellationTokenSource();

        async Task ConsumeAsync() {
            await foreach (var _ in MapsJsonSerializer
                .DeserializeStreamAsync<int>(stream, CancellationToken.None)
                .WithCancellation(cts.Token)
            ) { }
        }

        var task = ConsumeAsync();
        await stream.ReadStarted;
        cts.Cancel();

        var act = () => task;
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}