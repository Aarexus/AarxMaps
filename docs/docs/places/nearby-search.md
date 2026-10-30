---
sidebar_position: 5
title: Nearby search
---

# Nearby search

Nearby search returns places inside a circle, optionally limited to some place types. Use it for "cafes around here" without a text query.

## Example

```csharp
using AarxMaps.Core.Models.Geometry;
using AarxMaps.Places.Services.Searching;

var eiffelTower = new LatLng(48.8584, 2.2945);

var request = new NearbyRequestBuilder()
    .WithLocationRestriction(eiffelTower, radius: 1_500)
    .WithIncludedTypes(["cafe", "bakery"])
    .WithMaxResults(10)
    .Build();

var result = await places.Search.SearchNearbyAsync(request, ct);

foreach (var place in result.Places)
    Console.WriteLine($"{place.DisplayName}: {string.Join(", ", place.Types)}");
```

## Options

`NearbyRequestBuilder`:

| Method | Required | Rule |
| --- | --- | --- |
| `WithLocationRestriction` | Yes | Circle, radius up to 50,000 m |
| `WithIncludedTypes` | No | 1 to 50 place types |
| `WithMaxResults` | No | 1 to 20, default 20 |

- Types come from Google's [place types](https://developers.google.com/maps/documentation/places/web-service/place-types) table. A place matches if it has at least one of them.
- Circle rules are on [Search areas](search-areas.md).

## Result

`NearbySearchResult` has one property, `Places` (`IReadOnlyList<PlaceSearchInfo>`), empty when nothing matches. Each `PlaceSearchInfo` has `PlaceId`, `DisplayName` and `Types`, none of them `null`. Call [Place details](place-details.md) with the `PlaceId` for the rest.

There is no paging: a nearby search returns one page of up to 20 places. To find more, use a smaller radius around several centers, or narrow the types.

## Rules and errors

The builder throws `MapsInvalidRequestException` when:

- `Build()` is called without `WithLocationRestriction`.
- The radius is not greater than 0 and at most 50,000 m.
- The type list is empty, has more than 50 items, or contains a blank item.
- The maximum is outside 1 to 20.

An unknown place type is rejected by Google with a 400 (`MapsInvalidRequestException`).

## See also

- [Search areas](search-areas.md)
- [Text search](text-search.md)
- [Place details](place-details.md)
