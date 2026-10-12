using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Internal.Factories;
using AarxMaps.Core.Internal.Http;
using AarxMaps.Core.Models;
using AarxMaps.Tests.Core.Helpers;
using FluentAssertions;
using RichardSzalay.MockHttp;

namespace AarxMaps.Tests.Core.Http;

[Trait("Package", "Core")]
[Trait("Module", "Http")]
[Trait("Type", "MapsApiClient")]
public class MapsApiClientTests {
    public enum Operation { Send, SendStream }

    public sealed class Sample {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
    }

    // helpers
    private sealed class Capture {
        public int Calls;
        public HttpRequestMessage? Request;
        public Uri? Uri;
        public string? ApiKeyHeader;
        public string? Body;
        public TrackedHttpContent? Content;
    }

    /// <summary>MockHttp responder that records the request and hands out a response with tracked content.</summary>
    private static (MockHttpMessageHandler Mock, MockedRequest Rule, Capture Cap) Setup(
        HttpStatusCode status, string body = "", Func<Stream>? stream = null
    ) {
        var cap = new Capture();
        var mock = new MockHttpMessageHandler();
        var rule = mock.When("*").Respond(async request => {
            Interlocked.Increment(ref cap.Calls);
            cap.Request = request;
            cap.Uri = request.RequestUri;
            cap.ApiKeyHeader = request.Headers.TryGetValues("X-Goog-Api-Key", out var v) ? v.SingleOrDefault() : null;
            cap.Body = request.Content is null ? null : await request.Content.ReadAsStringAsync();

            var (response, content) = TestHttp.TrackedResponse(status, body, stream);
            cap.Content = content;
            return response;
        });
        return (mock, rule, cap);
    }

    private static MapsApiClient NewClient(HttpMessageHandler handler)
        => new(new HttpClient(handler), new RequestFactory(TestRequests.ConfigOptions()));

    private static MapsApiClient NewClient(MockHttpMessageHandler mock)
        => new(mock.ToHttpClient(), new RequestFactory(TestRequests.ConfigOptions()));

    private static MapsApiRequest Req(HttpMethod? method = null, object? payload = null, IDictionary<string, string>? query = null)
        => TestRequests.Request(method: method, payload: payload, query: query);

    /// <summary>Runs the chosen operation; for streams the StreamResponse is disposed immediately.</summary>
    private static async Task CallAsync(MapsApiClient client, Operation op, MapsApiRequest? request = null, CancellationToken ct = default) {
        request ??= Req();
        if (op == Operation.Send)
            { await client.SendAsync<Sample>(request, ct); }
        else 
            { await using var response = await client.SendStreamAsync(request, ct); }
    }

    public static TheoryData<Operation> Ops => [ Operation.Send, Operation.SendStream ];
    public static TheoryData<Operation, HttpStatusCode, Type> ErrorCases() {
        var data = new TheoryData<Operation, HttpStatusCode, Type>();
        foreach (var op in new[] { Operation.Send, Operation.SendStream }) {
            data.Add(op, HttpStatusCode.BadRequest, typeof(MapsInvalidRequestException));
            data.Add(op, HttpStatusCode.Unauthorized, typeof(MapsApiAuthException));
            data.Add(op, HttpStatusCode.Forbidden, typeof(MapsApiAuthException));
            data.Add(op, HttpStatusCode.NotFound, typeof(MapsNotFoundException));
            data.Add(op, HttpStatusCode.TooManyRequests, typeof(MapsRateLimitException));
            data.Add(op, HttpStatusCode.InternalServerError, typeof(MapsApiException));
            data.Add(op, HttpStatusCode.ServiceUnavailable, typeof(MapsApiException));
        }
        return data;
    }

    // ======================================================================
    // SendAsync<T> — success path
    // ======================================================================
    [Fact]
    public async Task SendAsync_SuccessResponse_ReturnsDeserializedObject() {
        var (mock, _, _) = Setup(HttpStatusCode.OK, """{"id":3,"name":"x"}""");

        var result = await NewClient(mock).SendAsync<Sample>(Req(), default);

        result.Should().BeEquivalentTo(new Sample { Id = 3, Name = "x" });
    }

    [Fact]
    public async Task SendAsync_ValueTypeResultEqualToDefault_IsReturnedNotTreatedAsNull() {
        var (mock, _, _) = Setup(HttpStatusCode.OK, "0");

        var result = await NewClient(mock).SendAsync<int>(Req(), default);

        result.Should().Be(0);
    }

    [Fact]
    public async Task SendAsync_Request_IsBuiltByTheFactoryAndSentExactlyOnce() {
        var (mock, rule, cap) = Setup(HttpStatusCode.OK, """{"id":1}""");
        var request = Req(HttpMethod.Post, payload: new Sample { Id = 7, Name = "p" }, query: new Dictionary<string, string> { ["q"] = "1" });

        await NewClient(mock).SendAsync<Sample>(request, default);

        mock.GetMatchCount(rule).Should().Be(1);
        cap.Request!.Method.Should().Be(HttpMethod.Post);
        cap.Uri.Should().Be(new Uri("https://maps.test/v1/things?q=1"));
        cap.ApiKeyHeader.Should().Be(TestRequests.DefaultApiKey);
        using var body = JsonDocument.Parse(cap.Body!);
        body.RootElement.GetProperty("id").GetInt32().Should().Be(7);
    }

    [Fact]
    public async Task SendAsync_Success_DisposesResponseAndRequest() {
        var (mock, _, cap) = Setup(HttpStatusCode.OK, """{"id":1}""");

        await NewClient(mock).SendAsync<Sample>(Req(HttpMethod.Post, payload: new Sample { Id = 1 }), default);

        cap.Content!.IsDisposed.Should().BeTrue("SendAsync owns its response (rule 4)");
        var readDisposedRequestBody = () => cap.Request!.Content!.ReadAsStringAsync();
        await readDisposedRequestBody.Should().ThrowAsync<ObjectDisposedException>("the request message is disposed after sending");
    }

    // ======================================================================
    // SendAsync<T> — body problems (all must dispose the response)
    // ======================================================================
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\r\n\t")]
    public async Task SendAsync_EmptyOrWhitespaceBody_ThrowsMapsApiExceptionAndDisposesResponse(string body) {
        var (mock, _, cap) = Setup(HttpStatusCode.OK, body);

        var act = () => NewClient(mock).SendAsync<Sample>(Req(), default);

        (await act.Should().ThrowExactlyAsync<MapsApiException>())
            .WithMessage("The Maps API returned an empty response body.");
        cap.Content!.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_NoContentStatusWithEmptyBody_ThrowsMapsApiException() {
        var (mock, _, _) = Setup(HttpStatusCode.NoContent);

        var act = () => NewClient(mock).SendAsync<Sample>(Req(), default);

        await act.Should().ThrowExactlyAsync<MapsApiException>();
    }

    [Fact]
    public async Task SendAsync_BodyDeserializesToNull_ThrowsMapsApiExceptionAndDisposesResponse() {
        var (mock, _, cap) = Setup(HttpStatusCode.OK, "null");

        var act = () => NewClient(mock).SendAsync<Sample>(Req(), default);

        (await act.Should().ThrowExactlyAsync<MapsApiException>())
            .WithMessage("The response deserialized to null for Sample.");
        cap.Content!.IsDisposed.Should().BeTrue();
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("<html>Bad gateway</html>")]
    [InlineData("""{"id":""")]
    [InlineData("""{"id":"abc"}""")]
    public async Task SendAsync_MalformedOrMismatchedJson_ThrowsMapsApiExceptionWithJsonInnerAndDisposesResponse(string body) {
        var (mock, _, cap) = Setup(HttpStatusCode.OK, body);

        var act = () => NewClient(mock).SendAsync<Sample>(Req(), default);

        (await act.Should().ThrowExactlyAsync<MapsApiException>())
            .WithInnerException<JsonException>();
        cap.Content!.IsDisposed.Should().BeTrue();
    }

    /// <summary>
    /// The connection dies while HttpClient buffers the body (ResponseContentRead). HttpClient surfaces
    /// that as HttpRequestException, which must become MapsApiException. If this fails with a raw
    /// IOException, SendAsync leaks IOExceptions — worth a catch clause.
    /// </summary>
    [Fact]
    public async Task SendAsync_BodyTransferFailsMidway_ThrowsMapsApiExceptionAndDisposesResponse() {
        var (mock, _, cap) = Setup(HttpStatusCode.OK,
            stream: () => FaultyStream.FromString(new string('x', 100), failAfterBytes: 10));

        var act = () => NewClient(mock).SendAsync<Sample>(Req(), default);

        (await act.Should().ThrowExactlyAsync<MapsApiException>())
            .WithMessage("Network error while calling the Maps API.");
        cap.Content!.IsDisposed.Should().BeTrue();
    }

    // ======================================================================
    // SendStreamAsync — ownership & laziness
    // ======================================================================
    [Fact]
    public async Task SendStreamAsync_Success_HandsOverOwnedStreamResponse_DisposedOnlyByReceiver() {
        var (mock, _, cap) = Setup(HttpStatusCode.OK, """[{"id":1}]""");

        var streamResponse = await NewClient(mock).SendStreamAsync(Req(), default);

        cap.Content!.IsDisposed.Should().BeFalse("ownership moves to the StreamResponse (rule 2)");
        streamResponse.ResponseMessage.StatusCode.Should().Be(HttpStatusCode.OK);
        using var copy = new MemoryStream();
        await streamResponse.Stream.CopyToAsync(copy);
        Encoding.UTF8.GetString(copy.ToArray()).Should().Be("""[{"id":1}]""");

        await streamResponse.DisposeAsync();

        cap.Content.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task SendStreamAsync_Success_DoesNotBufferTheBody_UsesResponseHeadersRead() {
        var neverEnding = new BlockingStream();
        var (mock, _, _) = Setup(HttpStatusCode.OK, stream: () => neverEnding);

        // With ResponseContentRead HttpClient would try to buffer the endless body and hang → WaitAsync guard fails the test.
        await using var streamResponse = await NewClient(mock)
            .SendStreamAsync(Req(), default)
            .WaitAsync(TimeSpan.FromSeconds(10));

        neverEnding.ReadStarted.IsCompleted.Should().BeFalse("no body byte may be consumed before the receiver reads");
    }

    [Fact]
    public async Task SendStreamAsync_Success_DisposesTheRequestMessage() {
        var (mock, _, cap) = Setup(HttpStatusCode.OK, "[]");

        await using var streamResponse = await NewClient(mock)
            .SendStreamAsync(Req(HttpMethod.Post, payload: new Sample { Id = 1 }), default);

        var readDisposedRequestBody = () => cap.Request!.Content!.ReadAsStringAsync();
        await readDisposedRequestBody.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task SendStreamAsync_CalledTwice_SendsTwoRequestsAndReturnsIndependentResponses() {
        var (mock, rule, _) = Setup(HttpStatusCode.OK, "[]");
        var client = NewClient(mock);

        await using var first = await client.SendStreamAsync(Req(), default);
        await using var second = await client.SendStreamAsync(Req(), default);

        mock.GetMatchCount(rule).Should().Be(2);
        second.Should().NotBeSameAs(first);
        second.ResponseMessage.Should().NotBeSameAs(first.ResponseMessage);
    }

    [Fact]
    public async Task SendStreamAsync_OpeningTheBodyStreamFails_PropagatesAsIsAndDisposesResponse() {
        var (mock, _, cap) = Setup(HttpStatusCode.OK, stream: () => throw new InvalidOperationException("cannot open"));

        var act = () => NewClient(mock).SendStreamAsync(Req(), default);

        await act.Should().ThrowExactlyAsync<InvalidOperationException>().WithMessage("cannot open");
        cap.Content!.IsDisposed.Should().BeTrue("a failing client disposes the response before throwing (rule 1)");
    }

    [Fact]
    public async Task SendStreamAsync_OpeningTheBodyStreamIsCancelled_PropagatesCancellationAndDisposesResponse() {
        var (mock, _, cap) = Setup(HttpStatusCode.OK, stream: () => throw new OperationCanceledException());

        var act = () => NewClient(mock).SendStreamAsync(Req(), default);

        var thrown = (await act.Should().ThrowAsync<OperationCanceledException>()).Which;
        thrown.Should().NotBeAssignableTo<MapsApiException>();
        cap.Content!.IsDisposed.Should().BeTrue();
    }

    // ======================================================================
    // Both operations — HTTP error mapping & disposal
    // ======================================================================
    [Theory]
    [MemberData(nameof(ErrorCases))]
    public async Task NonSuccessStatus_ThrowsMappedExceptionWithBody_AndDisposesResponse(
        Operation op, HttpStatusCode status, Type expectedType
    ) {
        var (mock, _, cap) = Setup(status, """{"error":"x"}""");

        var act = () => CallAsync(NewClient(mock), op);

        var ex = (await act.Should().ThrowAsync<MapsApiException>()).Which;
        ex.Should().BeOfType(expectedType);
        ex.ResponseBody.Should().Be("""{"error":"x"}""");
        cap.Content!.IsDisposed.Should().BeTrue("failures dispose the response before throwing");
    }

    // ======================================================================
    // Both operations — transport failures, timeout, cancellation
    // ======================================================================
    [Theory]
    [MemberData(nameof(Ops))]
    public async Task HttpRequestException_FromTransport_BecomesMapsApiExceptionKeepingInner(Operation op) {
        var network = new HttpRequestException("dns failure");
        var handler = new StubHandler((_, _) => throw network);

        var act = () => CallAsync(NewClient(handler), op);

        var ex = (await act.Should().ThrowExactlyAsync<MapsApiException>()).Which;
        ex.Message.Should().Be("Network error while calling the Maps API.");
        ex.InnerException.Should().BeSameAs(network);
    }

    [Theory]
    [MemberData(nameof(Ops))]
    public async Task TimeoutWithoutCallerCancellation_BecomesMapsApiException(Operation op) {
        var timeout = TestHttp.TimeoutException();
        var handler = new StubHandler((_, _) => throw timeout);

        var act = () => CallAsync(NewClient(handler), op); // caller token is None → not caller cancellation

        var ex = (await act.Should().ThrowExactlyAsync<MapsApiException>()).Which;
        ex.Message.Should().Be("The Maps API request timed out.");
        ex.InnerException.Should().BeSameAs(timeout);
    }

    [Theory]
    [MemberData(nameof(Ops))]
    public async Task CallerCancelledBeforeSend_PropagatesOperationCanceledException(Operation op) {
        var handler = new StubHandler((_, ct) => {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => CallAsync(NewClient(handler), op, ct: cts.Token);

        var thrown = (await act.Should().ThrowAsync<OperationCanceledException>()).Which;
        thrown.Should().NotBeAssignableTo<MapsApiException>("caller cancellation must not look like a timeout");
    }

    [Theory]
    [MemberData(nameof(Ops))]
    public async Task CallerCancelledDuringSend_PropagatesOperationCanceledException(Operation op) {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new StubHandler(async (_, ct) => {
            started.SetResult();
            await Task.Delay(Timeout.Infinite, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var cts = new CancellationTokenSource();

        var pending = CallAsync(NewClient(handler), op, ct: cts.Token);
        await started.Task;         // the request is in flight
        cts.Cancel();

        var act = () => pending;
        var thrown = (await act.Should().ThrowAsync<OperationCanceledException>()).Which;
        thrown.Should().NotBeAssignableTo<MapsApiException>();
    }

    [Theory]
    [MemberData(nameof(Ops))]
    public async Task SendFailure_DisposesTheRequestMessage(Operation op) {
        HttpRequestMessage? captured = null;
        var handler = new StubHandler((request, _) => {
            captured = request;
            throw new HttpRequestException("boom");
        });

        var act = () => CallAsync(NewClient(handler), op, Req(HttpMethod.Post, payload: new Sample { Id = 1 }));

        await act.Should().ThrowExactlyAsync<MapsApiException>();
        var readDisposedRequestBody = () => captured!.Content!.ReadAsStringAsync();
        await readDisposedRequestBody.Should().ThrowAsync<ObjectDisposedException>();
    }
}
