using AarxMaps.Routes.Common.Extensions;
using AarxMaps.Tests.Core.Helpers;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;

namespace AarxMaps.Tests.Routes.Helpers;

/// <summary>ServiceCollection factories for Routes tests (Core is always registered first).</summary>
internal static class RoutesTestServices {
    /// <summary>New collection with AddAarxMaps and AddAarxMapsRoutes already applied.</summary>
    public static ServiceCollection Create() {
        var services = TestServices.Create();
        services.AddAarxMapsRoutes();
        return services;
    }

    /// <summary>
    /// Strict provider whose real MapsApiClient sends through <paramref name="mock"/>.
    /// <paramref name="configure"/> runs after AddAarxMapsRoutes (e.g. to add a cache layer).
    /// </summary>
    public static ServiceProvider BuildWire(MockHttpMessageHandler mock, Action<IServiceCollection>? configure = null) {
        var services = Create();
        services.UseMockHttp(mock);
        configure?.Invoke(services);
        return TestServices.BuildStrict(services);
    }
}
