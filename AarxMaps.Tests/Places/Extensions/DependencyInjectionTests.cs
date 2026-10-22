using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Extensions;
using AarxMaps.Places.Common.Extensions;
using AarxMaps.Places.Internal.Services;
using AarxMaps.Places.Internal.Services.Autocomplete;
using AarxMaps.Places.Internal.Services.Details;
using AarxMaps.Places.Internal.Services.Searching;
using AarxMaps.Places.Services;
using AarxMaps.Places.Services.Autocomplete;
using AarxMaps.Places.Services.Details;
using AarxMaps.Places.Services.Searching;
using AarxMaps.Tests.Core.Helpers;
using AarxMaps.Tests.Places.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using RichardSzalay.MockHttp;

namespace AarxMaps.Tests.Places.Extensions;

[Trait("Package", "Places")]
[Trait("Module", "Extensions")]
[Trait("Type", "DependencyInjection")]
public class DependencyInjectionTests {
    private const string AutocompleteUrl = "https://places.googleapis.com/v1/places:autocomplete";

    private static readonly (Type Service, Type Implementation)[] _expectedRegistrations = [
        (typeof(IAutocompleteService), typeof(AutocompleteService)),
        (typeof(IDetailsService), typeof(DetailsService)),
        (typeof(ISearchService), typeof(SearchService)),
        (typeof(IPlacesService), typeof(PlacesService))
    ];

    private static List<ServiceDescriptor> PlacesDescriptors(IServiceCollection services)
        => services.Where(d => _expectedRegistrations.Any(r => r.Service == d.ServiceType)).ToList();
    private static Task<IReadOnlyList<PlaceSuggestion>> SuggestAsync(IServiceProvider provider)
        => provider.GetRequiredService<IAutocompleteService>()
            .SuggestPlacesAsync(new AutocompleteRequestBuilder().WithInput("Bogota").Build());

    // ===================== guards & shape =====================
    [Fact]
    public void AddAarxMapsPlaces_NullServices_ThrowsArgumentNullException() {
        var act = () => ((IServiceCollection)null!).AddAarxMapsPlaces();
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }
    [Fact]
    public void AddAarxMapsPlaces_WithoutCore_ThrowsMapsApiExceptionNamingAddAarxMapsAndRegistersNothing() {
        var services = new ServiceCollection();

        var act = () => services.AddAarxMapsPlaces();

        act.Should().Throw<MapsApiException>().WithMessage("*AddAarxMaps()*AddAarxMapsPlaces()*");
        services.Should().BeEmpty();
    }
    [Fact]
    public void AddAarxMapsPlaces_Always_ReturnsTheSameServiceCollection() {
        var services = TestServices.Create();
        services.AddAarxMapsPlaces().Should().BeSameAs(services);
    }
    [Fact]
    public void AddAarxMapsPlaces_Registrations_AreTheFourServicesAsTransient() {
        var services = PlacesTestServices.Create();

        foreach (var (service, implementation) in _expectedRegistrations)
            services.Should().ContainSingle(d =>
                d.ServiceType == service
                && d.ImplementationType == implementation
                && !d.IsKeyedService
                && d.Lifetime == ServiceLifetime.Transient,
                $"{service.Name} should map to {implementation.Name}");
    }
    [Fact]
    public void AddAarxMapsPlaces_CalledTwice_IsIdempotent() {
        var services = PlacesTestServices.Create();
        var before = services.Count;

        services.AddAarxMapsPlaces();

        services.Count.Should().Be(before);
        PlacesDescriptors(services).Should().HaveCount(_expectedRegistrations.Length);
    }
    [Fact]
    public void AddAarxMapsPlaces_ServiceRegisteredBeforehand_IsNotReplaced() {
        // TryAdd semantics: a consumer's own registration wins over the module default.
        var custom = Substitute.For<ISearchService>();
        var services = TestServices.Create();
        services.AddSingleton(custom);

        services.AddAarxMapsPlaces();
        using var provider = TestServices.BuildStrict(services);

        provider.GetRequiredService<ISearchService>().Should().BeSameAs(custom);
        provider.GetRequiredService<IPlacesService>().Search.Should().BeSameAs(custom);
    }

    // ===================== resolution =====================
    [Fact]
    public void BuildServiceProvider_StrictScopesAndValidateOnBuild_Succeeds() {
        var act = () => TestServices.BuildStrict(PlacesTestServices.Create());
        act.Should().NotThrow();
    }
    [Fact]
    public void IPlacesService_Resolved_ExposesTheThreeServices() {
        using var provider = TestServices.BuildStrict(PlacesTestServices.Create());

        var places = provider.GetRequiredService<IPlacesService>();

        places.Should().BeOfType<PlacesService>();
        places.Autocomplete.Should().BeOfType<AutocompleteService>();
        places.Details.Should().BeOfType<DetailsService>();
        places.Search.Should().BeOfType<SearchService>();
    }
    [Fact]
    public void Services_ResolvedRepeatedly_AreTransient() {
        using var provider = TestServices.BuildStrict(PlacesTestServices.Create());

        foreach (var (service, _) in _expectedRegistrations)
            provider.GetRequiredService(service).Should().NotBeSameAs(provider.GetRequiredService(service),
                $"{service.Name} is transient");
    }
    [Fact]
    public void IPlacesService_ResolvedFromAScope_Works() {
        using var provider = TestServices.BuildStrict(PlacesTestServices.Create());
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IPlacesService>().Should().BeOfType<PlacesService>();
    }

    // ===================== end to end (MockHttp) =====================
    [Fact]
    public async Task EndToEnd_WithoutCache_EveryCallReachesGoogle() {
        var mock = new MockHttpMessageHandler();
        var rule = mock.When(HttpMethod.Post, AutocompleteUrl).Respond("application/json", PlacesFixtures.AutocompleteSingle);
        using var provider = PlacesTestServices.BuildWire(mock);

        await SuggestAsync(provider);
        await SuggestAsync(provider);

        mock.GetMatchCount(rule).Should().Be(2);
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EndToEnd_MemoryCache_PlacesCallsGoThroughTheCacheRegardlessOfRegistrationOrder(bool cacheFirst) {
        var mock = new MockHttpMessageHandler();
        var rule = mock.When(HttpMethod.Post, AutocompleteUrl).Respond("application/json", PlacesFixtures.AutocompleteSingle);
        var services = TestServices.Create();
        if (cacheFirst) services.AddAarxMapsMemoryCache(TestServices.ValidCaching);
        services.AddAarxMapsPlaces();
        if (!cacheFirst) services.AddAarxMapsMemoryCache(TestServices.ValidCaching);
        using var provider = TestServices.BuildStrict(services.UseMockHttp(mock));

        var first = await SuggestAsync(provider);
        var second = await SuggestAsync(provider);

        mock.GetMatchCount(rule).Should().Be(1);
        first.Should().ContainSingle().Which.PlaceId.Should().Be("ChIJ-place-1");
        second.Should().BeEquivalentTo(first);
    }
}
