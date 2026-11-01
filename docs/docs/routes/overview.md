---
sidebar_position: 1
title: Routes overview
---

# Routes overview

`AarxMaps.Routes` wraps the [Routes API](https://developers.google.com/maps/documentation/routes): directions between waypoints and distance matrices between many origins and destinations. Every request is built with a fluent builder and sent through one injected service.

## Example

Register the module after `AddAarxMaps`:

```csharp title="Program.cs"
using AarxMaps.Core.Extensions;
using AarxMaps.Routes.Common.Extensions;

builder.Services.AddAarxMaps(o =>
    builder.Configuration.GetSection("AarxMaps").Bind(o));
builder.Services.AddAarxMapsRoutes();
```

Then inject `IRoutesService` where you need it:

```csharp title="TravelTimes.cs"
using AarxMaps.Core.Models.Geometry;
using AarxMaps.Routes.Common.Models;
using AarxMaps.Routes.Services;
using AarxMaps.Routes.Services.Directions;

public sealed class TravelTimes(IRoutesService routes) {
    public async Task<TimeSpan?> DriveAsync(
        LatLng from, LatLng to, CancellationToken ct) {
        var request = new RouteRequestBuilder()
            .From(new WaypointBuilder().FromLocation(from).Build())
            .To(new WaypointBuilder().FromLocation(to).Build())
            .Build();

        var result = await routes.Directions.GetRoutesAsync(
            request, RouteDetailLevel.Summary, ct);
        return result.Routes.FirstOrDefault()?.Duration;
    }
}
```

The examples on the other Routes pages assume an injected `IRoutesService routes` and a `CancellationToken ct`, like the class above.

## Services

`IRoutesService` exposes one service per feature. Each is also registered on its own, so you can inject it directly.

| Property | Service | Methods |
| --- | --- | --- |
| `Directions` | `IDirectionsService` | `GetRoutesAsync`, `GetRoutesRawAsync` |
| `DistanceMatrix` | `IDistanceMatrixService` | `GetDistancesAsync` |

## Which feature to use

| You want | Use |
| --- | --- |
| A route from A to B, optionally through stops, with its path and turn-by-turn steps | [Directions](directions.md) |
| Distance and travel time for every pair of many origins and destinations | [Distance matrix](distance-matrix.md) |

Both take [waypoints](waypoints.md), and both apply the same [option rules by travel mode](travel-mode-rules.md).

## Detail levels

`GetRoutesAsync` and `GetDistancesAsync` take a `RouteDetailLevel` right after the request. It decides which fields Google returns. Fields outside the level stay empty, zero or `null`. The default is `Standard`.

| Level | Directions | Distance matrix |
| --- | --- | --- |
| `Summary` | Route totals, labels, description, warnings, travel advisory, localized values, optimized waypoint order | Indices, condition, status, distance, duration, fallback info |
| `Standard` | Summary plus the route polyline, the viewport, and each leg's totals, start and end locations | Summary plus the static duration and localized values |
| `Full` | Summary plus the route polyline, the viewport, the route token, and complete legs with steps and transit details | Standard plus the travel advisory |

Ask for the smallest level that has what you show. For fields the SDK doesn't map, Directions has a [raw JSON](directions.md#raw-json) method.

## Namespaces

| Namespace | Contains |
| --- | --- |
| `AarxMaps.Routes.Common.Extensions` | `AddAarxMapsRoutes` |
| `AarxMaps.Routes.Services` | `IRoutesService` |
| `AarxMaps.Routes.Common.Models` | Waypoints, route modifiers, transit preferences, detail levels, shared enums and shared result types |
| `AarxMaps.Routes.Services.Directions` | Directions builder, request, service and results |
| `AarxMaps.Routes.Services.DistanceMatrix` | Distance matrix builder, request, service and results |

Coordinates are `LatLng` from `AarxMaps.Core.Models.Geometry`. See [Coordinates](../core/coordinates.md).

## Common behavior

- **Builders validate early.** An invalid value throws `MapsInvalidRequestException` from the builder method or `Build()`, before anything is sent.
- **Builders are reusable.** Each `Build()` returns a new request; changing the builder afterwards doesn't affect requests already built. Builders aren't thread-safe.
- **Units are fixed.** Distances are meters (`int`), durations are `TimeSpan`, timestamps are `DateTimeOffset` in UTC, and paths are encoded polyline strings. Localized values hold the same numbers as display text.
- **Zero is omitted.** Google leaves out zero values, so a missing number reads as `0` and a missing flag as `false`. Missing objects are `null`; lists are never `null`, only empty.
- **Unknown enum values.** A value Google returns that the SDK doesn't know reads as `Unknown`.
- **Links are safe.** Every `Uri` in a result is `http` or `https`; anything else becomes `null`.
- **Caching.** With a cache layer, directions results are cached and may be the same instance for every caller, so treat them as read-only. The distance matrix is never cached. See [Caching](../core/caching.md).
- **Language.** Display text uses the `Language` from your configuration, sent as an `Accept-Language` header. See [Known limitations](../known-limitations.md).

## See also

- [Waypoints](waypoints.md)
- [Configuration and registration](../core/configuration.md)
- [Errors](../core/errors.md)
