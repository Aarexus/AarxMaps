using AarxMaps.Core.Configuration;
using AarxMaps.Core.Enums;
using AarxMaps.Core.Internal.Builders;
using AarxMaps.Core.Models;
using Microsoft.Extensions.Options;

namespace AarxMaps.Core.Internal.Factories;

internal sealed class RequestFactory {
    // -------------------- INIT --------------------
    private readonly Dictionary<string, string> _defaultConfig = [];
    private readonly Dictionary<string, string> _keyInHeader = [];
    private readonly Dictionary<string, string> _keyInQuery = [];

    public RequestFactory(IOptions<MapsConfiguration> mapsConfig) {
        var config = mapsConfig.Value;
        _defaultConfig = new() {
            { "Accept-Language", $"{config.Language}-{config.Region}" }
        };
        _keyInHeader = new() { { "X-Goog-Api-Key", config.ApiKey } };
        _keyInQuery = new() { { "key", config.ApiKey } };
    }

    // -------------------- METHS --------------------
    public HttpRequestMessage CreateRequest(MapsApiRequest request) {
        var builder = new RequestBuilder()
            .SetMethod(request.Method)
            .SetPath(request.BaseUrl, request.Endpoint)
            .AddHeaders(_defaultConfig); // Default config, it will be overwritten if it is defined in request

        if (request?.Headers?.Count > 0) 
            builder.AddHeaders(request.Headers);

        if (request?.Query?.Count > 0) 
            builder.AddQuery(request.Query);

        if (request?.Payload is not null) 
            builder.SetJsonPayload(request.Payload);

        // Key inscription
        switch (request?.ApiKeyLocation) {
            case ApiKeyLocation.Query: 
                builder.AddQuery(_keyInQuery); break;
            case ApiKeyLocation.Header: 
            default:
                builder.AddHeaders(_keyInHeader); break;
        }

        return builder.Build();
    }
}