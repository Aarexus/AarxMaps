using System.Reflection;
using AarxMaps.Core.Abstractions;
using AarxMaps.Core.Configuration;
using AarxMaps.Core.Configuration.Validation;
using AarxMaps.Core.Extensions;
using AarxMaps.Core.Internal;
using AarxMaps.Core.Internal.Constants;
using AarxMaps.Core.Internal.Factories;
using AarxMaps.Core.Internal.Http;
using AarxMaps.Core.Models;
using AarxMaps.Tests.Core.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AarxMaps.Tests.Core.Extensions;

[Trait("Package", "Core")]
[Trait("Module", "Extensions")]
[Trait("Type", "DependencyInjection")]
public class DependencyInjectionTests {
    private const string Primary = CoreConstants.MapsApiClient.Primary;

    private sealed class OtherClient : IMapsApiClient {
        public Task<T> SendAsync<T>(MapsApiRequest request, CancellationToken ct) => throw new NotImplementedException();
        public Task<StreamResponse> SendStreamAsync(MapsApiRequest request, CancellationToken ct) => throw new NotImplementedException();
    }

    private static HttpClient HttpClientOf(MapsApiClient client)
        => (HttpClient)typeof(MapsApiClient)
            .GetField("_httpClient", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(client)!;

    private static async Task StartHostAsync(Action<MapsConfiguration> config) {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
        builder.Services.AddAarxMaps(config);
        using var host = builder.Build();   // (Build and Start are one unit: ValidateOnStart may fire in either, by .NET version)
        await host.StartAsync();
        await host.StopAsync();
    }

    // ===================== guards & shape =====================
    [Fact]
    public void AddAarxMaps_NullServices_ThrowsArgumentNullException() {
        var act = () => ((IServiceCollection)null!).AddAarxMaps(TestServices.ValidConfig);
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddAarxMaps_NullConfig_ThrowsArgumentNullException() {
        var act = () => new ServiceCollection().AddAarxMaps(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("config");
    }

    [Fact]
    public void AddAarxMaps_Always_ReturnsTheSameServiceCollection() {
        var services = new ServiceCollection();
        services.AddAarxMaps(TestServices.ValidConfig).Should().BeSameAs(services);
    }

    [Fact]
    public void AddAarxMaps_CalledTwice_ThrowsInvalidOperationExceptionAndLeavesRegistrationsUntouched() {
        var services = TestServices.Create();
        var before = services.Count;

        var act = () => services.AddAarxMaps(TestServices.ValidConfig);

        act.Should().Throw<InvalidOperationException>().WithMessage("AddAarxMaps() was already called.");
        services.Count.Should().Be(before);
    }

    [Fact]
    public void AddAarxMaps_Registrations_HaveTheDocumentedLifetimesAndKeys() {
        var services = TestServices.Create();

        services.Should().ContainSingle(d => d.ServiceType == typeof(MapsCoreMarker) && d.Lifetime == ServiceLifetime.Singleton);
        services.Should().ContainSingle(d => d.ServiceType == typeof(RequestFactory) && d.Lifetime == ServiceLifetime.Singleton);
        services.Should().ContainSingle(d =>
            d.ServiceType == typeof(IMapsApiClient) && !d.IsKeyedService && d.Lifetime == ServiceLifetime.Transient);
        services.Should().ContainSingle(d =>
            d.ServiceType == typeof(IMapsApiClient) && d.IsKeyedService && (string?)d.ServiceKey == Primary
            && d.Lifetime == ServiceLifetime.Transient);
        services.Should().ContainSingle(d =>
            d.ServiceType == typeof(IValidateOptions<MapsConfiguration>)
            && d.ImplementationType == typeof(MapsConfigurationValidator)
            && d.Lifetime == ServiceLifetime.Singleton);
    }

    // ===================== resolution =====================
    [Fact]
    public void BuildServiceProvider_StrictScopesAndValidateOnBuild_Succeeds() {
        var act = () => TestServices.BuildStrict(TestServices.Create());
        act.Should().NotThrow();
    }

    [Fact]
    public void IMapsApiClient_UnkeyedWithoutCacheLayer_ResolvesToMapsApiClient() {
        using var provider = TestServices.BuildStrict(TestServices.Create());
        provider.GetRequiredService<IMapsApiClient>().Should().BeOfType<MapsApiClient>();
    }

    [Fact]
    public void IMapsApiClient_Keyed_ResolvesToMapsApiClient() {
        using var provider = TestServices.BuildStrict(TestServices.Create());
        provider.GetRequiredKeyedService<IMapsApiClient>(Primary).Should().BeOfType<MapsApiClient>();
    }

    [Fact]
    public void IMapsApiClient_ResolvedRepeatedly_IsTransient() {
        using var provider = TestServices.BuildStrict(TestServices.Create());

        var a = provider.GetRequiredService<IMapsApiClient>();
        var b = provider.GetRequiredService<IMapsApiClient>();
        var keyed = provider.GetRequiredKeyedService<IMapsApiClient>(Primary);

        a.Should().NotBeSameAs(b);
        a.Should().NotBeSameAs(keyed);
    }

    [Fact]
    public void IMapsApiClient_ResolvedFromAScope_Works() {
        using var provider = TestServices.BuildStrict(TestServices.Create());
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IMapsApiClient>().Should().BeOfType<MapsApiClient>();
    }

    [Fact]
    public void RequestFactoryAndMarker_AreSingletonsAcrossScopes() {
        using var provider = TestServices.BuildStrict(TestServices.Create());
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        scope1.ServiceProvider.GetRequiredService<RequestFactory>()
            .Should().BeSameAs(scope2.ServiceProvider.GetRequiredService<RequestFactory>());
        provider.GetRequiredService<MapsCoreMarker>().Should().BeSameAs(provider.GetRequiredService<MapsCoreMarker>());
    }

    [Fact]
    public void Options_ReflectTheConfigureDelegate() {
        using var provider = TestServices.BuildStrict(TestServices.Create(c => {
            TestServices.ValidConfig(c);
            c.ApiKey = "my-key";
            c.Language = "es";
            c.Region = "CO";
        }));

        var options = provider.GetRequiredService<IOptions<MapsConfiguration>>().Value;

        options.ApiKey.Should().Be("my-key");
        options.Language.Should().Be("es");
        options.Region.Should().Be("CO");
    }

    [Fact]
    public void Validator_IsRegisteredForMapsConfiguration() {
        using var provider = TestServices.BuildStrict(TestServices.Create());

        provider.GetServices<IValidateOptions<MapsConfiguration>>()
            .Should().ContainSingle().Which.Should().BeOfType<MapsConfigurationValidator>();
    }

    [Fact]
    public void AddAarxMaps_LaterUnkeyedRegistration_OverridesDefaultButKeyedClientStaysTheHttpClient() {
        // This is exactly how the cache layer takes over the unkeyed IMapsApiClient.
        var services = TestServices.Create();
        services.AddTransient<IMapsApiClient, OtherClient>();
        using var provider = TestServices.BuildStrict(services);

        provider.GetRequiredService<IMapsApiClient>().Should().BeOfType<OtherClient>();
        provider.GetRequiredKeyedService<IMapsApiClient>(Primary).Should().BeOfType<MapsApiClient>();
    }

    // ===================== HttpClient timeout =====================
    [Theory]
    [InlineData(5.0)]
    [InlineData(0.1)]
    public void MapsApiClient_ConfiguredRequestTimeout_ReachesItsHttpClient(double seconds) {
        using var provider = TestServices.BuildStrict(TestServices.Create(c => {
            TestServices.ValidConfig(c);
            c.RequestTimeout = TimeSpan.FromSeconds(seconds);
        }));

        var client = (MapsApiClient)provider.GetRequiredService<IMapsApiClient>();

        HttpClientOf(client).Timeout.Should().Be(TimeSpan.FromSeconds(seconds));
    }

    [Fact]
    public void MapsApiClient_InfiniteRequestTimeout_ReachesItsHttpClient() {
        using var provider = TestServices.BuildStrict(TestServices.Create(c => {
            TestServices.ValidConfig(c);
            c.RequestTimeout = Timeout.InfiniteTimeSpan;
        }));

        var client = (MapsApiClient)provider.GetRequiredService<IMapsApiClient>();

        HttpClientOf(client).Timeout.Should().Be(Timeout.InfiniteTimeSpan);
    }

    [Fact]
    public void MinimalConfiguration_ApiKeyOnly_IsValidAndUsesTheDefaultTimeout() {
        using var provider = TestServices.BuildStrict(TestServices.Create(c => c.ApiKey = "only-a-key"));

        var client = (MapsApiClient)provider.GetRequiredService<IMapsApiClient>();

        HttpClientOf(client).Timeout.Should().Be(new MapsConfiguration().RequestTimeout);
    }

    // ===================== validation =====================
    [Theory]
    [InlineData(null, 30.0)]
    [InlineData("", 30.0)]
    [InlineData("   ", 30.0)]
    [InlineData("k", 0.0)]
    [InlineData("k", -5.0)]
    public void ResolvingOptions_InvalidConfiguration_ThrowsOptionsValidationException(string? apiKey, double timeoutSeconds) {
        using var provider = TestServices.BuildStrict(TestServices.Create(c => {
            TestServices.ValidConfig(c);
            c.ApiKey = apiKey!;
            c.RequestTimeout = TimeSpan.FromSeconds(timeoutSeconds);
        }));

        var act = () => provider.GetRequiredService<IOptions<MapsConfiguration>>().Value;

        act.Should().Throw<OptionsValidationException>().Which.Failures.Should().NotBeEmpty();
    }

    [Fact]
    public void BuildServiceProvider_InvalidConfiguration_DoesNotThrowUntilOptionsAreResolved() {
        var services = TestServices.Create(c => { TestServices.ValidConfig(c); c.ApiKey = ""; });

        var build = () => TestServices.BuildStrict(services);

        build.Should().NotThrow("without a host, validation runs on first resolution (see the XML docs)");
    }

    [Fact]
    public void IMapsApiClient_InvalidConfiguration_ThrowsOptionsValidationExceptionOnResolve() {
        using var provider = TestServices.BuildStrict(
            TestServices.Create(c => { TestServices.ValidConfig(c); c.ApiKey = ""; }));

        var act = () => provider.GetRequiredService<IMapsApiClient>();

        act.Should().Throw<OptionsValidationException>();
    }

    [Theory]
    [InlineData("", 30.0)]
    [InlineData("k", 0.0)]
    public async Task HostStart_InvalidConfiguration_ThrowsOptionsValidationException(string apiKey, double timeoutSeconds) {
        var act = () => StartHostAsync(c => {
            TestServices.ValidConfig(c);
            c.ApiKey = apiKey;
            c.RequestTimeout = TimeSpan.FromSeconds(timeoutSeconds);
        });

        await act.Should().ThrowAsync<OptionsValidationException>();
    }

    [Fact]
    public async Task HostStart_ValidConfiguration_StartsAndStops() {
        var act = () => StartHostAsync(TestServices.ValidConfig);
        await act.Should().NotThrowAsync();
    }
}