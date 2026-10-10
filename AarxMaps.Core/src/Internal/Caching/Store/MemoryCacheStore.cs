using AarxMaps.Core.Configuration;
using AarxMaps.Core.Internal.Handlers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace AarxMaps.Core.Internal.Caching.Store;

internal sealed class MemoryCacheStore : ICacheStore {
    // -------------------- INIT --------------------
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheEntryOptions _entryOpts;

    public MemoryCacheStore(IMemoryCache cache, IOptions<MapsCachingOptions> options) {
        _cache = cache;
        var opts = options.Value;
        _entryOpts = new() {
            AbsoluteExpirationRelativeToNow = opts.AbsoluteExpiration,
            SlidingExpiration = opts.SlidingExpiration
        };
    }

    // -------------------- METHS --------------------
    public Task<T?> GetAsync<T>(string cacheKey, CancellationToken ct) {
        ct.ThrowIfCancellationRequested();

        var value = CacheHandler.ExecuteOrException(
            () => _cache.TryGetValue(cacheKey, out var v) && v is T typed ? typed : default,
            "MemoryCache read failed."
        );

        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string cacheKey, T value, CancellationToken ct) {
        ct.ThrowIfCancellationRequested();

        CacheHandler.ExecuteOrException(
            () => _cache.Set(cacheKey, value, _entryOpts), "MemoryCache write failed."
        );

        return Task.CompletedTask;
    }

    public Task ClearAsync(string cacheKey, CancellationToken ct) {
        ct.ThrowIfCancellationRequested();

        CacheHandler.ExecuteOrException(() => _cache.Remove(cacheKey), "MemoryCache remove failed.");

        return Task.CompletedTask;
    }
}