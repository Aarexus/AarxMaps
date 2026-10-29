using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Extensions;
using AarxMaps.Places.Internal.Services;
using AarxMaps.Places.Internal.Services.Autocomplete;
using AarxMaps.Places.Internal.Services.Details;
using AarxMaps.Places.Internal.Services.Searching;
using AarxMaps.Places.Services;
using AarxMaps.Places.Services.Autocomplete;
using AarxMaps.Places.Services.Details;
using AarxMaps.Places.Services.Searching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AarxMaps.Places.Common.Extensions;

/// <summary>
/// Provides extension methods for registering Places services
/// into the dependency injection container.
/// </summary>
/// <remarks>
/// This method registers all services required to interact with the
/// Google Places API, including search, autocomplete, and details services.
/// <para><b>Registration:</b> Core services must be registered first by calling <c>AddAarxMaps()</c>.</para>
/// </remarks>
public static class PlacesServiceCollectionExtensions {
    /// <summary>
    /// Registers all Places module services into the dependency injection container.
    /// Idempotent method therefore calling it several times does not throw.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/> used to configure application services.
    /// </param>
    /// <returns>
    /// The same <see cref="IServiceCollection"/> instance for chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="MapsApiException">
    /// Thrown when the Core services have not been registered.
    /// </exception>
    /// <example>
    /// <code>
    /// services.AddAarxMaps(configuration);
    /// services.AddAarxMapsPlaces();
    /// </code>
    /// </example>
    /// <remarks>
    /// The following services are registered with <c>Transient</c> lifetime:
    /// <list type="bullet">
    /// <item><description><see cref="IAutocompleteService"/></description></item>
    /// <item><description><see cref="IDetailsService"/></description></item>
    /// <item><description><see cref="ISearchService"/></description></item>
    /// <item><description><see cref="IPlacesService"/></description></item>
    /// </list>
    ///
    /// Transient lifetime ensures a new instance is created per request,
    /// keeping services lightweight and stateless.
    /// </remarks>
    public static IServiceCollection AddAarxMapsPlaces(this IServiceCollection services) {
        // CORE VERIFICATION
        if (!services.IsAarxMapsRegistered())
            throw new MapsApiException(
                "AarxMaps.Core services are not registered. Call AddAarxMaps() before AddAarxMapsPlaces().");

        // SERVICES
        services.TryAddTransient<IAutocompleteService, AutocompleteService>();
        services.TryAddTransient<IDetailsService, DetailsService>();
        services.TryAddTransient<ISearchService, SearchService>();
        services.TryAddTransient<IPlacesService, PlacesService>();

        return services;
    }
}