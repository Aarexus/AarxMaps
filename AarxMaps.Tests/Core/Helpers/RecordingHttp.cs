using System.Collections.Concurrent;
using System.Net;
using System.Text;
using RichardSzalay.MockHttp;

namespace AarxMaps.Tests.Core.Helpers;

/// <summary>What reached the wire. The body is read inside the handler, before the client disposes the request.</summary>
internal sealed record RecordedRequest(
    HttpMethod Method, Uri Uri, IReadOnlyDictionary<string, string> Headers, string? Body);

/// <summary>MockHttp handler that records every matched request (method, URI, headers, JSON body).</summary>
internal sealed class RecordingHttp {
    private readonly ConcurrentQueue<RecordedRequest> _requests = new();

    public MockHttpMessageHandler Mock { get; } = new();

    /// <summary>Snapshot of the recorded requests, in arrival order.</summary>
    public IReadOnlyList<RecordedRequest> Requests => [.. _requests];

    public RecordingHttp Respond(HttpMethod method, string url, string json, HttpStatusCode status = HttpStatusCode.OK) {
        Mock.When(method, url).Respond(async request => {
            _requests.Enqueue(await RecordAsync(request));
            return new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        });
        return this;
    }

    private static async Task<RecordedRequest> RecordAsync(HttpRequestMessage request) {
        var headers = request.Headers.ToDictionary(
            h => h.Key, h => string.Join(",", h.Value), StringComparer.OrdinalIgnoreCase);
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync();

        return new RecordedRequest(request.Method, request.RequestUri!, headers, body);
    }
}
