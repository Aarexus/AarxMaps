---
sidebar_position: 5
title: Coordinates
---

# Coordinates

Every coordinate in AarxMaps is a `LatLng` (`AarxMaps.Core.Models.Geometry`). Places and Routes take it as input and return it in results.

## Example

```csharp
using AarxMaps.Core.Models.Geometry;

var spaceNeedle = new LatLng(47.6205, -122.3493);   // latitude, longitude

Console.WriteLine($"{spaceNeedle.Latitude}, {spaceNeedle.Longitude}");
```

## Rules

| Argument | Range (decimal degrees) |
| --- | --- |
| `latitude` | -90 to 90 |
| `longitude` | -180 to 180 |

- **Latitude comes first.** GeoJSON and some map libraries use longitude first, so check the order when you copy values. Named arguments make it explicit: `new LatLng(latitude: 47.6205, longitude: -122.3493)`.
- **Invalid values throw at construction.** An out-of-range value, `NaN` or infinity throws `MapsInvalidRequestException`, before any request is sent.
- **A swapped pair can still be valid.** `new LatLng(-122.3, 47.6)` throws, but `new LatLng(-73.98, 40.75)` is a valid point in Antarctica. Range checks only catch some mistakes.
- **Instances are immutable.** `Latitude` and `Longitude` are read-only, so a `LatLng` is safe to share.
- **Equality is by reference.** `LatLng` is a class without value equality; compare `Latitude` and `Longitude` yourself.

## See also

- [Errors](errors.md)
