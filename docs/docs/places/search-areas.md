---
sidebar_position: 2
title: Search areas
---

# Search areas

Autocomplete, text search and nearby search can be limited to an area. A bias prefers results inside it; a restriction only returns results inside it. The area is a circle or a rectangle, depending on the feature.

## Example

```csharp
using AarxMaps.Core.Models.Geometry;
using AarxMaps.Places.Services.Searching;

var downtown = new LatLng(40.7128, -74.0060);

// Prefer bakeries within 2 km of downtown; others can still appear.
var biased = new TextRequestBuilder()
    .WithTextQuery("bakery")
    .WithLocationBias(downtown, radius: 2_000)
    .Build();

// Only bakeries inside the rectangle.
var restricted = new TextRequestBuilder()
    .WithTextQuery("bakery")
    .WithLocationRestriction(
        southWest: new LatLng(40.70, -74.02),
        northEast: new LatLng(40.80, -73.93))
    .Build();
```

## Bias or restriction

| | Bias | Restriction |
| --- | --- | --- |
| Results inside the area | Ranked higher | Returned |
| Results outside the area | Possible | Never |
| Builder method | `WithLocationBias` | `WithLocationRestriction` |

Use a bias when the area is a hint, such as the user's position. Use a restriction when results outside it are useless, such as a delivery zone.

## Shapes by feature

| Feature | Bias | Restriction |
| --- | --- | --- |
| [Autocomplete](autocomplete.md) | Circle | Circle |
| [Text search](text-search.md) | Circle or rectangle | Rectangle |
| [Nearby search](nearby-search.md) | None | Circle, required |

Each builder only has the overloads its feature accepts, so an unsupported shape doesn't compile.

## Circles

`WithLocationBias(center, radius)` and `WithLocationRestriction(center, radius)` take a `LatLng` center and a radius in meters.

- The radius must be greater than 0 and at most 50,000 m.
- A `null` center, a radius out of range, or a `NaN` radius throws `MapsInvalidRequestException`.

## Rectangles

`WithLocationBias(southWest, northEast)` and `WithLocationRestriction(southWest, northEast)` take the south-west and north-east corners.

- The south-west latitude must not be greater than the north-east latitude.
- Longitudes aren't ordered. A south-west longitude greater than the north-east one means the rectangle crosses the 180th meridian. For example, south-west `(-21, 177)` and north-east `(-12, -178)` cover Fiji.
- A `null` corner or swapped latitudes throws `MapsInvalidRequestException`.

## Rules and errors

- A bias and a restriction on the same builder: `Build()` throws `MapsInvalidRequestException`.
- Calling the same method twice keeps the last area.
- Coordinates out of range fail earlier, when you create the `LatLng`. See [Coordinates](../core/coordinates.md).

## See also

- [Autocomplete](autocomplete.md)
- [Text search](text-search.md)
- [Nearby search](nearby-search.md)
