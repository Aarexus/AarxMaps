---
sidebar_position: 3
title: Directions
---

# Directions

Directions computes a route from an origin to a destination, optionally through intermediate waypoints, for one travel mode. Use it when you need the path, the legs or the turn-by-turn steps. This page covers the request; the result is on [Route results](route-results.md).

## Example

```csharp
using AarxMaps.Routes.Common.Models;
using AarxMaps.Routes.Services.Directions;

var origin = new WaypointBuilder()
    .FromAddress("Ferry Building, San Francisco")
    .Build();
var destination = new WaypointBuilder()
    .FromAddress("Golden Gate Park, San Francisco")
    .Build();

var request = new RouteRequestBuilder()
    .From(origin)
    .To(destination)
    .WithTravelMode(TravelMode.Drive)
    .WithRoutingPreference(RoutingPreference.TrafficAware)
    .Build();

var result = await routes.Directions.GetRoutesAsync(request, ct: ct);

foreach (var route in result.Routes) {
    var text = route.LocalizedValues;
    Console.WriteLine($"{text?.Distance}, {text?.Duration}");
}
```

The detail level is the second parameter of `GetRoutesAsync`, so pass the token by name (`ct: ct`) or pass both. See [Detail levels](overview.md#detail-levels).

## Options

`RouteRequestBuilder` builds the `RouteRequest`. The common options:

| Method | Required | Rule |
| --- | --- | --- |
| `From` | Yes | Not a via waypoint |
| `To` | Yes | Not a via waypoint |
| `AddIntermediate` | No | Appends one; up to 25 in total |
| `WithIntermediates` | No | Replaces all; up to 25, no `null` values |
| `WithTravelMode` | No | Default `Drive`; `Unknown` is rejected |
| `WithRoutingPreference` | No | `Drive` and `TwoWheeler` only |
| `WithDepartureTime` | No | Default now; not with an arrival time |
| `WithArrivalTime` | No | `Transit` only; not with a departure time |

<details>
<summary>All other options</summary>

| Method | Required | Rule |
| --- | --- | --- |
| `WithRouteModifiers` | No | Roads to avoid, emission type |
| `WithTrafficModel` | No | `Drive` with `TrafficAwareOptimal` only |
| `WithTransitPreferences` | No | `Transit` only |
| `WithPolylineQuality` | No | Default `Overview` |
| `WithUnits` | No | Default from the origin's country |
| `WithAlternativeRoutes` | No | Up to two more routes |
| `WithOptimizedWaypointOrder` | No | Needs intermediates, none of them via |

</details>

Options left unset aren't sent, so Google's defaults apply. Which options each travel mode accepts is on [Option rules by travel mode](travel-mode-rules.md).

### Travel mode and traffic

| Enum | Values |
| --- | --- |
| `TravelMode` | `Drive`, `Bicycle`, `Walk`, `TwoWheeler`, `Transit` |
| `RoutingPreference` | `TrafficUnaware`, `TrafficAware`, `TrafficAwareOptimal` |
| `TrafficModel` | `BestGuess`, `Pessimistic`, `Optimistic` |

- `TrafficUnaware` (Google's default) ignores live traffic: fastest response and lowest price. `TrafficAware` adds live traffic with latency optimizations; `TrafficAwareOptimal` drops most of them and is the slowest. Traffic-aware preferences are billed at a higher rate.
- `TrafficModel` sets the assumptions for time in traffic: `BestGuess` (Google's default), or longer (`Pessimistic`) or shorter (`Optimistic`) than the actual time on most days.
- An unset travel mode means `Drive`. `TravelMode.Unknown` exists for results only; `WithTravelMode` rejects it.

### Times

- The departure time defaults to the moment of the request. Use `WithArrivalTime` instead to arrive by a given time; it is only valid for `Transit`, and you can set only one of the two.
- Times are sent in UTC with whole-second precision, so the offset of your `DateTimeOffset` doesn't matter.
- A departure time in the past is only valid for `Transit`. Transit times must be within 7 days in the past and 100 days in the future. The builder doesn't check either rule, since it depends on the clock.

### Response options

| Enum | Values |
| --- | --- |
| `PolylineQuality` | `Overview` (Google's default, fewer points), `HighQuality` |
| `Units` | `Metric`, `Imperial` |

`Units` only changes display text (localized values and instructions). Distances in meters and durations are never converted. `WithAlternativeRoutes` asks for up to two alternatives besides the main route; Google returns none for requests with intermediates.

## Intermediates

Intermediates are visited in the order you add them. Each one that isn't a via waypoint ends a leg and starts a new one, so a route with two intermediates has three legs.

`WithOptimizedWaypointOrder` lets Google reorder the intermediates to minimize the route's cost. Here `depot` is a `Waypoint` and `stops` a list of them:

```csharp
using AarxMaps.Routes.Common.Models;
using AarxMaps.Routes.Services.Directions;

var request = new RouteRequestBuilder()
    .From(depot)
    .To(depot)
    .WithIntermediates(stops)
    .WithOptimizedWaypointOrder()
    .Build();

var result = await routes.Directions.GetRoutesAsync(
    request, RouteDetailLevel.Summary, ct);

foreach (var index in result.Routes[0].OptimizedWaypointOrder) {
    Console.WriteLine(stops[index].Address);
}
```

- `OptimizedWaypointOrder` holds zero-based indexes into the intermediates you sent. `[2, 0, 1]` means the third intermediate is visited first. It's empty when optimization wasn't requested, and it's included in every detail level.
- Optimization needs at least one intermediate and no via intermediates. It is billed at a higher rate.

## Route modifiers

`RouteModifiers` sets conditions the route should satisfy. It's immutable; pass it to `WithRouteModifiers`:

```csharp
using AarxMaps.Routes.Common.Models;

var modifiers = new RouteModifiers(
    avoidTolls: true,
    avoidFerries: true,
    emissionType: VehicleEmissionType.Electric);
```

| Parameter | Applies to |
| --- | --- |
| `avoidTolls`, `avoidHighways`, `avoidFerries` | `Drive`, `TwoWheeler` |
| `emissionType` | `Drive`, `TwoWheeler` |
| `avoidIndoor` | `Walk` |

- Avoiding is a preference, not a guarantee: Google avoids those roads where reasonable. When it can't satisfy them, the route's `TravelAdvisory.RouteRestrictionsPartiallyIgnored` is `true`.
- `emissionType` (`Gasoline`, `Electric`, `Hybrid`, `Diesel`) lets Google apply emission-based road restrictions.
- Google ignores modifiers that don't apply to the travel mode. The builder doesn't reject them.

## Transit

For `TravelMode.Transit`, `TransitPreferences` narrows the kinds of transit and what to optimize for:

```csharp
using AarxMaps.Routes.Common.Models;
using AarxMaps.Routes.Services.Directions;

var request = new RouteRequestBuilder()
    .From(origin)
    .To(destination)
    .WithTravelMode(TravelMode.Transit)
    .WithArrivalTime(DateTimeOffset.UtcNow.AddHours(2))
    .WithTransitPreferences(new TransitPreferences(
        allowedTravelModes: [TransitTravelMode.Rail],
        routingPreference: TransitRoutingPreference.LessWalking))
    .Build();
```

| Enum | Values |
| --- | --- |
| `TransitTravelMode` | `Bus`, `Subway`, `Train`, `LightRail`, `Rail` |
| `TransitRoutingPreference` | `LessWalking`, `FewerTransfers` |

- `Rail` is the same as `Subway`, `Train` and `LightRail` together. A `null` or empty list allows every kind of transit.
- A `null` routing preference uses Google's default.
- Transit steps, stops, lines and agencies are on [Route results](route-results.md#transit).

## Raw JSON

`GetRoutesRawAsync` returns Google's JSON for the fields you name, including fields the SDK doesn't map, such as transit headways (`routes.legs.steps.transitDetails.headway`):

```csharp
using AarxMaps.Core.Extensions;
using AarxMaps.Routes.Services.Directions;

using var json = await routes.Directions.GetRoutesRawAsync(
    request, ["routes.distanceMeters", "routes.duration"], ct);

foreach (var route in json.RootElement.GetArray("routes")) {
    var meters = route.GetInt("distanceMeters");
    var duration = route.GetStringValue("duration"); // e.g. "1234s"
    Console.WriteLine($"{meters} m, {duration}");
}
```

- Fields use Google's field mask paths, with dots for nested fields. `"*"` asks for every field. See Google's [field mask guide](https://developers.google.com/maps/documentation/routes/choose_fields).
- Values are Google's wire format: durations are strings like `"1234s"`, enums are `SCREAMING_SNAKE_CASE`.
- Names are trimmed and duplicates removed. A `null` or empty list, or a blank name, throws `MapsInvalidRequestException`; an unknown name is rejected by Google with a 400.
- With `WithOptimizedWaypointOrder`, include `routes.optimizedIntermediateWaypointIndex`, or Google rejects the request.
- The `JsonElement` helpers (`GetInt`, `GetArray`, ...) are described on [Custom endpoints](../core/custom-endpoints.md#json-helpers). Responses are cached like other calls.

:::warning[Dispose the document]
The caller owns the `JsonDocument`. Use `using var json = ...`. Every call returns a new document, even from the cache.
:::

## Rules and errors

- `Build()` throws `MapsInvalidRequestException` when the origin or destination is missing, for travel-mode conflicts (see [Option rules by travel mode](travel-mode-rules.md)), for a via origin or destination, for via intermediates with optimization, and for optimization without intermediates.
- More than 25 intermediates throws from `AddIntermediate` or `WithIntermediates`.
- An enum value that isn't defined (for example a cast integer) throws `MapsInvalidRequestException` when the request is sent.
- Google's 400s, such as an address it can't geocode, surface as `MapsInvalidRequestException`. Other errors are on [Errors](../core/errors.md).
- No route between the waypoints isn't an error: `RouteResult.Routes` is empty.

:::warning[Cached traffic]
With a cache layer, identical requests are served from the cache. A request without a departure time means "now", so a cached traffic-aware duration is as old as its cache entry. Set an expiration that fits, see [Caching](../core/caching.md).
:::

## See also

- [Route results](route-results.md)
- [Waypoints](waypoints.md)
- [Option rules by travel mode](travel-mode-rules.md)
