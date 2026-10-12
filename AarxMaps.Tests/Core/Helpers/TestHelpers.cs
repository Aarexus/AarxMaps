using System.Net;
using System.Net.Http.Headers;
using System.Text;
using RichardSzalay.MockHttp;

namespace AarxMaps.Tests.Core.Helpers;

/// <summary>
/// HttpContent that records Dispose and hands out a caller-supplied stream.
/// HttpResponseMessage.Dispose() disposes its Content, so Content.IsDisposed == "response disposed".
/// </summary>
internal sealed class TrackedHttpContent : HttpContent {
    private readonly Func<Stream> _streamFactory;
    private int _disposeCount;

    public TrackedHttpContent(Func<Stream> streamFactory, string mediaType = "application/json") {
        _streamFactory = streamFactory;
        Headers.ContentType = new MediaTypeHeaderValue(mediaType);
    }

    public static TrackedHttpContent FromString(string body, string mediaType = "application/json") {
        var bytes = Encoding.UTF8.GetBytes(body);
        return new TrackedHttpContent(() => new MemoryStream(bytes, writable: false), mediaType);
    }

    /// <summary>Last stream handed out by ReadAsStreamAsync (null until read).</summary>
    public Stream? LastStream { get; private set; }

    public int DisposeCount => Volatile.Read(ref _disposeCount);
    public bool IsDisposed => DisposeCount > 0;

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        => _streamFactory().CopyToAsync(stream);

    protected override bool TryComputeLength(out long length) {
        length = 0;
        return false;
    }

    protected override Stream CreateContentReadStream(CancellationToken cancellationToken)
        => LastStream = _streamFactory();

    protected override Task<Stream> CreateContentReadStreamAsync()
        => Task.FromResult(LastStream = _streamFactory());

    protected override void Dispose(bool disposing) {
        if (disposing) Interlocked.Increment(ref _disposeCount);
        base.Dispose(disposing);
    }
}

/// <summary>
/// Read-only stream over a byte[] that can fail mid-read and/or fail on Dispose, and records Dispose.
/// Note: the read fault only fires if the data is longer than failAfterBytes.
/// </summary>
internal sealed class FaultyStream : Stream {
    private readonly byte[] _data;
    private readonly long? _failAfterBytes;
    private readonly Func<Exception> _readFault;
    private readonly Exception? _disposeFault;
    private long _position;
    private int _disposeCount;

    public FaultyStream(
        byte[] data,
        long? failAfterBytes = null,
        Func<Exception>? readFault = null,
        Exception? disposeFault = null)
    {
        _data = data;
        _failAfterBytes = failAfterBytes;
        _readFault = readFault ?? (() => new IOException("Simulated connection reset."));
        _disposeFault = disposeFault;
    }

    public static FaultyStream FromString(
        string body, long? failAfterBytes = null, Func<Exception>? readFault = null, Exception? disposeFault = null)
        => new(Encoding.UTF8.GetBytes(body), failAfterBytes, readFault, disposeFault);

    public int DisposeCount => Volatile.Read(ref _disposeCount);
    public bool IsDisposed => DisposeCount > 0;

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => _position; set => throw new NotSupportedException(); }
    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override int Read(byte[] buffer, int offset, int count) => ReadCore(buffer.AsSpan(offset, count));
    public override int Read(Span<byte> buffer) => ReadCore(buffer);

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        return new ValueTask<int>(ReadCore(buffer.Span));
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    private int ReadCore(Span<byte> destination) {
        if (_failAfterBytes is { } limit && _position >= limit && _position < _data.Length)
            throw _readFault();

        var remaining = (int)(_data.Length - _position);
        var allowed = _failAfterBytes is { } l ? (int)Math.Min(remaining, l - _position) : remaining;
        var n = Math.Min(destination.Length, allowed);
        _data.AsSpan((int)_position, n).CopyTo(destination);
        _position += n;
        return n;
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            Interlocked.Increment(ref _disposeCount);
            base.Dispose(disposing);
            if (_disposeFault is not null) throw _disposeFault;
            return;
        }
        base.Dispose(disposing);
    }
}

/// <summary>
/// Stream whose reads block until the token is cancelled. ReadStarted completes once a read is in flight,
/// so tests can cancel deterministically (no sleeps).
/// </summary>
internal sealed class BlockingStream : Stream {
    private readonly TaskCompletionSource _readStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _disposeCount;

    public Task ReadStarted => _readStarted.Task;
    public int DisposeCount => Volatile.Read(ref _disposeCount);
    public bool IsDisposed => DisposeCount > 0;

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

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        _readStarted.TrySetResult();
        await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
        return 0;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    protected override void Dispose(bool disposing)
    {
        if (disposing) Interlocked.Increment(ref _disposeCount);
        base.Dispose(disposing);
    }
}

/// <summary>Effectively endless stream of one byte value (capped at 16 MB), counting bytes consumed.</summary>
internal sealed class RepeatingStream : Stream {
    private const long Limit = 16 * 1024 * 1024;
    private readonly byte _value;
    private long _bytesRead;

    public RepeatingStream(byte value) => _value = value;

    public long BytesRead => Interlocked.Read(ref _bytesRead);

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override int Read(byte[] buffer, int offset, int count) => Fill(buffer.AsSpan(offset, count));
    public override int Read(Span<byte> buffer) => Fill(buffer);
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        => new(Fill(buffer.Span));
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => Task.FromResult(Fill(buffer.AsSpan(offset, count)));

    private int Fill(Span<byte> destination) {
        var remaining = Limit - Interlocked.Read(ref _bytesRead);
        var n = (int)Math.Min(destination.Length, Math.Max(remaining, 0));
        destination[..n].Fill(_value);
        Interlocked.Add(ref _bytesRead, n);
        return n;
    }
}

/// <summary>
/// Stream that hands out ONE configured chunk per read (never fills the caller's buffer past a chunk),
/// then either reports EOF or throws. Lets tests control exactly where reads split.
/// </summary>
internal sealed class ChunkedStream : Stream {
    private readonly Queue<byte[]> _chunks;
    private readonly Exception? _thenThrow;
    private byte[]? _current;
    private int _offset;
    private int _disposeCount;

    public ChunkedStream(IEnumerable<string> chunks, Exception? thenThrow = null) {
        _chunks = new Queue<byte[]>(chunks.Select(Encoding.UTF8.GetBytes));
        _thenThrow = thenThrow;
    }

    /// <summary>Splits <paramref name="text"/> (ASCII!) into fixed-size chunks.</summary>
    public static ChunkedStream Split(string text, int chunkSize)
        => new(Enumerable.Range(0, (text.Length + chunkSize - 1) / chunkSize)
            .Select(i => text.Substring(i * chunkSize, Math.Min(chunkSize, text.Length - i * chunkSize))));

    public int DisposeCount => Volatile.Read(ref _disposeCount);
    public bool IsDisposed => DisposeCount > 0;

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override int Read(byte[] buffer, int offset, int count) => ReadCore(buffer.AsSpan(offset, count));
    public override int Read(Span<byte> buffer) => ReadCore(buffer);
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        return new ValueTask<int>(ReadCore(buffer.Span));
    }
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    private int ReadCore(Span<byte> destination) {
        if (_current is null || _offset >= _current.Length) {
            if (_chunks.Count == 0) {
                if (_thenThrow is not null) throw _thenThrow;
                return 0;
            }
            _current = _chunks.Dequeue();
            _offset = 0;
        }

        var n = Math.Min(destination.Length, _current.Length - _offset);
        _current.AsSpan(_offset, n).CopyTo(destination);
        _offset += n;
        return n;
    }

    protected override void Dispose(bool disposing) {
        if (disposing) Interlocked.Increment(ref _disposeCount);
        base.Dispose(disposing);
    }
}

internal static class AsyncEnumerableTestExtensions {
    /// <summary>Drains the sequence into a list (named to avoid clashing with System.Linq.Async).</summary>
    public static async Task<List<T>> CollectAsync<T>(this IAsyncEnumerable<T> source, CancellationToken ct = default)
    {
        var list = new List<T>();
        await foreach (var item in source.WithCancellation(ct).ConfigureAwait(false))
            list.Add(item);
        return list;
    }
}

/// <summary>
/// Minimal cancellation-aware handler (MockHttp's Respond callbacks don't receive the CancellationToken).
/// Use for send-failure, timeout and cancellation scenarios; use MockHttp for plain responses.
/// </summary>
internal sealed class StubHandler : HttpMessageHandler {
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;
    private int _sendCount;

    public StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        => _handler = handler;

    public int SendCount => Volatile.Read(ref _sendCount);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        Interlocked.Increment(ref _sendCount);
        return _handler(request, cancellationToken);
    }
}

internal static class TestHttp {
    /// <summary>Response whose Content records disposal. Use with MockHttp's Respond(Func&lt;HttpRequestMessage, HttpResponseMessage&gt;).</summary>
    /// <remarks>
    /// Do NOT use MockHttp's Respond(HttpContent)/Respond(status, content) overloads for disposal tests:
    /// they can buffer/copy the content, so your tracked instance would never be disposed.
    /// </remarks>
    public static (HttpResponseMessage Response, TrackedHttpContent Content) TrackedResponse(
        HttpStatusCode status, string body = "", Func<Stream>? streamFactory = null)
    {
        var content = streamFactory is null
            ? TrackedHttpContent.FromString(body)
            : new TrackedHttpContent(streamFactory);
        return (new HttpResponseMessage(status) { Content = content }, content);
    }

    /// <summary>What HttpClient.Timeout surfaces: a TaskCanceledException whose token is NOT the caller's.</summary>
    public static TaskCanceledException TimeoutException()
        => new("The request was canceled due to the configured HttpClient.Timeout.", new TimeoutException());

    public static HttpClient ClientFor(MockHttpMessageHandler handler, string baseAddress = "https://maps.test/")
    {
        var client = handler.ToHttpClient();
        client.BaseAddress = new Uri(baseAddress);
        return client;
    }
}