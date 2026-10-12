using AarxMaps.Core.Configuration; // TODO: adjust namespace of MapsConfiguration
using AarxMaps.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace AarxMaps.Tests.Core.Helpers;

/// <summary>ServiceCollection factory used by the DI tests (AddAarxMaps lives in the namespace you import below).</summary>
internal static class TestServices {
    /// <summary>A fully valid configuration.</summary>
    public static void ValidConfig(MapsConfiguration c) {
        c.ApiKey = TestRequests.DefaultApiKey;
        c.Language = "en";
        c.Region = "US";
        c.RequestTimeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>A fully valid caching configuration.</summary>
    public static void ValidCaching(MapsCachingOptions c) {
        c.Prefix = "aarx";
        c.AbsoluteExpiration = TimeSpan.FromMinutes(10);
        c.SlidingExpiration = TimeSpan.FromMinutes(2);
    }

    /// <summary>New collection with AddAarxMaps already applied (valid config unless told otherwise).</summary>
    public static ServiceCollection Create(Action<MapsConfiguration>? config = null) {
        var services = new ServiceCollection();
        services.AddAarxMaps(config ?? ValidConfig);
        return services;
    }

    /// <summary>Provider with the strictest options: scope validation and build-time graph validation.</summary>
    public static ServiceProvider BuildStrict(IServiceCollection services)
        => services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
}