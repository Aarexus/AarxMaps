---
sidebar_position: 4
title: Route results
---

# Route results

`GetRoutesAsync` returns a `RouteResult`. It holds one or more routes; each route is split into legs, each leg into steps, and transit steps carry their line, stops and times. How much of this is filled depends on the [detail level](overview.md#detail-levels).

## Example

Here `request` is a `RouteRequest` built as on [Directions](directions.md):

```csharp
using AarxMaps.Routes.Common.Models;
using AarxMaps.Routes.Services.Directions;

var result = await routes.Directions.GetRoutesAsync(
    request, RouteDetailLevel.Full, ct);

var main = result.Routes.FirstOrDefault(
    r => r.Labels.Contains(RouteLabel.DefaultRoute));
if (main is null) {
    return; // no route between the waypoints
}

Console.WriteLine($"{main.DistanceMeters} m, {main.Duration}");
foreach (var leg in main.Legs) {
    foreach (var step in leg.Steps) {
        Console.WriteLine(step.NavigationInstruction?.Instructions);
    }
}
```

:::warning[Name clash in ASP.NET Core]
`Route` also exists as `Microsoft.AspNetCore.Routing.Route`, which ASP.NET Core projects import by default. Use `var`, as above, or add an alias: `using Route = AarxMaps.Routes.Services.Directions.Route;`.
:::

## Route result

| Property | Type | Notes |
| --- | --- | --- |
| `Routes` | `IReadOnlyList<Route>` | Empty when no route exists |
| `FallbackInfo` | `FallbackInfo?` | Set when Google couldn't honor the routing preference |

The main route is the one labeled `RouteLabel.DefaultRoute`, normally the first. With `WithAlternativeRoutes` there are up to three routes in total.

| `RouteLabel` | Meaning |
| --- | --- |
| `DefaultRoute` | The main route |
| `DefaultRouteAlternate` | An alternative to the main route |
| `FuelEfficient` | A route optimized for fuel consumption |
| `ShorterDistance` | A route optimized for travel distance |

## Route

`Route`, most-used properties:

| Property | Type | Notes |
| --- | --- | --- |
| `Labels` | `IReadOnlyList<RouteLabel>` | See above |
| `DistanceMeters` | `int` | |
| `Duration` | `TimeSpan` | With traffic when requested |
| `StaticDuration` | `TimeSpan` | Without traffic |
| `EncodedPolyline` | `string?` | `Standard` and `Full` |
| `Viewport` | `Viewport?` | `Standard` and `Full` |
| `Legs` | `IReadOnlyList<RouteLeg>` | `Standard` and `Full` |
| `LocalizedValues` | `RouteLocalizedValues?` | Display text |

<details>
<summary>All properties</summary>

| Property | Type | Notes |
| --- | --- | --- |
| `Labels` | `IReadOnlyList<RouteLabel>` | |
| `DistanceMeters` | `int` | |
| `Duration` | `TimeSpan` | Equals `StaticDuration` without traffic |
| `StaticDuration` | `TimeSpan` | |
| `Description` | `string?` | For example the main road |
| `Warnings` | `IReadOnlyList<string>` | Show them with the route |
| `Viewport` | `Viewport?` | `Low` (south-west), `High` (north-east) |
| `EncodedPolyline` | `string?` | |
| `Legs` | `IReadOnlyList<RouteLeg>` | |
| `OptimizedWaypointOrder` | `IReadOnlyList<int>` | See [Intermediates](directions.md#intermediates) |
| `TravelAdvisory` | `RouteTravelAdvisory?` | See below |
| `LocalizedValues` | `RouteLocalizedValues?` | |
| `RouteToken` | `string?` | `Full` only |

</details>

`RouteToken` is an opaque token that lets the Google Navigation SDK rebuild this exact route.

## Legs

There is one leg between every two consecutive waypoints that aren't via waypoints. `Standard` fills the totals, the start and end locations and the localized values; `Full` fills everything.

| Property | Type | Notes |
| --- | --- | --- |
| `DistanceMeters` | `int` | |
| `Duration` | `TimeSpan` | With traffic when requested |
| `StaticDuration` | `TimeSpan` | Without traffic |
| `StartLocation` | `Location?` | The waypoint, snapped to the road |
| `EndLocation` | `Location?` | The waypoint, snapped to the road |
| `EncodedPolyline` | `string?` | `Full` only |
| `Steps` | `IReadOnlyList<RouteStep>` | `Full` only |
| `Segments` | `IReadOnlyList<MultiModalSegment>` | `Full`, transit only |
| `LocalizedValues` | `RouteLegLocalizedValues?` | Display text |

## Steps

A step is one navigation instruction, such as a turn.

| Property | Type | Notes |
| --- | --- | --- |
| `DistanceMeters` | `int` | |
| `StaticDuration` | `TimeSpan` | Steps have no traffic duration |
| `StartLocation` | `Location?` | |
| `EndLocation` | `Location?` | |
| `EncodedPolyline` | `string?` | |
| `NavigationInstruction` | `NavigationInstruction?` | What to do |
| `LocalizedValues` | `RouteLegStepLocalizedValues?` | Display text |
| `TravelMode` | `TravelMode` | |
| `TransitDetails` | `TransitDetails?` | Transit steps only |

`NavigationInstruction` has `Instructions` (`string?`, for example "Turn left onto Main St", in the request language) and `Maneuver`, the action to take, for example to pick an icon.

<details>
<summary>All Maneuver values</summary>

`Unknown`, `TurnSlightLeft`, `TurnSharpLeft`, `UturnLeft`, `TurnLeft`, `TurnSlightRight`, `TurnSharpRight`, `UturnRight`, `TurnRight`, `Straight`, `RampLeft`, `RampRight`, `Merge`, `ForkLeft`, `ForkRight`, `Ferry`, `FerryTrain`, `RoundaboutLeft`, `RoundaboutRight`, `Depart`, `NameChange`, `Destination`.

</details>

## Transit

In a transit route, steps alternate between `TravelMode.Walk` and `TravelMode.Transit`. Only transit steps have `TransitDetails`:

```csharp
using AarxMaps.Routes.Services.Directions;

foreach (var step in leg.Steps) {
    if (step.TransitDetails is not { } transit) {
        continue;
    }

    Console.WriteLine(
        $"{transit.Line?.ShortName} to {transit.Headsign}: " +
        $"{transit.DepartureStop?.Name} at {transit.DepartureTimeText}, " +
        $"{transit.StopCount} stops");
}
```

| Property | Type | Notes |
| --- | --- | --- |
| `DepartureStop` | `TransitStop?` | Where to board |
| `ArrivalStop` | `TransitStop?` | Where to leave |
| `DepartureTime` | `DateTimeOffset?` | UTC |
| `ArrivalTime` | `DateTimeOffset?` | UTC |
| `DepartureTimeText` | `string?` | Local time at the stop |
| `ArrivalTimeText` | `string?` | Local time at the stop |
| `Headsign` | `string?` | Direction shown on the vehicle |
| `StopCount` | `int` | Includes the arrival stop |
| `Line` | `TransitLine?` | |

<details>
<summary>Stops, lines and agencies</summary>

`TransitStop`: `Name` (`string?`) and `Location` (`LatLng?`).

`TransitLine`: `Name` and `ShortName` (for example "M7"), `Color` and `TextColor` (hex strings such as "#FF0000"), `VehicleType` (`TransitVehicleType`), `VehicleName` (in the request language), all `string?` except `VehicleType`, and `Agencies` (`IReadOnlyList<TransitAgency>`).

`TransitAgency`: `Name` (`string?`) and `Uri` (`Uri?`, `http` or `https` only). Show the agency names and links wherever you display transit directions.

`TransitVehicleType`: `Unknown`, `Bus`, `CableCar`, `CommuterTrain`, `Ferry`, `Funicular`, `GondolaLift`, `HeavyRail`, `HighSpeedTrain`, `IntercityBus`, `LongDistanceTrain`, `MetroRail`, `Monorail`, `Other`, `Rail`, `ShareTaxi`, `Subway`, `Tram`, `Trolleybus`.

</details>

`Segments` group consecutive steps that share a travel mode, for example a walk and then a bus ride. Each `MultiModalSegment` has `StepStartIndex` and `StepEndIndex` (indexes into `Steps`), a `NavigationInstruction` and a `TravelMode`.

## Localized values

Display text, formatted in the request language and units, for example "12.3 km" or "25 mins". Show it, don't parse it; use the numeric properties for math.

| Type | Used on | Properties |
| --- | --- | --- |
| `RouteLocalizedValues` | Route, matrix element | `Distance`, `Duration`, `StaticDuration`, `TransitFare` |
| `RouteLegLocalizedValues` | Leg | `Distance`, `Duration`, `StaticDuration` |
| `RouteLegStepLocalizedValues` | Step | `Distance`, `StaticDuration` |

All of them are `string?`.

## Advisories and fallback

`RouteTravelAdvisory` (route level only):

- `RouteRestrictionsPartiallyIgnored` (`bool`): Google ignored some requested restrictions, such as route modifiers, because no route satisfied all of them.
- `TransitFare` (`Money?`): the total fare, only when Google knows the fare of every transit step. `Money` has `CurrencyCode` (ISO 4217, `string?`) and `Amount` (`decimal`).

`FallbackInfo` is set only when Google couldn't use the requested routing preference. Durations and traffic data then follow its `RoutingMode`:

| Property | Values |
| --- | --- |
| `RoutingMode` | `FallbackTrafficUnaware`, `FallbackTrafficAware` |
| `Reason` | `ServerError`, `LatencyExceeded` |

Both enums also have `Unknown`.

## Values

- Distances are meters, durations `TimeSpan`, timestamps `DateTimeOffset` in UTC.
- Polylines are strings in Google's [encoded polyline format](https://developers.google.com/maps/documentation/utilities/polylinealgorithm). The SDK has no decoder; see [Known limitations](../known-limitations.md).
- Google leaves out zero values, so they read as `0` or `false`. A missing object is `null`; a list is empty, never `null`.
- `Location` (start and end points) has `LatLng` and an optional `Heading` (`int?`).

## See also

- [Directions](directions.md)
- [Routes overview](overview.md)
- [Known limitations](../known-limitations.md)
