namespace AarxMaps.Core.Models;

/// <summary>
/// Holds a streamed HTTP response: the content <see cref="Stream"/> together with the
/// <see cref="HttpResponseMessage"/> that backs it.
/// </summary>
/// <remarks>
/// Disposing the stream alone does not reliably release the HTTP connection, so both
/// are owned and released together by this type.
/// <para>
/// <b>Ownership:</b> whoever receives a <see cref="StreamResponse"/> must dispose it
/// (preferably with <c>await using</c>). There are no exceptions to this rule.
/// </para>
/// <para>
/// <b>Reading:</b> <see cref="Stream"/> is the only supported read path. Do not read
/// <c>ResponseMessage.Content</c> directly, since it is the same underlying stream.
/// </para>
/// </remarks>
public sealed class StreamResponse : IDisposable, IAsyncDisposable {
    private readonly Stream _stream;
    private readonly HttpResponseMessage _responseMessage;
    private int _disposed;

    internal StreamResponse(Stream stream, HttpResponseMessage responseMessage) {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(responseMessage);

        _stream = stream;
        _responseMessage = responseMessage;
    }

    /// <summary>Gets the response content stream.</summary>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    public Stream Stream {
        get {
            ObjectDisposedException.ThrowIf(_disposed != 0, this);
            return _stream;
        }
    }

    /// <summary>
    /// Gets the underlying response, for inspecting status, headers or diagnostics only.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    public HttpResponseMessage ResponseMessage {
        get {
            ObjectDisposedException.ThrowIf(_disposed != 0, this);
            return _responseMessage;
        }
    }

    /// <summary>
    /// Disposes the stream and the response message. Idempotent and thread-safe.
    /// The response message is disposed even if disposing the stream throws.
    /// </summary>
    public void Dispose() {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

        try { _stream.Dispose(); }
        finally { _responseMessage.Dispose(); }
    }

    /// <summary>
    /// Asynchronously disposes the stream, then disposes the response message.
    /// Idempotent and thread-safe. The response message is disposed even if
    /// disposing the stream throws.
    /// </summary>
    public async ValueTask DisposeAsync() {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

        try { await _stream.DisposeAsync().ConfigureAwait(false); }
        finally { _responseMessage.Dispose(); }
    }
}