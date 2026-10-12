using AarxMaps.Core.Configuration;
using AarxMaps.Core.Internal.Handlers;
using AarxMaps.Core.Internal.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace AarxMaps.Core.Internal.Caching.Store;

internal sealed class DistributedCacheStore : ICacheStore {
    // -------------------- INIT --------------------
    private readonly IDistributedCache _cache;
    private readonly DistributedCacheEntryOptions _entryOpts;

    public DistributedCacheStore(IDistributedCache cache, IOptions<MapsCachingOptions> options) {
        _cache = cache;
        var opts = options.Value;
        _entryOpts = new() {
            AbsoluteExpirationRelativeToNow = opts.AbsoluteExpiration,
            SlidingExpiration = opts.SlidingExpiration
        };
    }

    // -------------------- METHS --------------------
    public async Task<T?> GetAsync<T>(string cacheKey, CancellationToken ct) {
        var json = await CacheHandler.ExecuteOrExceptionAsync(
                () => _cache.GetStringAsync(cacheKey, ct), "DistributedCache read failed."
            ).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(json)) return default;

        return CacheHandler.ExecuteOrException(
            () => MapsJsonSerializer.Deserialize<T>(json), "DistributedCache entry could not be deserialized."
        );
    }

    public Task SetAsync<T>(string cacheKey, T value, CancellationToken ct) {
        if (value is null) return Task.CompletedTask;

        return CacheHandler.ExecuteOrExceptionAsync(
            () => _cache.SetStringAsync(cacheKey, MapsJsonSerializer.Serialize(value), _entryOpts, ct),
            "DistributedCache write failed."
        );
    }
    
    public Task ClearAsync(string cacheKey, CancellationToken ct)
        => CacheHandler.ExecuteOrExceptionAsync(
            () => _cache.RemoveAsync(cacheKey, ct), "DistributedCache remove failed."
        );
}