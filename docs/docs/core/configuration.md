---
sidebar_position: 1
title: Configuration and registration
---

# Configuration and registration

`AddAarxMaps` registers the shared HTTP client and the settings every module uses. Call it once, then add the modules you need.

## Example

```csharp title="Program.cs"
using AarxMaps.Core.Extensions;
using AarxMaps.Places.Common.Extensions;
using AarxMaps.Routes.Common.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAarxMaps(o => {
    o.ApiKey = builder.Configuration["AarxMaps:ApiKey"]!;
    o.Language = "es";
    o.Region = "CO";
    o.RequestTimeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddAarxMapsPlaces();
builder.Services.AddAarxMapsRoutes();
```

## Options

`MapsConfiguration` (`AarxMaps.Core.Configuration`):

| Property | Default | Rule |
| --- | --- | --- |
| `ApiKey` | none | Required, not blank |
| `Language` | `"en"` | None |
| `Region` | `"US"` | None |
| `RequestTimeout` | 30 s | Positive, or infinite |

- `ApiKey` is sent in the `X-Goog-Api-Key` header on every Places and Routes call.
- `Language` and `Region` are sent together as one `Accept-Language` header, for example `es-CO`. Google documents separate language and region request fields that the SDK doesn't send yet, so `Region` has no documented effect. See [Known limitations](../known-limitations.md).
- `RequestTimeout` limits each HTTP call. Use `Timeout.InfiniteTimeSpan` to turn it off. See [Cancellation and timeouts](cancellation.md).

## Binding from configuration

Put the settings in a configuration section and bind it inside the delegate:

```json title="appsettings.json"
{
  "AarxMaps": {
    "Language": "es",
    "Region": "CO",
    "RequestTimeout": "00:00:15"
  }
}
```

```csharp title="Program.cs"
using AarxMaps.Core.Extensions;

builder.Services.AddAarxMaps(o =>
    builder.Configuration.GetSection("AarxMaps").Bind(o));
```

Keep `ApiKey` out of `appsettings.json`. The same section picks it up from other configuration sources:

- **Development:** `dotnet user-secrets set "AarxMaps:ApiKey" "YOUR_API_KEY"`.
- **Production:** an environment variable named `AarxMaps__ApiKey`, or a secret store wired into configuration.

## Validation

The options are validated when the host starts. A blank `ApiKey`, or a `RequestTimeout` that is zero or negative, throws `OptionsValidationException` at startup with every failure listed.

Without a host (a bare `ServiceCollection`), validation runs the first time a service that needs the configuration is resolved.

## Registration order

```csharp
using AarxMaps.Core.Extensions;
using AarxMaps.Places.Common.Extensions;
using AarxMaps.Routes.Common.Extensions;

// 1. Required, exactly once.
services.AddAarxMaps(o => o.ApiKey = apiKey);

// 2. Optional: one cache layer, memory or distributed.
services.AddAarxMapsMemoryCache(o => { });

// 3. Modules, in any order.
services.AddAarxMapsPlaces();
services.AddAarxMapsRoutes();
```

Mistakes fail at registration, before the app runs:

- A module or cache method before `AddAarxMaps`: `MapsApiException` naming the missing call.
- `AddAarxMaps` called twice: `InvalidOperationException`.
- Two cache layers (memory and distributed, or either one twice): `MapsCacheException`.
- `AddAarxMapsDistributedCache` before an `IDistributedCache` is registered: `MapsCacheException`.

`AddAarxMapsPlaces` and `AddAarxMapsRoutes` can be called more than once: later calls do nothing. If you registered your own implementation of one of their interfaces before the call, yours is kept.

## Service lifetime

Module services are registered as transient and use a typed `HttpClient` underneath. Inject them where you use them (endpoints, controllers, scoped services). Don't hold one in a singleton for the app's lifetime: it would keep the same HTTP handler forever and miss DNS changes.

## Writing your own module

`services.IsAarxMapsRegistered()` returns `true` once `AddAarxMaps` has run. Use it in your own registration method to fail early with a clear message when Core is missing, the way `AddAarxMapsPlaces` and `AddAarxMapsRoutes` do.

## See also

- [Caching](caching.md)
- [Errors](errors.md)
- [Cancellation and timeouts](cancellation.md)
