using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using AarxMaps.Core.Abstractions;
using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Extensions;
using AarxMaps.Core.Models;
using AarxMaps.Tests.Core.Helpers;
using FluentAssertions;
using NSubstitute;

namespace AarxMaps.Tests.Core.Extensions;

/// <summary>
/// SendAndEnumerateAsync is tested against a substituted IMapsApiClient that hands out real StreamResponse
/// instances. "Released" = the body stream AND the response (observed via TrackedHttpContent) are disposed.
/// </summary>
[Trait("Package", "Core")]
[Trait("Module", "Extensions")]
[Trait("Type", "MapsApiClientExtensions")]
public class MapsApiClientExtensionsTests {
    public sealed class Sample {
        [JsonPropertyName("id")] public int Id { get; set; }
    }

    private static readonly MapsApiRequest Req = TestRequests.Request();

    // ------------------------------------------------------------------ stream doubles

    /// <summary>First read returns the chunk, every later read hangs until cancelled.</summary>
    private sealed class HangAfterChunkStream(string firstChunk) : Stream {
        private readonly byte[] _bytes = Encoding.UTF8.GetBytes(firstChunk);
        private bool _served;
        private int _disposeCount;
        public bool IsDisposed => Volatile.Read(ref _disposeCount) > 0;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) {
            if (!_served) {
                _served = true;
                var n = Math.Min(buffer.Length, _bytes.Length);
                _bytes.AsMemory(0, n).CopyTo(buffer);
                return n;
            }
            await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
            return 0;
        }

        protected override void Dispose(bool disposing) {
            if (disposing) Interlocked.Increment(ref _disposeCount);
            base.Dispose(disposing);
        }
    }

    /// <summary>Hangs on read; when cancelled it reports an IOException (as some transports do) instead of an OCE.</summary>
    private sealed class CancelAsIoExceptionStream : Stream {
        private readonly TaskCompletionSource _readStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _disposeCount;
        public Task ReadStarted => _readStarted.Task;
        public bool IsDisposed => Volatile.Read(ref _disposeCount) > 0;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) {
            _readStarted.TrySetResult();
            try { await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false); }
            catch (OperationCanceledException) { throw new IOException("The I/O operation has been aborted."); }
            return 0;
        }

        protected override void Dispose(bool disposing) {
            if (disposing) Interlocked.Increment(ref _disposeCount);
            base.Dispose(disposing);
        }
    }

    // ------------------------------------------------------------------ StreamResponse factory
    private sealed record Streamed(StreamResponse Response, TrackedHttpContent Content, Func<bool> StreamDisposed) {
        public bool Released => Content.IsDisposed && StreamDisposed();
    }

    private static Streamed From(Stream stream, Func<bool> streamDisposed) {
        var content = new TrackedHttpContent(() => stream);
        var message = new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        return new Streamed(new StreamResponse(stream, message), content, streamDisposed);
    }

    private static Streamed Json(string json) {
        var stream = FaultyStream.FromString(json);
        return From(stream, () => stream.IsDisposed);
    }

    private static Streamed Chunks(IEnumerable<string> chunks, Exception? thenThrow = null) {
        var stream = new ChunkedStream(chunks, thenThrow);
        return From(stream, () => stream.IsDisposed);
    }

    private static Streamed Blocking(out BlockingStream stream) {
        var s = stream = new BlockingStream();
        return From(s, () => s.IsDisposed);
    }

    private static IMapsApiClient InnerReturning(params Streamed[] responses) {
        var inner = Substitute.For<IMapsApiClient>();
        var queue = new Queue<Streamed>(responses);
        inner.SendStreamAsync(Arg.Any<MapsApiRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(queue.Dequeue().Response));
        return inner;
    }

    private static string SampleArray(int count)
        => "[" + string.Join(",", Enumerable.Range(1, count).Select(i => $$"""{"id":{{i}}}""")) + "]";

    // ======================================================================
    // Deferred execution & single use
    // ======================================================================
    [Fact]
    public void SendAndEnumerateAsync_NotEnumerated_SendsNoRequest() {
        var inner = InnerReturning(Json("[]"));

        _ = inner.SendAndEnumerateAsync<Sample>(Req, default);

        inner.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task SendAndEnumerateAsync_EnumeratorCreatedAndDisposedWithoutMoveNext_SendsNoRequest() {
        var inner = InnerReturning(Json("[]"));
        var sequence = inner.SendAndEnumerateAsync<Sample>(Req, default);

        await using (sequence.GetAsyncEnumerator()) { }

        inner.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task SendAndEnumerateAsync_FirstMoveNext_SendsExactlyOneRequestWithRequestAndToken() {
        var inner = InnerReturning(Json(SampleArray(1)));
        using var cts = new CancellationTokenSource();

        await inner.SendAndEnumerateAsync<Sample>(Req, cts.Token).CollectAsync();

        await inner.Received(1).SendStreamAsync(Req, cts.Token);
    }

    [Fact]
    public async Task SendAndEnumerateAsync_SameSequenceEnumeratedTwice_SendsTwoRequestsAndReleasesBoth() {
        var first = Json(SampleArray(2));
        var second = Json(SampleArray(3));
        var inner = InnerReturning(first, second);
        var sequence = inner.SendAndEnumerateAsync<Sample>(Req, default);

        var a = await sequence.CollectAsync();
        var b = await sequence.CollectAsync();

        a.Select(i => i.Id).Should().Equal(1, 2);
        b.Select(i => i.Id).Should().Equal(1, 2, 3);
        await inner.Received(2).SendStreamAsync(Req, Arg.Any<CancellationToken>());
        first.Released.Should().BeTrue();
        second.Released.Should().BeTrue();
    }

    [Fact]
    public async Task SendAndEnumerateAsync_HttpErrorOnSend_SurfacesOnFirstMoveNextNotWhenCalled() {
        var failure = new MapsNotFoundException("nf", HttpStatusCode.NotFound, null);
        var inner = Substitute.For<IMapsApiClient>();
        inner.SendStreamAsync(Arg.Any<MapsApiRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<StreamResponse>(failure));

        var create = () => inner.SendAndEnumerateAsync<Sample>(Req, default);
        var sequence = create.Should().NotThrow().Which;
        var enumerate = () => sequence.CollectAsync();

        (await enumerate.Should().ThrowAsync<MapsNotFoundException>()).Which.Should().BeSameAs(failure);
    }

    // ======================================================================
    // Elements
    // ======================================================================
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(500)]
    public async Task SendAndEnumerateAsync_ArrayOfNElements_YieldsAllInOrderAndReleasesResponse(int count) {
        var streamed = Json(SampleArray(count));
        var inner = InnerReturning(streamed);

        var items = await inner.SendAndEnumerateAsync<Sample>(Req, default).CollectAsync();

        items.Select(i => i.Id).Should().Equal(Enumerable.Range(1, count));
        streamed.Released.Should().BeTrue("the iterator disposes the response when enumeration completes");
    }

    [Fact]
    public async Task SendAndEnumerateAsync_NullElements_AreSkipped() {
        var inner = InnerReturning(Json("""[null,{"id":1},null,{"id":2}]"""));

        var items = await inner.SendAndEnumerateAsync<Sample>(Req, default).CollectAsync();

        items.Select(i => i.Id).Should().Equal(1, 2);
    }

    [Fact]
    public async Task SendAndEnumerateAsync_ElementsSplitAcrossTinyReads_AreReassembled() {
        var chunked = ChunkedStream.Split(SampleArray(20), 7);
        var streamed = From(chunked, () => chunked.IsDisposed);
        var inner = InnerReturning(streamed);

        var items = await inner.SendAndEnumerateAsync<Sample>(Req, default).CollectAsync();

        items.Select(i => i.Id).Should().Equal(Enumerable.Range(1, 20));
        streamed.Released.Should().BeTrue();
    }

    [Fact]
    public async Task SendAndEnumerateAsync_FirstElementsAreYieldedBeforeTheStreamEnds() {
        // The body delivers one chunk and then hangs forever. A true streaming pipeline must still yield
        // the first element. (If this times out, something upstream buffers the whole body.)
        var hanging = new HangAfterChunkStream("""[{"id":1},{"id":2},""");
        var streamed = From(hanging, () => hanging.IsDisposed);
        var inner = InnerReturning(streamed);
        using var cts = new CancellationTokenSource();

        var enumerator = inner.SendAndEnumerateAsync<Sample>(Req, cts.Token).GetAsyncEnumerator(cts.Token);
        var gotFirst = await enumerator.MoveNextAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        gotFirst.Should().BeTrue();
        enumerator.Current.Id.Should().Be(1);
        streamed.Released.Should().BeFalse("still enumerating");

        await enumerator.DisposeAsync(); // abandoning mid-stream
        streamed.Released.Should().BeTrue();
    }

    // ======================================================================
    // Ownership: every way out releases the response
    // ======================================================================
    [Fact]
    public async Task SendAndEnumerateAsync_ConsumerBreaksEarly_ReleasesResponse() {
        var streamed = Json(SampleArray(10));
        var inner = InnerReturning(streamed);

        await foreach (var item in inner.SendAndEnumerateAsync<Sample>(Req, default)) {
            item.Id.Should().Be(1);
            break;
        }

        streamed.Released.Should().BeTrue();
    }

    [Fact]
    public async Task SendAndEnumerateAsync_ConsumerThrowsInsideLoop_ReleasesResponse() {
        var streamed = Json(SampleArray(10));
        var inner = InnerReturning(streamed);

        var act = async () => {
            await foreach (var _ in inner.SendAndEnumerateAsync<Sample>(Req, default))
                throw new InvalidOperationException("consumer failed");
        };

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("consumer failed");
        streamed.Released.Should().BeTrue();
    }

    [Fact]
    public async Task SendAndEnumerateAsync_DisposedMidEnumerationManually_ReleasesResponse() {
        var streamed = Json(SampleArray(10));
        var inner = InnerReturning(streamed);
        var enumerator = inner.SendAndEnumerateAsync<Sample>(Req, default).GetAsyncEnumerator();

        (await enumerator.MoveNextAsync()).Should().BeTrue();
        streamed.Released.Should().BeFalse();
        await enumerator.DisposeAsync();

        streamed.Released.Should().BeTrue();
    }

    // ======================================================================
    // Mid-stream failures
    // ======================================================================
    [Theory]
    [InlineData("[1,2")]                       // truncated
    [InlineData("""[{"id":1},{"id":]""")]      // malformed after a good element
    [InlineData("""[{"id":"abc"}]""")]         // type mismatch
    [InlineData("""{"id":1}""")]               // top level is not an array
    public async Task SendAndEnumerateAsync_MalformedJson_ThrowsMapsApiExceptionAndReleasesResponse(string json) {
        var streamed = Json(json);
        var inner = InnerReturning(streamed);

        var act = () => inner.SendAndEnumerateAsync<Sample>(Req, default).CollectAsync();

        (await act.Should().ThrowExactlyAsync<MapsApiException>())
            .WithMessage("Stream could not be deserialized to Sample.");
        streamed.Released.Should().BeTrue();
    }

    [Fact]
    public async Task SendAndEnumerateAsync_JsonBreaksInLaterChunk_DeliversOnlyAValidPrefixThenThrowsAndReleases() {
        var streamed = Chunks(["""[{"id":1},{"id":2},""", "oops]"]);
        var inner = InnerReturning(streamed);
        var delivered = new List<int>();

        var act = async () => {
            await foreach (var item in inner.SendAndEnumerateAsync<Sample>(Req, default))
                delivered.Add(item.Id);
        };

        await act.Should().ThrowExactlyAsync<MapsApiException>();
        delivered.Should().BeEquivalentTo(new[] { 1, 2 }.Take(delivered.Count), o => o.WithStrictOrdering());
        streamed.Released.Should().BeTrue();
    }

    [Fact]
    public async Task SendAndEnumerateAsync_ConnectionDropsMidStream_ThrowsMapsApiExceptionWithIoInnerAndReleases() {
        var reset = new IOException("connection reset");
        var streamed = Chunks(["""[{"id":1},{"id":2},"""], thenThrow: reset);
        var inner = InnerReturning(streamed);
        var delivered = new List<int>();

        var act = async () => {
            await foreach (var item in inner.SendAndEnumerateAsync<Sample>(Req, default))
                delivered.Add(item.Id);
        };

        var ex = (await act.Should().ThrowExactlyAsync<MapsApiException>()).Which;
        ex.Message.Should().Be("The Maps API stream was interrupted.");
        ex.InnerException.Should().BeSameAs(reset);
        delivered.Should().BeEquivalentTo(new[] { 1, 2 }.Take(delivered.Count), o => o.WithStrictOrdering());
        streamed.Released.Should().BeTrue();
    }

    // ======================================================================
    // Cancellation
    // ======================================================================

    [Fact]
    public async Task SendAndEnumerateAsync_TokenGivenAsArgument_ReachesSendStreamAsync() {
        var inner = InnerReturning(Json("[]"));
        using var cts = new CancellationTokenSource();

        await inner.SendAndEnumerateAsync<Sample>(Req, cts.Token).CollectAsync();

        await inner.Received(1).SendStreamAsync(Req, cts.Token);
    }

    [Fact]
    public async Task SendAndEnumerateAsync_TokenGivenViaWithCancellation_ReachesSendStreamAsync() {
        var inner = InnerReturning(Json("[]"));
        using var cts = new CancellationTokenSource();

        await inner.SendAndEnumerateAsync<Sample>(Req, default).CollectAsync(cts.Token); // uses WithCancellation

        await inner.Received(1).SendStreamAsync(Req, cts.Token);
    }

    [Fact]
    public async Task SendAndEnumerateAsync_CancelledWhileReadingTheBody_ThrowsOperationCanceledExceptionAndReleases() {
        var streamed = Blocking(out var blocking);
        var inner = InnerReturning(streamed);
        using var cts = new CancellationTokenSource();

        var pending = inner.SendAndEnumerateAsync<Sample>(Req, default).CollectAsync(cts.Token);
        await blocking.ReadStarted;
        cts.Cancel();

        var act = () => pending;
        var thrown = (await act.Should().ThrowAsync<OperationCanceledException>()).Which;
        thrown.Should().NotBeAssignableTo<MapsApiException>();
        streamed.Released.Should().BeTrue();
    }

    [Fact]
    public async Task SendAndEnumerateAsync_TokenAlreadyCancelled_ThrowsOperationCanceledExceptionAndReleases() {
        var streamed = Blocking(out _);
        var inner = InnerReturning(streamed);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => inner.SendAndEnumerateAsync<Sample>(Req, default).CollectAsync(cts.Token);

        var thrown = (await act.Should().ThrowAsync<OperationCanceledException>()).Which;
        thrown.Should().NotBeAssignableTo<MapsApiException>();
        streamed.Released.Should().BeTrue();
    }

    /// <summary>
    /// FINDING (expected to FAIL against the code as pasted): the caller cancels, the transport reports that as an
    /// IOException instead of an OperationCanceledException, and `catch (IOException)` turns it into
    /// MapsApiException("stream was interrupted") — the caller's own cancellation is misreported as an API failure
    /// (same distinction MapsApiClient already makes for timeouts). Fix:
    /// catch (IOException ex) when (!ct.IsCancellationRequested).
    /// </summary>
    [Fact]
    [Trait("Finding", "io-exception-after-cancel")]
    public async Task SendAndEnumerateAsync_CallerCancelsAndTransportReportsIoException_ThrowsOperationCanceledException() {
        var stream = new CancelAsIoExceptionStream();
        var streamed = From(stream, () => stream.IsDisposed);
        var inner = InnerReturning(streamed);
        using var cts = new CancellationTokenSource();

        var pending = inner.SendAndEnumerateAsync<Sample>(Req, default).CollectAsync(cts.Token);
        await stream.ReadStarted;
        cts.Cancel();

        var act = () => pending;
        await act.Should().ThrowAsync<OperationCanceledException>();
        streamed.Released.Should().BeTrue();
    }
}