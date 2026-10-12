using System.Net;
using System.Net.Http.Headers;
using System.Text;
using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Internal.Handlers;
using AarxMaps.Tests.Core.Helpers;
using FluentAssertions;

namespace AarxMaps.Tests.Core.Handlers;

[Trait("Package", "Core")]
[Trait("Module", "Handlers")]
[Trait("Type", "ExceptionHandler")]
public class ExceptionHandlerTests {
    private static HttpResponseMessage Response(HttpStatusCode status, string body = "")
        => new(status) { Content = TrackedHttpContent.FromString(body) };
    private static HttpResponseMessage Response(HttpStatusCode status, Func<Stream> stream)
        => new(status) { Content = new TrackedHttpContent(stream) };

    private static Task Ensure(HttpResponseMessage response, CancellationToken ct = default)
        => ExceptionHandler.EnsureSuccessAsync(response, ct);

    private static async Task<MapsApiException> CaptureAsync(HttpResponseMessage response, CancellationToken ct = default) {
        var act = () => Ensure(response, ct);
        return (await act.Should().ThrowAsync<MapsApiException>()).Which;
    }

    // ===================== SUCCESS =====================
    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.Accepted)]
    [InlineData(HttpStatusCode.NoContent)]
    [InlineData(HttpStatusCode.PartialContent)]
    [InlineData((HttpStatusCode)299)]
    public async Task EnsureSuccessAsync_SuccessStatus_ReturnsWithoutTouchingBody(HttpStatusCode status) {
        using var response = Response(status, () => throw new InvalidOperationException("body must not be read"));
        var act = () => Ensure(response);
        await act.Should().NotThrowAsync();
    }

    // ===================== STATUS MAPPING =====================
    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, typeof(MapsApiAuthException))]
    [InlineData(HttpStatusCode.Forbidden, typeof(MapsApiAuthException))]
    [InlineData(HttpStatusCode.TooManyRequests, typeof(MapsRateLimitException))]
    [InlineData(HttpStatusCode.NotFound, typeof(MapsNotFoundException))]
    [InlineData(HttpStatusCode.BadRequest, typeof(MapsInvalidRequestException))]
    [InlineData(HttpStatusCode.MovedPermanently, typeof(MapsApiException))]
    [InlineData(HttpStatusCode.RequestTimeout, typeof(MapsApiException))]
    [InlineData(HttpStatusCode.Conflict, typeof(MapsApiException))]
    [InlineData(HttpStatusCode.Gone, typeof(MapsApiException))]
    [InlineData(HttpStatusCode.RequestEntityTooLarge, typeof(MapsApiException))]
    [InlineData((HttpStatusCode)418, typeof(MapsApiException))]
    [InlineData(HttpStatusCode.UnprocessableEntity, typeof(MapsApiException))]
    [InlineData((HttpStatusCode)499, typeof(MapsApiException))]
    [InlineData(HttpStatusCode.InternalServerError, typeof(MapsApiException))]
    [InlineData(HttpStatusCode.NotImplemented, typeof(MapsApiException))]
    [InlineData(HttpStatusCode.BadGateway, typeof(MapsApiException))]
    [InlineData(HttpStatusCode.ServiceUnavailable, typeof(MapsApiException))]
    [InlineData(HttpStatusCode.GatewayTimeout, typeof(MapsApiException))]
    public async Task EnsureSuccessAsync_NonSuccessStatus_ThrowsExactMappedTypeWithStatusBodyAndMessage(
        HttpStatusCode status, Type expectedType
    ) {
        using var response = Response(status, """{"error":{"message":"details"}}""");

        var ex = await CaptureAsync(response);

        ex.Should().BeOfType(expectedType);
        ex.StatusCode.Should().Be(status);
        ex.ResponseBody.Should().Be("""{"error":{"message":"details"}}""");
        ex.Message.Should().Be($"Maps API returned {(int)status} {status}.");
    }

    // ===================== BODY: BOUNDED + BEST-EFFORT =====================
    [Fact]
    public async Task EnsureSuccessAsync_EmptyBody_ThrowsTypedExceptionWithNullBody() {
        using var response = Response(HttpStatusCode.NotFound, "");

        var ex = await CaptureAsync(response);

        ex.Should().BeOfType<MapsNotFoundException>();
        ex.ResponseBody.Should().BeNull();
    }

    [Fact]
    public async Task EnsureSuccessAsync_ResponseWithoutExplicitContent_ThrowsTypedExceptionWithNullBody() {
        using var response = new HttpResponseMessage(HttpStatusCode.BadGateway);

        var ex = await CaptureAsync(response);

        ex.Should().BeOfType<MapsApiException>();
        ex.ResponseBody.Should().BeNull();
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(4095, 4095)]
    [InlineData(4096, 4096)]
    [InlineData(4097, 4096)]
    [InlineData(10_000, 4096)]
    public async Task EnsureSuccessAsync_LongBody_IsTruncatedTo4096Chars(int bodyLength, int expectedLength) {
        using var response = Response(HttpStatusCode.InternalServerError, new string('a', bodyLength));

        var ex = await CaptureAsync(response);

        ex.ResponseBody.Should().HaveLength(expectedLength);
        ex.ResponseBody.Should().MatchRegex("^a+$");
    }

    [Fact]
    public async Task EnsureSuccessAsync_MultiByteBody_IsBoundedByCharsNotBytes() {
        using var response = Response(HttpStatusCode.InternalServerError, new string('é', 5000)); // 2 bytes per char

        var ex = await CaptureAsync(response);

        ex.ResponseBody.Should().Be(new string('é', 4096));
    }

    [Fact]
    public async Task EnsureSuccessAsync_EndlessBody_ReadsOnlyABoundedPrefix() {
        var stream = new RepeatingStream((byte)'x');
        using var response = Response(HttpStatusCode.InternalServerError, () => stream);

        var ex = await CaptureAsync(response);

        ex.ResponseBody.Should().Be(new string('x', 4096));
        stream.BytesRead.Should().BeLessThan(64 * 1024);
    }

    [Fact]
    public async Task EnsureSuccessAsync_InvalidUtf8Body_StillThrowsTypedExceptionWithReplacementChars() {
        using var response = Response(HttpStatusCode.BadRequest, () => new MemoryStream([ 0xC3, 0x28, 0x41 ]));

        var ex = await CaptureAsync(response);

        ex.Should().BeOfType<MapsInvalidRequestException>();
        ex.ResponseBody.Should().Contain("A").And.Contain("\uFFFD");
    }

    [Fact]
    public async Task EnsureSuccessAsync_BodyWithUtf8Bom_StripsBom() {
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes("hi")).ToArray();
        using var response = Response(HttpStatusCode.NotFound, () => new MemoryStream(bytes));

        var ex = await CaptureAsync(response);

        ex.ResponseBody.Should().Be("hi");
    }

    [Fact]
    public async Task EnsureSuccessAsync_StreamFailsMidRead_StillThrowsTypedExceptionWithNullBody_AndDisposesStream() {
        var stream = FaultyStream.FromString(new string('a', 100), failAfterBytes: 10); // IOException after 10 bytes
        using var response = Response(HttpStatusCode.Forbidden, () => stream);

        var ex = await CaptureAsync(response);

        ex.Should().BeOfType<MapsApiAuthException>();
        ex.ResponseBody.Should().BeNull();
        stream.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task EnsureSuccessAsync_OpeningBodyStreamFails_StillThrowsTypedExceptionWithNullBody() {
        using var response = Response(HttpStatusCode.NotFound, () => throw new InvalidOperationException("cannot open"));

        var ex = await CaptureAsync(response);

        ex.Should().BeOfType<MapsNotFoundException>();
        ex.ResponseBody.Should().BeNull();
    }

    [Fact]
    public async Task EnsureSuccessAsync_ContentAlreadyDisposed_StillThrowsTypedExceptionWithNullBody() {
        using var response = Response(HttpStatusCode.NotFound, "body");
        response.Content.Dispose();

        var ex = await CaptureAsync(response);

        ex.Should().BeOfType<MapsNotFoundException>();
        ex.ResponseBody.Should().BeNull();
    }

    // ===================== OWNERSHIP =====================
    [Fact]
    public async Task EnsureSuccessAsync_Failure_DisposesErrorBodyStreamButNotTheResponse() {
        var stream = FaultyStream.FromString("error body");
        var content = new TrackedHttpContent(() => stream);
        using var response = new HttpResponseMessage(HttpStatusCode.NotFound) { Content = content };

        var ex = await CaptureAsync(response);

        ex.ResponseBody.Should().Be("error body");
        stream.IsDisposed.Should().BeTrue();
        content.IsDisposed.Should().BeFalse("disposing the response is the caller's job (ownership rule 1)");
    }

    // ===================== CANCELLATION =====================
    [Fact]
    public async Task EnsureSuccessAsync_CancelledBeforeBodyRead_PropagatesOperationCanceledException() {
        using var response = Response(HttpStatusCode.NotFound, "body");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => Ensure(response, cts.Token);

        var thrown = (await act.Should().ThrowAsync<OperationCanceledException>()).Which;
        thrown.Should().NotBeAssignableTo<MapsApiException>();
    }

    [Fact]
    public async Task EnsureSuccessAsync_CancelledWhileReadingBody_PropagatesOperationCanceledException() {
        var stream = new BlockingStream();
        using var response = Response(HttpStatusCode.InternalServerError, () => stream);
        using var cts = new CancellationTokenSource();

        var pending = Ensure(response, cts.Token);
        await stream.ReadStarted;       // the body read is now in flight
        cts.Cancel();

        var act = () => pending;
        var thrown = (await act.Should().ThrowAsync<OperationCanceledException>()).Which;
        thrown.Should().NotBeAssignableTo<MapsApiException>();
        stream.IsDisposed.Should().BeTrue();
    }

    // ===================== RETRY-AFTER =====================
    [Fact]
    public async Task EnsureSuccessAsync_429WithDeltaRetryAfter_SetsRetryAfter() {
        using var response = Response(HttpStatusCode.TooManyRequests, "slow down");
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));

        var ex = await CaptureAsync(response);

        ex.Should().BeOfType<MapsRateLimitException>()
            .Which.RetryAfter.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task EnsureSuccessAsync_429WithoutRetryAfterHeader_LeavesRetryAfterNull() {
        using var response = Response(HttpStatusCode.TooManyRequests);
        var ex = await CaptureAsync(response);
        ex.Should().BeOfType<MapsRateLimitException>().Which.RetryAfter.Should().BeNull();
    }

    [Fact]
    public async Task EnsureSuccessAsync_429WithUnparseableRetryAfter_LeavesRetryAfterNull() {
        using var response = Response(HttpStatusCode.TooManyRequests);
        response.Headers.TryAddWithoutValidation("Retry-After", "soon");

        var ex = await CaptureAsync(response);

        ex.Should().BeOfType<MapsRateLimitException>().Which.RetryAfter.Should().BeNull();
    }
}