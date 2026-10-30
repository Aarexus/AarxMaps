---
sidebar_position: 6
title: Option rules by travel mode
---

# Option rules by travel mode

Some options only work with some travel modes. Google fails on some of these combinations and silently ignores others, so both request builders reject them in `Build()` with `MapsInvalidRequestException`, before anything is sent. The rules are the same for directions and the distance matrix.

## Example

Here `origin` and `destination` are waypoints:

```csharp
using AarxMaps.Core.Exceptions;
using AarxMaps.Routes.Common.Models;
using AarxMaps.Routes.Services.Directions;

var builder = new RouteRequestBuilder()
    .From(origin)
    .To(destination)
    .WithTravelMode(TravelMode.Walk)
    .WithRoutingPreference(RoutingPreference.TrafficAware);

try {
    var request = builder.Build();
} catch (MapsInvalidRequestException ex) {
    // Routing preferences are for Drive and TwoWheeler only.
    Console.WriteLine(ex.Message);
}
```

## Rules

| Option | `Drive` | `TwoWheeler` | `Bicycle` | `Walk` | `Transit` |
| --- | --- | --- | --- | --- | --- |
| `WithRoutingPreference` | Yes | Yes | No | No | No |
| `WithTrafficModel` | Yes (1) | No | No | No | No |
| `WithTransitPreferences` | No | No | No | No | Yes |
| `WithArrivalTime` | No | No | No | No | Yes |
| `WithDepartureTime` | Yes | Yes | Yes | Yes | Yes |
| Waypoint heading | Yes | Yes | No | No | No |
| `AsVehicleStopover` | Yes | Yes | No | No | No |
| `WithSideOfRoad` | Yes | Yes | No | No | No |

(1) Only together with `RoutingPreference.TrafficAwareOptimal`.

- **Unset means `Drive`.** A request without `WithTravelMode` follows the `Drive` column, because `Drive` is Google's default.
- **Departure or arrival.** You can set one of `WithDepartureTime` and `WithArrivalTime`, not both.
- **Waypoint options** (heading, stopover, side of road) are checked on every waypoint of the request: origin, destination, intermediates, and every matrix origin and destination.
- **`TravelMode.Unknown`** is rejected by `WithTravelMode` itself.

## Rules for any mode

| Rule | Applies to |
| --- | --- |
| No via origin or destination | Both builders |
| No via intermediates with waypoint order optimization | Directions |
| Optimization needs at least one intermediate | Directions |
| At most 25 intermediates | Directions |
| `AsVehicleStopover` only on a waypoint built with `FromLocation` | `WaypointBuilder` |
| Size limits | Distance matrix, see [Limits](distance-matrix.md#limits) |

## Not checked by the builders

These depend on the clock or are ignored by Google without an error, so the builders let them through:

- **Past departure times.** A departure time in the past is only valid for `Transit`.
- **Transit time window.** Transit departure and arrival times must be within 7 days in the past and 100 days in the future.
- **Route modifiers.** Avoiding tolls, highways and ferries, and the emission type, apply to `Drive` and `TwoWheeler`; avoiding indoor applies to `Walk`. Google ignores the others for the mode.
- **Alternatives with intermediates.** `WithAlternativeRoutes` is accepted, but Google returns no alternatives for a request with intermediates.

## See also

- [Directions](directions.md)
- [Distance matrix](distance-matrix.md)
- [Waypoints](waypoints.md)
