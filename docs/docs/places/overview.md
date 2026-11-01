---
sidebar_position: 1
title: Places overview
---

# Places overview

`AarxMaps.Places` wraps the [Places API (New)](https://developers.google.com/maps/documentation/places/web-service/op-overview): autocomplete, text search, nearby search, place details, photos and reviews. Every request is built with a fluent builder and sent through one injected service.

## Example

Register the module after `AddAarxMaps`:

```csharp title="Program.cs"
using AarxMaps.Core.Extensions;
using AarxMaps.Places.Common.Extensions;

builder.Services.AddAarxMaps(o =>
    builder.Configuration.GetSection("AarxMaps").Bind(o));
builder.Services.AddAarxMapsPlaces();
```

Then inject `IPlacesService` where you need it:

```csharp title="PlaceFinder.cs"
using AarxMaps.Places.Services;
using AarxMaps.Places.Services.Searching;

public sealed class PlaceFinder(IPlacesService places) {
    public async Task<string?> FindIdAsync(string query, CancellationToken ct) {
        var request = new TextRequestBuilder()
            .WithTextQuery(query)
            .WithPageSize(1)
            .Build();

        var result = await places.Search.SearchByTextAsync(request, ct);
        return result.Places.Count > 0 ? result.Places[0].PlaceId : null;
    }
}
```

The examples on the other Places pages assume an injected `IPlacesService places` and a `CancellationToken ct`, like the class above.

## Services

`IPlacesService` exposes one service per feature. Each is also registered on its own, so you can inject it directly.

| Property | Service | Methods |
| --- | --- | --- |
| `Autocomplete` | `IAutocompleteService` | `SuggestPlacesAsync` |
| `Search` | `ISearchService` | `SearchByTextAsync`, `SearchNearbyAsync` |
| `Details` | `IDetailsService` | `GetDetailsAsync`, `GetDetailsRawAsync`, `GetPhotosAsync`, `GetReviewsAsync` |

## Which feature to use

| You want | Use |
| --- | --- |
| Suggestions while the user types | [Autocomplete](autocomplete.md) |
| Places that match a free-text query | [Text search](text-search.md) |
| Places of some types inside a circle | [Nearby search](nearby-search.md) |
| Everything about one place | [Place details](place-details.md) |
| Photos or reviews of one place | [Photos and reviews](photos-and-reviews.md) |

Autocomplete and both searches return place IDs plus a few basic fields. Pass a place ID to the Details service for the rest.

## Namespaces

| Namespace | Contains |
| --- | --- |
| `AarxMaps.Places.Common.Extensions` | `AddAarxMapsPlaces` |
| `AarxMaps.Places.Services` | `IPlacesService` |
| `AarxMaps.Places.Services.Autocomplete` | Autocomplete builder, request, service and results |
| `AarxMaps.Places.Services.Searching` | Text and nearby search builders, requests, service and results |
| `AarxMaps.Places.Services.Details` | Details, photos and reviews builders, requests, service and results |
| `AarxMaps.Places.Common.Models.Geometry` | The search area types on built requests |

Coordinates are `LatLng` from `AarxMaps.Core.Models.Geometry`. See [Coordinates](../core/coordinates.md).

## Common behavior

- **Builders validate early.** An invalid value throws `MapsInvalidRequestException` from the builder method or `Build()`, before anything is sent.
- **Builders are reusable.** Each `Build()` returns a new request; changing the builder afterwards doesn't affect requests already built. Builders aren't thread-safe.
- **Missing values are null.** A value Google doesn't return is `null`; lists are never `null`, only empty.
- **Links are safe.** Every `Uri` in a result is `http` or `https`; anything else becomes `null`.
- **Results may be shared.** With a cache layer, a cached result can be the same instance for every caller. Treat results as read-only. See [Caching](../core/caching.md), including which Places content Google lets you store.
- **Language.** Results use the `Language` from your configuration, sent as an `Accept-Language` header. See [Known limitations](../known-limitations.md).

## See also

- [Search areas](search-areas.md)
- [Configuration and registration](../core/configuration.md)
- [Errors](../core/errors.md)
