using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Extensions;
using AarxMaps.Routes.Internal.Services;
using AarxMaps.Routes.Internal.Services.Directions;
using AarxMaps.Routes.Internal.Services.DistanceMatrix;
using AarxMaps.Routes.Services;
using AarxMaps.Routes.Services.Directions;
using AarxMaps.Routes.Services.DistanceMatrix;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AarxMaps.Routes.Common.Extensions;

/// <summary>
/// Provides extension methods for registering Routes services
/// into the dependency injection container.
/// </summary>
/// <remarks>
/// This method registers all services required to interact with the
/// Google Routes API, including directions (<c>computeRoutes</c>) and route matrix (<c>computeRouteMatrix</c>) services.
/// <para><b>Registration:</b> Core services must be registered first by calling <c>AddAarxMaps()</c>.</para>
/// </remarks>
public static class RoutesServiceCollectionExtensions {
    /// <summary>
    /// Registers all Routes module services into the dependency injection container.
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
    /// services.AddAarxMapsRoutes();
    /// </code>
    /// </example>
    /// <remarks>
    /// The following services are registered with <c>Transient</c> lifetime:
    /// <list type="bullet">
    /// <item><description><see cref="IDirectionsService"/></description></item>
    /// <item><description><see cref="IDistanceMatrixService"/></description></item>
    /// <item><description><see cref="IRoutesService"/></description></item>
    /// </list>
    ///
    /// Transient lifetime ensures a new instance is created per request,
    /// keeping services lightweight and stateless.
    /// </remarks>
    public static IServiceCollection AddAarxMapsRoutes(this IServiceCollection services) {
        // CORE VERIFICATION
        if (!services.IsAarxMapsRegistered())
            throw new MapsApiException(
                "AarxMaps.Core services are not registered. Call AddAarxMaps() before AddAarxMapsRoutes().");

        // SERVICES
        services.TryAddTransient<IDirectionsService, DirectionsService>();
        services.TryAddTransient<IDistanceMatrixService, DistanceMatrixService>();
        services.TryAddTransient<IRoutesService, RoutesService>();

        return services;
    }
}
