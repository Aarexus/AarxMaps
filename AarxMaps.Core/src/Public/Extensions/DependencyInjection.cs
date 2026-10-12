using AarxMaps.Core.Internal.Factories;
using AarxMaps.Core.Internal.Http;
using AarxMaps.Core.Abstractions;
using AarxMaps.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AarxMaps.Core.Internal;
using Microsoft.Extensions.Options;
using AarxMaps.Core.Internal.Constants;
using Microsoft.Extensions.DependencyInjection.Extensions;
using AarxMaps.Core.Configuration.Validation;

namespace AarxMaps.Core.Extensions;

/// <summary>
/// Provides extension methods for registering the AarxMaps SDK services
/// into the dependency injection container.
/// </summary>
public static partial class DependencyInjection {
    /// <summary>
    /// Registers the core services required to use the AarxMaps SDK.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/> used to configure application services.
    /// </param>
    /// <param name="config">
    /// The <see cref="MapsConfiguration"/> delegate used to configure authentication,
    /// localization and timeouts.
    /// </param>
    /// <returns>
    /// The updated <see cref="IServiceCollection"/> instance.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/> or <paramref name="config"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// <c>AddAarxMaps()</c> was already called on this service collection.
    /// </exception>
    /// <remarks>
    /// Registers the API client, request factory and typed <see cref="HttpClient"/>
    /// (configured with <see cref="MapsConfiguration.RequestTimeout"/>) required to communicate
    /// with the Google Maps API.
    /// <para>
    /// <b>Validation:</b> <see cref="MapsConfiguration"/> is validated when the host starts.
    /// An invalid configuration (for example, a missing <see cref="MapsConfiguration.ApiKey"/>)
    /// throws <see cref="OptionsValidationException"/> at startup. Without a host (a bare
    /// <see cref="ServiceCollection"/>), validation runs the first time the options are resolved.
    /// </para>
    /// <para>
    /// <b>Caching:</b> none by default. Add <c>AddAarxMapsMemoryCache()</c> or
    /// <c>AddAarxMapsDistributedCache()</c> afterwards to enable it.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddAarxMaps(
        this IServiceCollection services, Action<MapsConfiguration> config
    ) {
        // VALIDATION
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(config);
        if (services.Any(s => s.ServiceType == typeof(MapsCoreMarker)))
            throw new InvalidOperationException("AddAarxMaps() was already called.");

        // CONFIGURATION
        services.AddOptions<MapsConfiguration>().Configure(config).ValidateOnStart();
        services.AddSingleton<IValidateOptions<MapsConfiguration>, MapsConfigurationValidator>();

        // HTTP
        services.AddHttpClient<MapsApiClient>((sp, client) => {
            var opts = sp.GetRequiredService<IOptions<MapsConfiguration>>().Value;
            client.Timeout = opts.RequestTimeout;
        });
        // Default (unkeyed) registration, so the cache layer can override it
        services.TryAddTransient(
            sp => sp.GetRequiredKeyedService<IMapsApiClient>(CoreConstants.MapsApiClient.Primary)
        );
        services.AddKeyedTransient<IMapsApiClient>(
            CoreConstants.MapsApiClient.Primary, (sp, _) => sp.GetRequiredService<MapsApiClient>()
        );

        // UTILS
        services.AddSingleton<RequestFactory>();
        services.AddSingleton<MapsCoreMarker>();

        return services;
    }
}