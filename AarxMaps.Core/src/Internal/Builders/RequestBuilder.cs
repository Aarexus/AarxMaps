using System.Net.Http.Json;
using AarxMaps.Core.Internal.Json;

namespace AarxMaps.Core.Internal.Builders;

internal sealed class RequestBuilder {
    // -------------------- INIT --------------------
    private HttpMethod _method = HttpMethod.Get;
    private readonly Dictionary<string, string> _headers = new (StringComparer.OrdinalIgnoreCase);
    private object? _payload;
    private readonly MapsUriBuilder _uriBuilder = new();
    
    // -------------------- BUILD --------------------
    public HttpRequestMessage Build() {
        var request = new HttpRequestMessage() {
            Method = _method,
            RequestUri = _uriBuilder.Build()
        };

        foreach (var item in _headers)
            request.Headers.TryAddWithoutValidation(item.Key, item.Value);

        if (_payload is not null)
            request.Content = JsonContent.Create(_payload, options: MapsJsonOptions.Default);

        return request;
    }

    // -------------------- CONFIG --------------------
    public RequestBuilder SetMethod(HttpMethod method) {
        _method = method;
        return this;
    }

    public RequestBuilder SetPath(Uri baseUrl, string endpoint) {
        _uriBuilder.WithPath(baseUrl, endpoint);
        return this;
    }

    public RequestBuilder AddHeaders(IDictionary<string, string> headers) {
        foreach (var h in headers)
            _headers[h.Key] = h.Value;

        return this;
    }

    public RequestBuilder AddQuery(IDictionary<string, string> query) {
        _uriBuilder.AddQuery(query);
        return this;
    }

    public RequestBuilder SetJsonPayload(object payload) {
        _payload = payload;
        return this;
    }
}