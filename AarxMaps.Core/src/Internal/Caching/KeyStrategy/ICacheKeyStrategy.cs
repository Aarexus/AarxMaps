using AarxMaps.Core.Models;

namespace AarxMaps.Core.Internal.Caching.KeyStrategy;

internal interface ICacheKeyStrategy {
    string GenerateCacheKey<T>(MapsApiRequest request);
}