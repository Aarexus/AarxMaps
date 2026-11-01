---
sidebar_position: 2
title: Caching
---

# Caching

Caching is off by default. Add one cache layer after `AddAarxMaps`, and it applies to every module: identical requests are answered from the cache instead of calling Google again.

## Example

In memory, using the app's `IMemoryCache`:

```csharp title="Program.cs"
using AarxMaps.Core.Extensions;

builder.Services.AddAarxMaps(o =>
    builder.Configuration.GetSection("AarxMaps").Bind(o));
builder.Services.AddAarxMapsMemoryCache(o => {
    o.AbsoluteExpiration = TimeSpan.FromMinutes(10);
    o.SlidingExpiration = TimeSpan.FromMinutes(5);
});
```

Distributed, on any `IDistributedCache` (Redis, SQL Server, ...). Register the `IDistributedCache` first:

```csharp title="Program.cs"
using AarxMaps.Core.Extensions;

builder.Services.AddAarxMaps(o =>
    builder.Configuration.GetSection("AarxMaps").Bind(o));

// From the Microsoft.Extensions.Caching.StackExchangeRedis package.
builder.Services.AddStackExchangeRedisCache(o =>
    o.Configuration = "localhost:6379");
builder.Services.AddAarxMapsDistributedCache(o => o.Prefix = "my-app:maps");
```

## Options

`MapsCachingOptions` (`AarxMaps.Core.Configuration`), validated at startup:

| Property | Default | Rule |
| --- | --- | --- |
| `Prefix` | `"aarxMaps"` | Not blank |
| `AbsoluteExpiration` | 30 min | Positive |
| `SlidingExpiration` | 30 min | Positive, at most `AbsoluteExpiration` |

- `Prefix` starts every cache key. Give each app its own prefix when apps share one Redis instance, especially if their `Language` settings differ: the configured language isn't part of the key.
- An entry is removed when `AbsoluteExpiration` passes, or earlier if nobody reads it for `SlidingExpiration`.
- Sliding expiration is always on. To effectively turn it off, set it equal to `AbsoluteExpiration`. See [Known limitations](../known-limitations.md).

## Memory or distributed

| | Memory | Distributed |
| --- | --- | --- |
| Shared between app instances | No | Yes |
| Hits return | The same object | A new copy |
| Needs first | Nothing | An `IDistributedCache` |

## What is cached

Every request whose response is read in one piece, which covers most Places and Routes calls. The key is a fingerprint of the request: method, URL, query, body and field mask. Any difference means a different entry.

Never cached:

- The distance matrix, because its response is streamed.
- Places photo requests, because photo names expire.
- Custom requests with `Cacheable = false`. See [Custom endpoints](custom-endpoints.md).

## How it behaves

- **Failures are a miss.** If the cache throws (Redis down, an unreadable entry), the SDK logs a warning and calls Google. A cache problem never fails your request.
- **Results are shared.** A memory-cache hit returns the same instance to every caller, so treat results as read-only.
- **Raw JSON is per call.** Methods that return a `JsonDocument` give you a new document on every call, cached or not. You dispose it.
- **Traffic ages.** A route request without a departure time means "now". A cached traffic-aware duration is as old as its cache entry.

:::warning[Google's terms]
Google Maps Platform restricts caching of its content. The Places API policies forbid storing Places content beyond what the terms allow; place IDs are the exception and can be stored indefinitely. Check the [service-specific terms](https://cloud.google.com/maps-platform/terms/maps-service-terms) and the [Places API policies](https://developers.google.com/maps/documentation/places/web-service/policies) for every API you call, and set the expirations to match. Turning a cache on is your decision.
:::

## Rules and errors

All of these fail at registration:

- A cache method before `AddAarxMaps`: `MapsApiException`.
- Two cache layers (memory and distributed, or either one twice): `MapsCacheException`.
- `AddAarxMapsDistributedCache` with no `IDistributedCache` registered yet: `MapsCacheException`.
- Invalid options: `OptionsValidationException` at startup.

:::warning[Shared memory cache with a size limit]
`AddAarxMapsMemoryCache` uses the app's `IMemoryCache`. If your app sets `MemoryCacheOptions.SizeLimit`, AarxMaps entries have no size and are never stored, so caching silently does nothing. Remove the limit or use the distributed cache.
:::

## See also

- [Configuration and registration](configuration.md)
- [Custom endpoints](custom-endpoints.md)
