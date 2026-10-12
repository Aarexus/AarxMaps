using System.Net;
using System.Reflection;
using System.Text.Json.Serialization;
using AarxMaps.Core.Abstractions;
using AarxMaps.Core.Configuration;
using AarxMaps.Core.Configuration.Validation;
using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Extensions;
using AarxMaps.Core.Internal.Caching;
using AarxMaps.Core.Internal.Caching.KeyStrategy;
using AarxMaps.Core.Internal.Caching.Store;
using AarxMaps.Core.Internal.Constants;
using AarxMaps.Core.Internal.Http;
using AarxMaps.Tests.Core.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;

namespace AarxMaps.Tests.Core.Extensions;

[Trait("Package", "Core")]
[Trait("Module", "Extensions")]
[Trait("Type", "CacheDependencyInjection")]
public class CacheDependencyInjectionTests {
    private const string Primary = CoreConstants.MapsApiClient.Primary;

    public sealed class Sample {
        [JsonPropertyName("id")] public int Id { get; set; }
    }

    // ------------------------------------------------------------------ helpers

    private static ServiceCollection WithMemoryCache(Action<MapsCachingOptions>? opts = null) {
        var services = TestServices.Create();
        services.AddAarxMapsMemoryCache(opts ?? TestServices.ValidCaching);
        return services;
    }

    private static ServiceCollection WithDistributedCache(Action<MapsCachingOptions>? opts = null) {
        var services = TestServices.Create();
        services.AddDistributedMemoryCache();                       // an IDistributedCache must exist first
        services.AddAarxMapsDistributedCache(opts ?? TestServices.ValidCaching);
        return services;
    }

    private static ServiceDescriptor LastUnkeyedClient(IServiceCollection services)
        => services.Last(d => d.ServiceType == typeof(IMapsApiClient) && !d.IsKeyedService);

    private static async Task StartHostAsync(Action<IServiceCollection> configure) {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
        builder.Services.AddAarxMaps(TestServices.ValidConfig);
        configure(builder.Services);
        using var host = builder.Build();
        await host.StartAsync();
        await host.StopAsync();
    }

    /// <summary>Full stack with a MockHttp primary handler behind the real typed HttpClient.</summary>
    private static (ServiceProvider Provider, MockHttpMessageHandler Mock, MockedRequest Rule) BuildEndToEnd(
        Action<IServiceCollection> addCache, HttpStatusCode status = HttpStatusCode.OK, string body = """{"id":1}"""
    ) {
        var services = TestServices.Create();
        var mock = new MockHttpMessageHandler();
        var rule = mock.When("*").Respond(status, "application/json", body);
        services.AddHttpClient(nameof(MapsApiClient)).ConfigurePrimaryHttpMessageHandler(() => mock); // same name AddHttpClient<MapsApiClient> uses
        addCache(services);
        return (TestServices.BuildStrict(services), mock, rule);
    }

    // ======================================================================
    // Guards & ordering (both layers)
    // ======================================================================
    [Fact]
    public void AddAarxMapsMemoryCache_NullServices_ThrowsArgumentNullException() {
        var act = () => ((IServiceCollection)null!).AddAarxMapsMemoryCache(TestServices.ValidCaching);
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddAarxMapsDistributedCache_NullServices_ThrowsArgumentNullException() {
        var act = () => ((IServiceCollection)null!).AddAarxMapsDistributedCache(TestServices.ValidCaching);
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddAarxMapsMemoryCache_NullOptionsDelegate_ThrowsArgumentNullExceptionBeforeTheCoreCheck() {
        var act = () => new ServiceCollection().AddAarxMapsMemoryCache(null!); // core is missing too
        act.Should().Throw<ArgumentNullException>().WithParameterName("cachingOpts");
    }

    [Fact]
    public void AddAarxMapsDistributedCache_NullOptionsDelegate_ThrowsArgumentNullExceptionBeforeTheCoreCheck() {
        var act = () => new ServiceCollection().AddAarxMapsDistributedCache(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("cachingOpts");
    }

    [Fact]
    public void AddAarxMapsMemoryCache_WithoutCore_ThrowsMapsApiExceptionAndRegistersNothing() {
        var services = new ServiceCollection();

        var act = () => services.AddAarxMapsMemoryCache(TestServices.ValidCaching);

        act.Should().Throw<MapsApiException>().WithMessage("*AddAarxMaps()*");
        services.Should().BeEmpty();
    }

    [Fact]
    public void AddAarxMapsDistributedCache_WithoutCore_ThrowsMapsApiExceptionEvenIfNoDistributedCacheEither() {
        var services = new ServiceCollection();

        var act = () => services.AddAarxMapsDistributedCache(TestServices.ValidCaching);

        act.Should().Throw<MapsApiException>("the core check runs before the IDistributedCache check");
        services.Should().BeEmpty();
    }

    [Fact]
    public void AddAarxMapsDistributedCache_WithoutDistributedCache_ThrowsMapsCacheExceptionAndRegistersNothingMore() {
        var services = TestServices.Create();
        var before = services.Count;

        var act = () => services.AddAarxMapsDistributedCache(TestServices.ValidCaching);

        act.Should().Throw<MapsCacheException>().WithMessage("*IDistributedCache*");
        services.Count.Should().Be(before);
    }

    [Fact]
    public void AddAarxMapsDistributedCache_DistributedCacheMustBeRegisteredBeforeTheCall() {
        var services = TestServices.Create();

        var act = () => services.AddAarxMapsDistributedCache(TestServices.ValidCaching);
        act.Should().Throw<MapsCacheException>();

        services.AddDistributedMemoryCache(); // registration order matters (documented)
        var retry = () => services.AddAarxMapsDistributedCache(TestServices.ValidCaching);
        retry.Should().NotThrow();
    }

    [Theory]
    [InlineData("memory", "memory")]
    [InlineData("memory", "distributed")]
    [InlineData("distributed", "memory")]
    [InlineData("distributed", "distributed")]
    public void SecondCacheLayer_Anything_ThrowsMapsCacheExceptionAndRegistersNothingMore(string first, string second) {
        var services = first == "memory" ? WithMemoryCache() : WithDistributedCache();
        services.AddDistributedMemoryCache(); // so the distributed guard can't be the one that fires
        var before = services.Count;

        var act = () => {
            if (second == "memory") services.AddAarxMapsMemoryCache(TestServices.ValidCaching);
            else services.AddAarxMapsDistributedCache(TestServices.ValidCaching);
        };

        act.Should().Throw<MapsCacheException>().WithMessage("*already provided*");
        services.Count.Should().Be(before);
    }

    // ======================================================================
    // Registrations
    // ======================================================================
    [Fact]
    public void AddAarxMapsMemoryCache_Always_ReturnsTheSameServiceCollection() {
        var services = TestServices.Create();
        services.AddAarxMapsMemoryCache(TestServices.ValidCaching).Should().BeSameAs(services);
    }

    [Fact]
    public void AddAarxMapsDistributedCache_Always_ReturnsTheSameServiceCollection() {
        var services = TestServices.Create();
        services.AddDistributedMemoryCache();

        services.AddAarxMapsDistributedCache(TestServices.ValidCaching).Should().BeSameAs(services);
    }

    [Theory]
    [InlineData("memory")]
    [InlineData("distributed")]
    public void CacheLayer_Registrations_AreSingletonsWithTheRightImplementations(string layer) {
        var services = layer == "memory" ? WithMemoryCache() : WithDistributedCache();
        var storeType = layer == "memory" ? typeof(MemoryCacheStore) : typeof(DistributedCacheStore);

        services.Should().ContainSingle(d => d.ServiceType == typeof(ICacheStore)
            && d.ImplementationType == storeType && d.Lifetime == ServiceLifetime.Singleton);
        services.Should().ContainSingle(d => d.ServiceType == typeof(ICacheKeyStrategy)
            && d.ImplementationType == typeof(RequestFingerprintCacheKeyStrategy) && d.Lifetime == ServiceLifetime.Singleton);
        services.Should().ContainSingle(d => d.ServiceType == typeof(MapsCachingMarker) && d.Lifetime == ServiceLifetime.Singleton);
        services.Should().ContainSingle(d => d.ServiceType == typeof(IValidateOptions<MapsCachingOptions>)
            && d.ImplementationType == typeof(MapsCachingOptionsValidator) && d.Lifetime == ServiceLifetime.Singleton);
    }

    [Theory]
    [InlineData("memory")]
    [InlineData("distributed")]
    public void CacheLayer_DecoratorIsTheLastUnkeyedClientRegistration_SoItWinsAndTheKeyedClientStaysPlain(string layer) {
        var services = layer == "memory" ? WithMemoryCache() : WithDistributedCache();

        LastUnkeyedClient(services).ImplementationType.Should().Be(typeof(CacheMapsApiClient));
        services.Should().ContainSingle(d => d.ServiceType == typeof(IMapsApiClient) && d.IsKeyedService && (string?)d.ServiceKey == Primary);
    }

    [Theory]
    [InlineData("memory")]
    [InlineData("distributed")]
    public void CacheLayer_DecoratorLifetime_IsTransient(string layer) {
        var services = layer == "memory" ? WithMemoryCache() : WithDistributedCache();
        LastUnkeyedClient(services).Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    [Fact]
    public void MemoryCacheLayer_RegistersIMemoryCache() {
        var services = WithMemoryCache();
        services.Should().Contain(d => d.ServiceType == typeof(IMemoryCache));
    }

    [Fact]
    public void MemoryCacheLayer_KeepsTheHostsExistingIMemoryCache() {
        var hostsCache = new MemoryCache(new MemoryCacheOptions());
        var services = TestServices.Create();
        services.AddSingleton<IMemoryCache>(hostsCache);
        services.AddAarxMapsMemoryCache(TestServices.ValidCaching);
        using var provider = TestServices.BuildStrict(services);

        provider.GetRequiredService<IMemoryCache>().Should().BeSameAs(hostsCache);
    }

    // ======================================================================
    // Resolution
    // ======================================================================
    [Theory]
    [InlineData("memory")]
    [InlineData("distributed")]
    public void BuildServiceProvider_WithCacheLayer_StrictModeSucceeds(string layer) {
        var services = layer == "memory" ? WithMemoryCache() : WithDistributedCache();

        var act = () => TestServices.BuildStrict(services);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("memory")]
    [InlineData("distributed")]
    public void IMapsApiClient_WithCacheLayer_ResolvesToTheDecoratorWrappingThePlainHttpClient(string layer) {
        using var provider = TestServices.BuildStrict(layer == "memory" ? WithMemoryCache() : WithDistributedCache());

        var client = provider.GetRequiredService<IMapsApiClient>();

        client.Should().BeOfType<CacheMapsApiClient>();
        provider.GetRequiredKeyedService<IMapsApiClient>(Primary).Should().BeOfType<MapsApiClient>("no self-decoration loop");
        typeof(CacheMapsApiClient).GetField("_client", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(client).Should().BeOfType<MapsApiClient>();
    }

    [Theory]
    [InlineData("memory", typeof(MemoryCacheStore))]
    [InlineData("distributed", typeof(DistributedCacheStore))]
    public void Stores_ResolveAsSingletons(string layer, Type storeType) {
        using var provider = TestServices.BuildStrict(layer == "memory" ? WithMemoryCache() : WithDistributedCache());

        var store = provider.GetRequiredService<ICacheStore>();

        store.Should().BeOfType(storeType);
        provider.GetRequiredService<ICacheStore>().Should().BeSameAs(store);
        provider.GetRequiredService<ICacheKeyStrategy>().Should().BeSameAs(provider.GetRequiredService<ICacheKeyStrategy>());
    }

    // ======================================================================
    // Options & validation
    // ======================================================================

    [Fact]
    public void CachingOptions_ReflectTheConfigureDelegate() {
        using var provider = TestServices.BuildStrict(WithMemoryCache(c => {
            c.Prefix = "my-app";
            c.AbsoluteExpiration = TimeSpan.FromHours(1);
            c.SlidingExpiration = TimeSpan.FromMinutes(5);
        }));

        var options = provider.GetRequiredService<IOptions<MapsCachingOptions>>().Value;

        options.Prefix.Should().Be("my-app");
        options.AbsoluteExpiration.Should().Be(TimeSpan.FromHours(1));
        options.SlidingExpiration.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Theory]
    [InlineData("aarx", 10.0, 1.0)]
    [InlineData("aarx", 10.0, 10.0)]   // boundary: sliding == absolute is allowed
    [InlineData("aarx", 0.5, 0.25)]
    public void CachingOptions_ValidCombinations_Resolve(string prefix, double absoluteMinutes, double slidingMinutes) {
        using var provider = TestServices.BuildStrict(WithMemoryCache(c => {
            c.Prefix = prefix;
            c.AbsoluteExpiration = TimeSpan.FromMinutes(absoluteMinutes);
            c.SlidingExpiration = TimeSpan.FromMinutes(slidingMinutes);
        }));

        var act = () => provider.GetRequiredService<IOptions<MapsCachingOptions>>().Value;

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null, 10.0, -1.0)]
    [InlineData("", 10.0, 1.0)]
    [InlineData("   ", 10.0, 0.0)]
    [InlineData("aarx", 0.0, 0.0)]
    [InlineData("aarx", -1.0, 2.0)]
    [InlineData("aarx", 10.0, 0.0)]
    [InlineData("aarx", 10.0, -1.0)]
    [InlineData("aarx", 10.0, 11.0)]   // sliding > absolute
    public void CachingOptions_InvalidCombinations_ThrowOptionsValidationException(string? prefix, double absoluteMinutes, double slidingMinutes) {
        using var provider = TestServices.BuildStrict(WithMemoryCache(c => {
            c.Prefix = prefix!;
            c.AbsoluteExpiration = TimeSpan.FromMinutes(absoluteMinutes);
            c.SlidingExpiration = TimeSpan.FromMinutes(slidingMinutes);
        }));

        var act = () => provider.GetRequiredService<IOptions<MapsCachingOptions>>().Value;

        act.Should().Throw<OptionsValidationException>().Which.Failures.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("memory")]
    [InlineData("distributed")]
    public async Task HostStart_InvalidCachingOptions_ThrowsOptionsValidationException(string layer) {
        var act = () => StartHostAsync(services => {
            if (layer == "distributed") services.AddDistributedMemoryCache();
            static void bad(MapsCachingOptions c) { TestServices.ValidCaching(c); c.Prefix = ""; }
            if (layer == "memory") services.AddAarxMapsMemoryCache(bad); else services.AddAarxMapsDistributedCache(bad);
        });

        await act.Should().ThrowAsync<OptionsValidationException>();
    }

    [Theory]
    [InlineData("memory")]
    [InlineData("distributed")]
    public async Task HostStart_ValidCachingOptions_StartsAndStops(string layer) {
        var act = () => StartHostAsync(services => {
            if (layer == "distributed") services.AddDistributedMemoryCache();
            if (layer == "memory") services.AddAarxMapsMemoryCache(TestServices.ValidCaching);
            else services.AddAarxMapsDistributedCache(TestServices.ValidCaching);
        });

        await act.Should().NotThrowAsync();
    }

    // ======================================================================
    // End to end through the container (real stores + key strategy + decorator, mocked HTTP)
    // ======================================================================
    [Fact]
    public async Task EndToEnd_MemoryCache_SecondIdenticalCallIsServedFromCacheAsTheSameInstance() {
        var (provider, mock, rule) = BuildEndToEnd(s => s.AddAarxMapsMemoryCache(TestServices.ValidCaching));
        using var _ = provider;
        var client = provider.GetRequiredService<IMapsApiClient>();

        var first = await client.SendAsync<Sample>(TestRequests.Request(), default);
        var second = await client.SendAsync<Sample>(TestRequests.Request(), default);

        mock.GetMatchCount(rule).Should().Be(1);
        first.Id.Should().Be(1);
        second.Should().BeSameAs(first, "memory hits share one instance (documented: cached models must not be mutated)");
    }

    [Fact]
    public async Task EndToEnd_MemoryCache_DifferentRequestsAreCachedSeparately() {
        var (provider, mock, rule) = BuildEndToEnd(s => s.AddAarxMapsMemoryCache(TestServices.ValidCaching));
        using var _ = provider;
        var client = provider.GetRequiredService<IMapsApiClient>();
        var a = TestRequests.Request(query: new Dictionary<string, string> { ["q"] = "a" });
        var b = TestRequests.Request(query: new Dictionary<string, string> { ["q"] = "b" });

        await client.SendAsync<Sample>(a, default);
        await client.SendAsync<Sample>(b, default);
        await client.SendAsync<Sample>(a, default);
        await client.SendAsync<Sample>(b, default);

        mock.GetMatchCount(rule).Should().Be(2);
    }

    [Fact]
    public async Task EndToEnd_DistributedCache_SecondIdenticalCallIsServedFromCacheAsAnEquivalentCopy() {
        var (provider, mock, rule) = BuildEndToEnd(s => {
            s.AddDistributedMemoryCache();
            s.AddAarxMapsDistributedCache(TestServices.ValidCaching);
        });
        using var _ = provider;
        var client = provider.GetRequiredService<IMapsApiClient>();

        var first = await client.SendAsync<Sample>(TestRequests.Request(), default);
        var second = await client.SendAsync<Sample>(TestRequests.Request(), default);

        mock.GetMatchCount(rule).Should().Be(1);
        second.Should().BeEquivalentTo(first);
        second.Should().NotBeSameAs(first, "the distributed layer round-trips through JSON");
    }

    [Theory]
    [InlineData("memory")]
    [InlineData("distributed")]
    public async Task EndToEnd_ApiFailures_AreNeverCached(string layer) {
        var (provider, mock, rule) = BuildEndToEnd(s => {
            if (layer == "distributed") s.AddDistributedMemoryCache();
            if (layer == "memory") s.AddAarxMapsMemoryCache(TestServices.ValidCaching);
            else s.AddAarxMapsDistributedCache(TestServices.ValidCaching);
        }, HttpStatusCode.InternalServerError, "{}");
        using var _ = provider;
        var client = provider.GetRequiredService<IMapsApiClient>();

        var act = () => client.SendAsync<Sample>(TestRequests.Request(), default);
        await act.Should().ThrowAsync<MapsApiException>();
        await act.Should().ThrowAsync<MapsApiException>();

        mock.GetMatchCount(rule).Should().Be(2);
    }

    [Theory]
    [InlineData("memory")]
    [InlineData("distributed")]
    public async Task EndToEnd_Streams_AreNeverCached(string layer) {
        var (provider, mock, rule) = BuildEndToEnd(s => {
            if (layer == "distributed") s.AddDistributedMemoryCache();
            if (layer == "memory") s.AddAarxMapsMemoryCache(TestServices.ValidCaching);
            else s.AddAarxMapsDistributedCache(TestServices.ValidCaching);
        }, body: "[]");
        using var _ = provider;
        var client = provider.GetRequiredService<IMapsApiClient>();

        await using (await client.SendStreamAsync(TestRequests.Request(), default)) { }
        await using (await client.SendStreamAsync(TestRequests.Request(), default)) { }

        mock.GetMatchCount(rule).Should().Be(2);
    }
}