namespace AarxMaps.Core.Internal.Caching.Store;

internal interface ICacheStore {
    Task<T?> GetAsync<T>(string cacheKey, CancellationToken ct);
    Task SetAsync<T>(string cacheKey, T value, CancellationToken ct);
    Task ClearAsync(string cacheKey, CancellationToken ct);
}