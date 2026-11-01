---
sidebar_position: 2
title: Waypoints
---

# Waypoints

A waypoint is a point a route starts at, ends at or passes through. Directions and the distance matrix both take `Waypoint` instances, built with `WaypointBuilder`.

## Example

```csharp
using AarxMaps.Core.Models.Geometry;
using AarxMaps.Routes.Common.Models;

var office = new WaypointBuilder()
    .FromPlaceId("ChIJN1t_tDeuEmsRUsoyG83frY4")
    .Build();

var pickup = new WaypointBuilder()
    .FromLocation(new LatLng(40.7580, -73.9855), heading: 90)
    .AsVehicleStopover()
    .Build();

var station = new WaypointBuilder()
    .FromAddress("Grand Central Terminal, New York")
    .Build();
```

A built `Waypoint` is immutable, so you can reuse it in as many requests as you like.

## Sources

Set exactly one source:

| Method | Rule |
| --- | --- |
| `FromPlaceId(placeId)` | Not blank; trimmed |
| `FromLocation(location, heading)` | `location` not `null`; `heading` optional, 0 to 360 |
| `FromAddress(address)` | Not blank; trimmed. An address or a plus code |

- **Location** takes a `LatLng`; see [Coordinates](../core/coordinates.md). The heading is the compass direction of travel (0 is north, 90 is east). It tells Google which side of the road to pick up or drop off at.
- **Address** is geocoded by Google on every request. Prefer a place ID or coordinates when you have one. An address Google can't geocode fails the directions request with a 400; in a distance matrix it fails only the elements that use it.

## Options

| Method | Effect |
| --- | --- |
| `AsVia()` | The route passes through without stopping and without starting a new leg |
| `AsVehicleStopover()` | A pickup or drop-off point: Google avoids roads unsuitable for stopping |
| `WithSideOfRoad()` | The vehicle stops on the side of the road the waypoint is on |

- A via waypoint is only valid as a directions intermediate, and not together with waypoint order optimization.
- `AsVehicleStopover` needs a waypoint built with `FromLocation`.
- A heading, `AsVehicleStopover` and `WithSideOfRoad` only apply to `TravelMode.Drive` and `TravelMode.TwoWheeler`. The request builders check this, not `WaypointBuilder`, because the waypoint doesn't know the travel mode. See [Option rules by travel mode](travel-mode-rules.md).

## Result

`Waypoint` has one property per source, of which exactly one is set, plus the three flags:

| Property | Type | Notes |
| --- | --- | --- |
| `PlaceId` | `string?` | |
| `Location` | `Location?` | `LatLng` plus `Heading` (`int?`) |
| `Address` | `string?` | |
| `Via` | `bool` | |
| `VehicleStopover` | `bool` | |
| `SideOfRoad` | `bool` | |

`Location` is also the type of the start and end points of legs and steps in [route results](route-results.md).

## Using waypoints

| Request | Methods |
| --- | --- |
| [Directions](directions.md) | `From`, `To`, `AddIntermediate`, `WithIntermediates` |
| [Distance matrix](distance-matrix.md) | `AddOrigin`, `WithOrigins`, `AddDestination`, `WithDestinations` |

## Rules and errors

All of these throw `MapsInvalidRequestException`:

- `FromPlaceId` or `FromAddress` with a blank value, `FromLocation` with a `null` location or a heading outside 0 to 360.
- `Build()` with no source, with more than one source, or with `AsVehicleStopover` on a waypoint that isn't a location.
- A request builder's `Build()` with a via origin or destination, or with a heading, stopover or side of road in a travel mode other than `Drive` or `TwoWheeler`.

## See also

- [Directions](directions.md)
- [Distance matrix](distance-matrix.md)
- [Option rules by travel mode](travel-mode-rules.md)
