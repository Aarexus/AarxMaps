---
sidebar_position: 5
title: Distance matrix
---

# Distance matrix

The distance matrix computes the distance and travel time from every origin to every destination in one request. Use it to compare many candidates, for example which depot is closest to each customer. It returns totals only, no paths or steps.

## Example

```csharp
using AarxMaps.Core.Models.Geometry;
using AarxMaps.Routes.Common.Models;
using AarxMaps.Routes.Services.DistanceMatrix;

var north = new WaypointBuilder()
    .FromLocation(new LatLng(37.8044, -122.2712))
    .Build();
var south = new WaypointBuilder()
    .FromLocation(new LatLng(37.3382, -121.8863))
    .Build();
var customer = new WaypointBuilder()
    .FromAddress("1 Market St, San Francisco")
    .Build();

var request = new DistanceRequestBuilder()
    .WithOrigins([north, south])
    .AddDestination(customer)
    .Build();

var elements = await routes.DistanceMatrix.GetDistancesAsync(
    request, ct: ct);

foreach (var element in elements) {
    if (element.Condition == RouteMatrixElementCondition.RouteExists) {
        Console.WriteLine(
            $"{element.OriginIndex} -> {element.DestinationIndex}: " +
            $"{element.DistanceMeters} m, {element.Duration}");
    }
}
```

The detail level is the second parameter of `GetDistancesAsync`, so pass the token by name (`ct: ct`) or pass both. See [Detail levels](overview.md#detail-levels).

## Options

`DistanceRequestBuilder` builds the `DistanceRequest`:

| Method | Required | Rule |
| --- | --- | --- |
| `AddOrigin` | Yes | Appends one origin; optional route modifiers |
| `WithOrigins` | Yes | Replaces all; optional route modifiers for all |
| `AddDestination` | Yes | Appends one destination |
| `WithDestinations` | Yes | Replaces all |
| `WithTravelMode` | No | Default `Drive`; `Unknown` is rejected |
| `WithRoutingPreference` | No | `Drive` and `TwoWheeler` only |
| `WithTrafficModel` | No | `Drive` with `TrafficAwareOptimal` only |
| `WithTransitPreferences` | No | `Transit` only |
| `WithDepartureTime` | No | Default now; not with an arrival time |
| `WithArrivalTime` | No | `Transit` only; not with a departure time |
| `WithUnits` | No | Default from the first origin's country |

- You need at least one origin and one destination, through either the `Add` or the `With` method.
- Route modifiers belong to an origin: `AddOrigin(waypoint, modifiers)` sets them for one row, and `WithOrigins(waypoints, modifiers)` for every row.
- The values (travel modes, routing preferences, modifiers, transit preferences, times) mean the same as for directions. See [Directions](directions.md#options).
- Via waypoints aren't allowed: every waypoint here is an origin or a destination.

## Limits

`Build()` checks Google's size limits:

| Limit | Value |
| --- | --- |
| Origins × destinations | At most 625 |
| ... with `TrafficAwareOptimal` or `Transit` | At most 100 |
| Origins and destinations given as a place ID or an address | At most 50 together |

Waypoints built with `FromLocation` don't count toward the 50.

## Result

`GetDistancesAsync` returns an `IReadOnlyList<RouteMatrixElement>`: one element per origin and destination pair, sorted by `OriginIndex` and then `DestinationIndex`.

| Property | Type | Notes |
| --- | --- | --- |
| `OriginIndex` | `int` | Zero-based index into the origins |
| `DestinationIndex` | `int` | Zero-based index into the destinations |
| `Condition` | `RouteMatrixElementCondition` | Whether a route was found |
| `Status` | `RouteMatrixElementStatus?` | `null` on success |
| `DistanceMeters` | `int` | |
| `Duration` | `TimeSpan` | With traffic when requested |
| `StaticDuration` | `TimeSpan` | `Standard` and `Full` |
| `LocalizedValues` | `RouteLocalizedValues?` | `Standard` and `Full` |
| `TravelAdvisory` | `RouteTravelAdvisory?` | `Full` only |
| `FallbackInfo` | `FallbackInfo?` | Set when Google fell back |

- The metrics are only meaningful when `Condition` is `RouteExists`. The other values are `RouteNotFound` and `Unknown` (also used when the element failed).
- One failed element doesn't fail the others. Its `Status` has `Code`, the gRPC status code (for example 3 for `INVALID_ARGUMENT`, 5 for `NOT_FOUND`), and `Message`, in English. An address Google can't geocode fails only its elements, for example.
- Google leaves out zero values, so an origin equal to its destination has a distance and durations of zero.
- `RouteLocalizedValues`, `RouteTravelAdvisory` and `FallbackInfo` are the same types as on routes; see [Route results](route-results.md#localized-values).

## Streaming and caching

Google streams the matrix element by element. `GetDistancesAsync` reads the whole stream, then returns the sorted list; there is no method that yields elements as they arrive. If you need that, use `SendAndEnumerateAsync` on [Custom endpoints](../core/custom-endpoints.md) with your own request.

- The distance matrix is never cached, even with a cache layer. See [Caching](../core/caching.md#what-is-cached).
- `RequestTimeout` stops at the response headers; reading the stream is limited only by your token. See [Cancellation and timeouts](../core/cancellation.md#the-timeout).

## Rules and errors

- `Build()` throws `MapsInvalidRequestException` with no origins or no destinations, above a limit, for travel-mode conflicts (see [Option rules by travel mode](travel-mode-rules.md)) and for via waypoints.
- `AddOrigin` and `AddDestination` with `null`, and `WithOrigins` and `WithDestinations` with a `null` collection or `null` values, throw `MapsInvalidRequestException`.
- When Google rejects the whole request (400), `GetDistancesAsync` throws `MapsInvalidRequestException`. An undefined enum value throws the same before sending.
- A stream interrupted while reading throws `MapsApiException`. Other errors are on [Errors](../core/errors.md).

## See also

- [Waypoints](waypoints.md)
- [Option rules by travel mode](travel-mode-rules.md)
- [Cancellation and timeouts](../core/cancellation.md)
