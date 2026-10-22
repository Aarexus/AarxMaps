using System.Text.Json;
using AarxMaps.Core.Abstractions;
using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Internal.Caching.KeyStrategy;
using AarxMaps.Core.Internal.Caching.Store;
using AarxMaps.Core.Internal.Constants;
using AarxMaps.Core.Internal.Handlers;
using AarxMaps.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AarxMaps.Core.Internal.Http;

internal sealed partial class CacheMapsApiClient : IMapsApiClient {
    // -------------------- INIT --------------------
    private readonly ICacheKeyStrategy _keyStrategy;
    private readonly ICacheStore _cache;
    private readonly IMapsApiClient _client;
    private readonly ILogger<CacheMapsApiClient> _logger;
    public CacheMapsApiClient(
        ICacheKeyStrategy keyStrategy, ICacheStore cache, ILogger<CacheMapsApiClient> logger,
        [FromKeyedServices(CoreConstants.MapsApiClient.Primary)]IMapsApiClient client
    ) {
        _keyStrategy = keyStrategy;
        _cache = cache;
        _logger = logger;
        _client = client;
    }

    // -------------------- LOGGING DELEGATES --------------------
    [LoggerMessage(Level = LogLevel.Warning, Message = "Maps cache key generation failed; bypassing the cache.")]
    private partial void LogKeyGenerationFailed(Exception ex);
    [LoggerMessage(Level = LogLevel.Warning, Message = "Maps cache read failed; falling back to the API.")]
    private partial void LogCacheReadFailed(Exception ex);
    [LoggerMessage(Level = LogLevel.Warning, Message = "Maps cache write failed; returning the response uncached.")]
    private partial void LogCacheWriteFailed(Exception ex);

    // -------------------- METHS --------------------
    public async Task<T> SendAsync<T>(MapsApiRequest apiRequest, CancellationToken ct) {
        if (!apiRequest.Cacheable)
            return await _client.SendAsync<T>(apiRequest, ct).ConfigureAwait(false);

        if (typeof(T) == typeof(JsonDocument))
            return (T)(object)await SendJsonDocumentAsync(apiRequest, ct).ConfigureAwait(false);

        var key = TryGenerateKey<T>(apiRequest);

        var hit = await TryReadAsync<T>(key, ct).ConfigureAwait(false);
        if (hit is not null) return hit;

        var response = await _client.SendAsync<T>(apiRequest, ct).ConfigureAwait(false);
        await TryWriteAsync(key, response, ct).ConfigureAwait(false);

        return response;
    }

    public Task<StreamResponse> SendStreamAsync(MapsApiRequest apiRequest, CancellationToken ct)
        => _client.SendStreamAsync(apiRequest, ct);

    // -------------------- INNER METHS --------------------
    // JsonDocument is disposed by its caller, so a cached instance would be dead on the next hit.
    // The raw JSON is cached instead and every call gets its own document.
    private async Task<JsonDocument> SendJsonDocumentAsync(MapsApiRequest apiRequest, CancellationToken ct) {
        var key = TryGenerateKey<JsonDocument>(apiRequest);

        var rawJson = await TryReadAsync<string>(key, ct).ConfigureAwait(false);
        if (rawJson is not null) {
            try {
                return CacheHandler.ExecuteOrException(() => JsonDocument.Parse(rawJson), "Cached JSON could not be parsed.");
            }
            catch (MapsCacheException ex) { LogCacheReadFailed(ex); }
        }

        var response = await _client.SendAsync<JsonDocument>(apiRequest, ct).ConfigureAwait(false);
        try {
            await TryWriteAsync(key, response.RootElement.GetRawText(), ct).ConfigureAwait(false);
        }
        catch {
            response.Dispose();
            throw;
        }

        return response;
    }

    private string? TryGenerateKey<T>(MapsApiRequest apiRequest) {
        try { return _keyStrategy.GenerateCacheKey<T>(apiRequest); }
        catch (MapsCacheException ex) {
            LogKeyGenerationFailed(ex);
            return null;
        }
    }
    private async Task<T?> TryReadAsync<T>(string? key, CancellationToken ct) {
        if (key is null) return default;

        try { return await _cache.GetAsync<T>(key, ct).ConfigureAwait(false); }
        catch (MapsCacheException ex) {
            LogCacheReadFailed(ex);
            return default;
        }
    }
    private async Task TryWriteAsync<T>(string? key, T value, CancellationToken ct) {
        if (key is null) return;

        try { await _cache.SetAsync(key, value, ct).ConfigureAwait(false); }
        catch (MapsCacheException ex) { LogCacheWriteFailed(ex); }
    }
}