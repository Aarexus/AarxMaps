using System.Collections.Concurrent;
using System.Net;
using AarxMaps.Core.Models;
using AarxMaps.Tests.Core.Helpers;
using FluentAssertions;

namespace AarxMaps.Tests.Core.Models;

[Trait("Package", "Core")]
[Trait("Module", "Models")]
[Trait("Type", "StreamResponse")]
public class StreamResponseTests {
    // ------------------------------------------------------------------ spies

    /// <summary>MemoryStream that logs which dispose path was taken and can be told to fail.</summary>
    private sealed class SpyStream(ConcurrentQueue<string> log) : MemoryStream {
        public int DisposeCalls;
        public int DisposeAsyncCalls;
        public Exception? DisposeFault;          // thrown from Dispose(bool)
        public Exception? DisposeAsyncSyncFault; // thrown synchronously by DisposeAsync()
        public Exception? DisposeAsyncTaskFault; // returned as a faulted ValueTask

        protected override void Dispose(bool disposing) {
            if (disposing) {
                Interlocked.Increment(ref DisposeCalls);
                log.Enqueue("stream.Dispose");
            }
            base.Dispose(disposing);
            if (disposing && DisposeFault is not null) throw DisposeFault;
        }

        public override ValueTask DisposeAsync() {
            Interlocked.Increment(ref DisposeAsyncCalls);
            log.Enqueue("stream.DisposeAsync");
            base.Dispose(true); // bypass our own Dispose(bool) so only the async path is logged

            if (DisposeAsyncSyncFault is not null) throw DisposeAsyncSyncFault;
            if (DisposeAsyncTaskFault is not null) return ValueTask.FromException(DisposeAsyncTaskFault);
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>HttpResponseMessage that counts every Dispose(true) call (the real one is guarded internally).</summary>
    private sealed class SpyResponse(ConcurrentQueue<string> log) : HttpResponseMessage {
        public int DisposeCalls;

        protected override void Dispose(bool disposing) {
            if (disposing) {
                Interlocked.Increment(ref DisposeCalls);
                log.Enqueue("response.Dispose");
            }
            base.Dispose(disposing);
        }
    }

    private static (StreamResponse Sut, SpyStream Stream, SpyResponse Response, ConcurrentQueue<string> Log) Create() {
        var log = new ConcurrentQueue<string>();
        var stream = new SpyStream(log);
        var response = new SpyResponse(log);
        return (new StreamResponse(stream, response), stream, response, log);
    }

    // ------------------------------------------------------------------ construction
    [Fact]
    public void Ctor_NullStream_ThrowsArgumentNullException() {
        using var response = new HttpResponseMessage();

        var act = () => new StreamResponse(null!, response);

        act.Should().Throw<ArgumentNullException>().WithParameterName("stream");
    }

    [Fact]
    public void Ctor_NullResponseMessage_ThrowsArgumentNullException() {
        using var stream = new MemoryStream();

        var act = () => new StreamResponse(stream, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("responseMessage");
    }

    // ------------------------------------------------------------------ getters
    [Fact]
    public async Task Getters_BeforeDispose_ReturnTheInstancesPassedIn() {
        var (sut, stream, response, _) = Create();
        await using var guard = sut;

        sut.Stream.Should().BeSameAs(stream);
        sut.ResponseMessage.Should().BeSameAs(response);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Getters_AfterDispose_ThrowObjectDisposedException(bool disposeAsync) {
        var (sut, _, _, _) = Create();
        if (disposeAsync) await sut.DisposeAsync(); else sut.Dispose();

        var readStream = () => sut.Stream;
        var readMessage = () => sut.ResponseMessage;

        readStream.Should().Throw<ObjectDisposedException>().Which.ObjectName.Should().Contain("StreamResponse");
        readMessage.Should().Throw<ObjectDisposedException>().Which.ObjectName.Should().Contain("StreamResponse");
    }

    // ------------------------------------------------------------------ dispose behaviour
    [Fact]
    public void Dispose_Always_DisposesStreamThenResponse_EachOnce() {
        var (sut, stream, response, log) = Create();

        sut.Dispose();

        log.Should().Equal("stream.Dispose", "response.Dispose");
        stream.DisposeCalls.Should().Be(1);
        response.DisposeCalls.Should().Be(1);
    }

    [Fact]
    public async Task DisposeAsync_Always_UsesTheStreamsAsyncDisposeThenDisposesResponse_EachOnce() {
        var (sut, stream, response, log) = Create();

        await sut.DisposeAsync();

        log.Should().Equal("stream.DisposeAsync", "response.Dispose");
        stream.DisposeAsyncCalls.Should().Be(1);
        stream.DisposeCalls.Should().Be(0, "the sync path must not also run");
        response.DisposeCalls.Should().Be(1);
    }

    [Fact]
    public void Dispose_CalledRepeatedly_IsIdempotent() {
        var (sut, stream, response, _) = Create();

        sut.Dispose();
        sut.Dispose();
        sut.Dispose();

        stream.DisposeCalls.Should().Be(1);
        response.DisposeCalls.Should().Be(1);
    }

    [Fact]
    public async Task DisposeAsync_CalledRepeatedly_IsIdempotent() {
        var (sut, stream, response, _) = Create();

        await sut.DisposeAsync();
        await sut.DisposeAsync();

        stream.DisposeAsyncCalls.Should().Be(1);
        response.DisposeCalls.Should().Be(1);
    }

    [Fact]
    public async Task DisposeThenDisposeAsync_SharesOneDisposedFlag() {
        var (sut, stream, response, _) = Create();

        sut.Dispose();
        await sut.DisposeAsync();

        stream.DisposeCalls.Should().Be(1);
        stream.DisposeAsyncCalls.Should().Be(0);
        response.DisposeCalls.Should().Be(1);
    }

    [Fact]
    public async Task DisposeAsyncThenDispose_SharesOneDisposedFlag() {
        var (sut, stream, response, _) = Create();

        await sut.DisposeAsync();
        sut.Dispose();

        stream.DisposeAsyncCalls.Should().Be(1);
        stream.DisposeCalls.Should().Be(0);
        response.DisposeCalls.Should().Be(1);
    }

    [Fact]
    public async Task Dispose_ConcurrentMixedSyncAndAsyncCalls_DisposeEachResourceExactlyOnce() {
        var (sut, stream, response, _) = Create();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var tasks = Enumerable.Range(0, 64).Select(i => Task.Run(async () => {
            await gate.Task;                       // release everyone at once
            if (i % 2 == 0) sut.Dispose();
            else await sut.DisposeAsync();
        })).ToArray();
        gate.SetResult();
        await Task.WhenAll(tasks);

        (stream.DisposeCalls + stream.DisposeAsyncCalls).Should().Be(1);
        response.DisposeCalls.Should().Be(1);
    }

    // ------------------------------------------------------------------ failure of the stream's dispose
    [Fact]
    public void Dispose_StreamDisposeThrows_StillDisposesResponsePropagatesAndDoesNotRetry() {
        var (sut, stream, response, _) = Create();
        stream.DisposeFault = new IOException("stream boom");

        var act = () => sut.Dispose();

        act.Should().Throw<IOException>().WithMessage("stream boom");
        response.DisposeCalls.Should().Be(1, "the response is released even if the stream fails");

        act.Should().NotThrow("a second call is a no-op");
        stream.DisposeCalls.Should().Be(1);
        response.DisposeCalls.Should().Be(1);
        var readStream = () => sut.Stream;
        readStream.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public async Task DisposeAsync_StreamDisposeAsyncThrowsSynchronously_StillDisposesResponseAndPropagates() {
        var (sut, stream, response, _) = Create();
        stream.DisposeAsyncSyncFault = new IOException("sync boom");

        var act = () => sut.DisposeAsync().AsTask();

        await act.Should().ThrowAsync<IOException>().WithMessage("sync boom");
        response.DisposeCalls.Should().Be(1);

        await act.Should().NotThrowAsync("a second call is a no-op");
        stream.DisposeAsyncCalls.Should().Be(1);
        response.DisposeCalls.Should().Be(1);
    }

    [Fact]
    public async Task DisposeAsync_StreamDisposeAsyncReturnsFaultedTask_StillDisposesResponseAndPropagates() {
        var (sut, stream, response, _) = Create();
        stream.DisposeAsyncTaskFault = new IOException("task boom");

        var act = () => sut.DisposeAsync().AsTask();

        await act.Should().ThrowAsync<IOException>().WithMessage("task boom");
        response.DisposeCalls.Should().Be(1);
    }

    // ------------------------------------------------------------------ realistic scenarios
    [Fact]
    public void Dispose_StreamAlreadyDisposedByTheConsumer_DoesNotThrowAndStillDisposesResponse() {
        // e.g. a StreamReader/`using var s = response.Stream` closed the stream first
        var (sut, stream, response, _) = Create();
        sut.Stream.Dispose();

        var act = () => sut.Dispose();

        act.Should().NotThrow();
        response.DisposeCalls.Should().Be(1);
        stream.DisposeCalls.Should().Be(2); // consumer's call + ours; harmless for a well-behaved stream
    }

    [Fact]
    public async Task DisposeAsync_RealResponse_DisposesItsContent() {
        var content = TrackedHttpContent.FromString("[]");
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        var sut = new StreamResponse(new MemoryStream(), response);

        await sut.DisposeAsync();

        content.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task AwaitUsing_ReleasesBothResourcesWhenScopeExits() {
        var (sut, stream, response, _) = Create();

        await using (sut) {
            response.DisposeCalls.Should().Be(0);
        }

        stream.DisposeAsyncCalls.Should().Be(1);
        response.DisposeCalls.Should().Be(1);
    }
}