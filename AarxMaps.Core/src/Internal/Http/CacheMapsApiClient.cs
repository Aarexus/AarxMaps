using AarxMaps.Core.Abstractions;
using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Internal.Caching.KeyStrategy;
using AarxMaps.Core.Internal.Caching.Store;
using AarxMaps.Core.Internal.Constants;
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
        string? key = null;
        try { key = _keyStrategy.GenerateCacheKey<T>(apiRequest); }
        catch (MapsCacheException ex) { LogKeyGenerationFailed(ex); }

        if (key is not null) {
            try {
                var hit = await _cache.GetAsync<T>(key, ct).ConfigureAwait(false);
                if (hit is not null) return hit;
            }
            catch (MapsCacheException ex) { LogCacheReadFailed(ex); }
        }
        
        var response = await _client.SendAsync<T>(apiRequest, ct).ConfigureAwait(false);
        
        if (key is not null) {
            try { await _cache.SetAsync(key, response, ct).ConfigureAwait(false); }
            catch (MapsCacheException ex) { LogCacheWriteFailed(ex); }
        }

        return response;
    }

    public Task<StreamResponse> SendStreamAsync(MapsApiRequest apiRequest, CancellationToken ct)
        => _client.SendStreamAsync(apiRequest, ct);
}