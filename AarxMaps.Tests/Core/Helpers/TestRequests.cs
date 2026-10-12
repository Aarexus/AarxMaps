using AarxMaps.Core.Configuration;
using AarxMaps.Core.Enums;
using AarxMaps.Core.Models;
using Microsoft.Extensions.Options;

namespace AarxMaps.Tests.Core.Helpers;

/// <summary>
/// The ONLY place that knows how to construct MapsApiRequest / MapsConfiguration.
/// </summary>
internal static class TestRequests {
    public const string DefaultApiKey = "test-api-key";
    public static readonly Uri DefaultBaseUrl = new("https://maps.test/");

    public static MapsConfiguration Config(string apiKey = DefaultApiKey, string language = "en", string region = "US")
        => new() { ApiKey = apiKey, Language = language, Region = region };

    public static IOptions<MapsConfiguration> ConfigOptions(
        string apiKey = DefaultApiKey, string language = "en", string region = "US")
        => Options.Create(Config(apiKey, language, region));

    public static MapsApiRequest Request(
        HttpMethod? method = null,
        Uri? baseUrl = null,
        string endpoint = "v1/things",
        IDictionary<string, string>? headers = null,
        IDictionary<string, string>? query = null,
        object? payload = null,
        ApiKeyLocation apiKeyLocation = ApiKeyLocation.Header
    ) => new() {
            Method = method ?? HttpMethod.Get,
            BaseUrl = baseUrl ?? DefaultBaseUrl,
            Endpoint = endpoint,
            Headers = headers,
            Query = query,
            Payload = payload,
            ApiKeyLocation = apiKeyLocation
        };

    public static string[] QueryPairs(Uri uri)
        => uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
}
