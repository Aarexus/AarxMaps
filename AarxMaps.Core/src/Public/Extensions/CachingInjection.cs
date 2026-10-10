using AarxMaps.Core.Abstractions;
using AarxMaps.Core.Configuration;
using AarxMaps.Core.Configuration.Validation;
using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Internal;
using AarxMaps.Core.Internal.Caching;
using AarxMaps.Core.Internal.Caching.KeyStrategy;
using AarxMaps.Core.Internal.Caching.Store;
using AarxMaps.Core.Internal.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AarxMaps.Core.Extensions;

public static partial class DependencyInjection {
    /// <summary>
    /// Registers an in-memory caching layer for AarxMaps using <see cref="IMemoryCache"/>.
    /// </summary>
    /// <param name="services">The application service collection.</param>
    /// <param name="cachingOpts">Configuration delegate for <see cref="MapsCachingOptions"/>.</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/> or <paramref name="cachingOpts"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="MapsApiException">
    /// <c>AddAarxMaps()</c> was not called beforehand.
    /// </exception>
    /// <exception cref="MapsCacheException">
    /// Another cache layer was already registered.
    /// </exception>
    /// <remarks>
    /// Deserialized responses are cached and shared across all AarxMaps modules
    /// (Core, Places, Routes). Entry lifetime comes from <see cref="MapsCachingOptions"/>.
    /// <para>
    /// This layer only applies to standard request/response endpoints. Streaming endpoints such as
    /// Compute Route Matrix (<c>DistanceMatrixService</c>) are never cached.
    /// </para>
    /// <para>
    /// Requires <c>AddAarxMaps()</c> to be registered beforehand.
    /// </para>
    /// <para>
    /// <b>Shared cache:</b> this uses the host's <see cref="IMemoryCache"/>. If the host configures
    /// <see cref="MemoryCacheOptions.SizeLimit"/>, entries have no size and cannot be stored, so
    /// caching is silently disabled. Cache failures never fail a request; they are logged and
    /// treated as a miss.
    /// </para>
    /// <para>
    /// <b>Instances:</b> hits return the same object instance to every caller, so cached models
    /// must not be mutated.
    /// </para>
    /// <para>
    /// <see cref="MapsCachingOptions"/> is validated at host startup.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddAarxMapsMemoryCache(
        this IServiceCollection services, Action<MapsCachingOptions> cachingOpts
    ) {
        // VERIFICATION
        VerifyCoreAndCacheOverlap(services, cachingOpts);
        
        // CACHING INJECTION
        services.AddMemoryCache();
        AddCacheServices<MemoryCacheStore>(services, cachingOpts);

        return services;
    }

     /// <summary>
    /// Registers a distributed caching layer for AarxMaps using an existing
    /// <see cref="IDistributedCache"/> implementation (Redis, SQL Server, NCache, etc.).
    /// </summary>
    /// <param name="services">The application service collection.</param>
    /// <param name="cachingOpts">Configuration delegate for <see cref="MapsCachingOptions"/>.</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/> or <paramref name="cachingOpts"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="MapsApiException">
    /// <c>AddAarxMaps()</c> was not called beforehand.
    /// </exception>
    /// <exception cref="MapsCacheException">
    /// Thrown when:
    /// <list type="bullet">
    /// <item><description>No <see cref="IDistributedCache"/> implementation is registered.</description></item>
    /// <item><description>Another cache layer was already registered.</description></item>
    /// </list>
    /// </exception>
    /// <remarks>
    /// Deserialized responses are cached as JSON and shared across all AarxMaps modules
    /// (Core, Places, Routes). Entry lifetime comes from <see cref="MapsCachingOptions"/>.
    /// <para>
    /// This layer only applies to standard request/response endpoints. Streaming endpoints such as
    /// Compute Route Matrix (<c>DistanceMatrixService</c>) are never cached.
    /// </para>
    /// <para>
    /// Requires <c>AddAarxMaps()</c> to be registered beforehand, and an
    /// <see cref="IDistributedCache"/> to already be in the service collection at the time of
    /// this call (registration order matters).
    /// </para>
    /// <para>
    /// Cache failures never fail a request; they are logged and treated as a miss. Entries are keyed
    /// by a versioned fingerprint of the request, so deploying an incompatible model change
    /// requires bumping the key version.
    /// </para>
    /// <para>
    /// <see cref="MapsCachingOptions"/> is validated at host startup.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddAarxMapsDistributedCache(
        this IServiceCollection services, Action<MapsCachingOptions> cachingOpts
    ) {   
        // VERIFICATION
        VerifyCoreAndCacheOverlap(services, cachingOpts);

        if (!services.Any(s => s.ServiceType == typeof(IDistributedCache)))
            throw new MapsCacheException(
                "No IDistributedCache is registered. Register one (Redis, SQL Server, ...) " +
                "before calling AddAarxMapsDistributedCache()."
            );
        
        // CACHING INJECTION
        AddCacheServices<DistributedCacheStore>(services, cachingOpts);

        return services;
    }

    private static void AddCacheServices<TStore>(
        IServiceCollection services, Action<MapsCachingOptions> cachingOpts
    ) where TStore : class, ICacheStore {
        // CONFIGURATION
        services.AddOptions<MapsCachingOptions>().Configure(cachingOpts).ValidateOnStart();
        services.AddSingleton<IValidateOptions<MapsCachingOptions>, MapsCachingOptionsValidator>();
            
        services.AddSingleton<ICacheStore, TStore>();
        services.AddSingleton<ICacheKeyStrategy, RequestFingerprintCacheKeyStrategy>();

        // SERVICES
        services.AddSingleton<IMapsApiClient, CacheMapsApiClient>();
        services.AddSingleton<MapsCachingMarker>();
    }
    
    private static void VerifyCoreAndCacheOverlap(
        IServiceCollection services, Action<MapsCachingOptions> cachingOpts
    ) {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(cachingOpts);

        if (!services.Any(s => s.ServiceType == typeof(MapsCoreMarker)))
            throw new MapsApiException(
            "AarxMaps.Core services are not registered. Call AddAarxMaps() before adding cache-memory layer."
        );
        
        if (services.Any(s => s.ServiceType == typeof(MapsCachingMarker)))
            throw new MapsCacheException(
                "Cache layer was already provided. You can only use either AddMemoryCache() or AddDistributedCache()."
            );
    }
}