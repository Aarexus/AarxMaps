using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Extensions;
using AarxMaps.Routes.Common.Extensions;
using AarxMaps.Routes.Internal.Services;
using AarxMaps.Routes.Internal.Services.Directions;
using AarxMaps.Routes.Internal.Services.DistanceMatrix;
using AarxMaps.Routes.Services;
using AarxMaps.Routes.Services.Directions;
using AarxMaps.Routes.Services.DistanceMatrix;
using AarxMaps.Tests.Core.Helpers;
using AarxMaps.Tests.Routes.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using RichardSzalay.MockHttp;

namespace AarxMaps.Tests.Routes.Extensions;

[Trait("Package", "Routes")]
[Trait("Module", "Extensions")]
[Trait("Type", "DependencyInjection")]
public class DependencyInjectionTests {
    private const string RoutesUrl = "https://routes.googleapis.com/directions/v2:computeRoutes";

    private static readonly (Type Service, Type Implementation)[] _expectedRegistrations = [
        (typeof(IDirectionsService), typeof(DirectionsService)),
        (typeof(IDistanceMatrixService), typeof(DistanceMatrixService)),
        (typeof(IRoutesService), typeof(RoutesService))
    ];

    private static List<ServiceDescriptor> RoutesDescriptors(IServiceCollection services)
        => services.Where(d => _expectedRegistrations.Any(r => r.Service == d.ServiceType)).ToList();
    private static Task<RouteResult> GetRoutesAsync(IServiceProvider provider)
        => provider.GetRequiredService<IDirectionsService>().GetRoutesAsync(new RouteRequestBuilder()
            .From(RoutesSamples.At(RoutesSamples.Bogota)).To(RoutesSamples.At(RoutesSamples.Usaquen)).Build());

    // ===================== guards & shape =====================
    [Fact]
    public void AddAarxMapsRoutes_NullServices_ThrowsArgumentNullException() {
        var act = () => ((IServiceCollection)null!).AddAarxMapsRoutes();
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }
    [Fact]
    public void AddAarxMapsRoutes_WithoutCore_ThrowsMapsApiExceptionNamingAddAarxMapsAndRegistersNothing() {
        var services = new ServiceCollection();

        var act = () => services.AddAarxMapsRoutes();

        act.Should().Throw<MapsApiException>().WithMessage("*AddAarxMaps()*AddAarxMapsRoutes()*");
        services.Should().BeEmpty();
    }
    [Fact]
    public void AddAarxMapsRoutes_Always_ReturnsTheSameServiceCollection() {
        var services = TestServices.Create();
        services.AddAarxMapsRoutes().Should().BeSameAs(services);
    }
    [Fact]
    public void AddAarxMapsRoutes_Registrations_AreTheThreeServicesAsTransient() {
        var services = RoutesTestServices.Create();

        foreach (var (service, implementation) in _expectedRegistrations)
            services.Should().ContainSingle(d =>
                d.ServiceType == service
                && d.ImplementationType == implementation
                && !d.IsKeyedService
                && d.Lifetime == ServiceLifetime.Transient,
                $"{service.Name} should map to {implementation.Name}");
    }
    [Fact]
    public void AddAarxMapsRoutes_CalledTwice_IsIdempotent() {
        var services = RoutesTestServices.Create();
        var before = services.Count;

        services.AddAarxMapsRoutes();

        services.Count.Should().Be(before);
        RoutesDescriptors(services).Should().HaveCount(_expectedRegistrations.Length);
    }
    [Fact]
    public void AddAarxMapsRoutes_ServiceRegisteredBeforehand_IsNotReplaced() {
        // TryAdd semantics: a consumer's own registration wins over the module default.
        var custom = Substitute.For<IDirectionsService>();
        var services = TestServices.Create();
        services.AddSingleton(custom);

        services.AddAarxMapsRoutes();
        using var provider = TestServices.BuildStrict(services);

        provider.GetRequiredService<IDirectionsService>().Should().BeSameAs(custom);
        provider.GetRequiredService<IRoutesService>().Directions.Should().BeSameAs(custom);
    }

    // ===================== resolution =====================
    [Fact]
    public void BuildServiceProvider_StrictScopesAndValidateOnBuild_Succeeds() {
        var act = () => TestServices.BuildStrict(RoutesTestServices.Create());
        act.Should().NotThrow();
    }
    [Fact]
    public void IRoutesService_Resolved_ExposesBothServices() {
        using var provider = TestServices.BuildStrict(RoutesTestServices.Create());

        var routes = provider.GetRequiredService<IRoutesService>();

        routes.Should().BeOfType<RoutesService>();
        routes.Directions.Should().BeOfType<DirectionsService>();
        routes.DistanceMatrix.Should().BeOfType<DistanceMatrixService>();
    }
    [Fact]
    public void Services_ResolvedRepeatedly_AreTransient() {
        using var provider = TestServices.BuildStrict(RoutesTestServices.Create());

        foreach (var (service, _) in _expectedRegistrations)
            provider.GetRequiredService(service).Should().NotBeSameAs(provider.GetRequiredService(service),
                $"{service.Name} is transient");
    }
    [Fact]
    public void IRoutesService_ResolvedFromAScope_Works() {
        using var provider = TestServices.BuildStrict(RoutesTestServices.Create());
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IRoutesService>().Should().BeOfType<RoutesService>();
    }

    // ===================== end to end (MockHttp) =====================
    [Fact]
    public async Task EndToEnd_WithoutCache_EveryCallReachesGoogle() {
        var mock = new MockHttpMessageHandler();
        var rule = mock.When(HttpMethod.Post, RoutesUrl).Respond("application/json", RoutesFixtures.RoutesDrive);
        using var provider = RoutesTestServices.BuildWire(mock);

        await GetRoutesAsync(provider);
        await GetRoutesAsync(provider);

        mock.GetMatchCount(rule).Should().Be(2);
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EndToEnd_MemoryCache_RoutesCallsGoThroughTheCacheRegardlessOfRegistrationOrder(bool cacheFirst) {
        var mock = new MockHttpMessageHandler();
        var rule = mock.When(HttpMethod.Post, RoutesUrl).Respond("application/json", RoutesFixtures.RoutesDrive);
        var services = TestServices.Create();
        if (cacheFirst) services.AddAarxMapsMemoryCache(TestServices.ValidCaching);
        services.AddAarxMapsRoutes();
        if (!cacheFirst) services.AddAarxMapsMemoryCache(TestServices.ValidCaching);
        using var provider = TestServices.BuildStrict(services.UseMockHttp(mock));

        var first = await GetRoutesAsync(provider);
        var second = await GetRoutesAsync(provider);

        mock.GetMatchCount(rule).Should().Be(1);
        first.Routes.Should().HaveCount(2);
        second.Should().BeEquivalentTo(first);
    }
}
