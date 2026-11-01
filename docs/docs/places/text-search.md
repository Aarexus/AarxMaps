---
sidebar_position: 4
title: Text search
---

# Text search

Text search finds places that match a free-text query such as "pizza in New York". Results come in pages of up to 20.

## Example

```csharp
using AarxMaps.Places.Services.Searching;

var request = new TextRequestBuilder()
    .WithTextQuery("pizza in New York")
    .WithIncludedType("restaurant")
    .WithPageSize(10)
    .Build();

var result = await places.Search.SearchByTextAsync(request, ct);

foreach (var place in result.Places)
    Console.WriteLine($"{place.DisplayName} ({place.PlaceId})");
```

## Options

`TextRequestBuilder`:

| Method | Required | Rule |
| --- | --- | --- |
| `WithTextQuery` | Yes | Not blank; trimmed |
| `WithIncludedType` | No | One place type |
| `WithPageSize` | No | 1 to 20, default 20 |
| `WithPageToken` | No | A previous `NextPageToken` |
| `WithLocationBias` | No | Circle or rectangle |
| `WithLocationRestriction` | No | Rectangle, not with a bias |

- The type comes from Google's [place types](https://developers.google.com/maps/documentation/places/web-service/place-types) table. An unknown type is rejected by Google with a 400 (`MapsInvalidRequestException`).
- Area rules are on [Search areas](search-areas.md).

## Result

`TextSearchResult`:

| Property | Type | Notes |
| --- | --- | --- |
| `Places` | `IReadOnlyList<PlaceSearchInfo>` | This page; empty if nothing matches |
| `NextPageToken` | `string?` | `null` on the last page |

Each `PlaceSearchInfo` has `PlaceId`, `DisplayName` and `Types`, none of them `null`. That's all a search returns; call [Place details](place-details.md) with the `PlaceId` for the rest.

## Paging

Reuse the builder and set the token from the previous page. Everything else stays identical, which Google requires:

```csharp
using AarxMaps.Places.Services.Searching;

var builder = new TextRequestBuilder().WithTextQuery("pizza in New York");
var all = new List<PlaceSearchInfo>();
string? token = null;

do {
    if (token is not null)
        builder.WithPageToken(token);

    var page = await places.Search.SearchByTextAsync(builder.Build(), ct);
    all.AddRange(page.Places);
    token = page.NextPageToken;
} while (token is not null);
```

- Only the page token and the page size may change between pages. Any other difference makes Google reject the request with a 400.
- Each page is a separate, billed request.

## Rules and errors

The builder throws `MapsInvalidRequestException` when:

- `Build()` is called without `WithTextQuery`.
- Both a bias and a restriction are set.
- The page size is outside 1 to 20, or the query, type or page token is blank.

## See also

- [Search areas](search-areas.md)
- [Nearby search](nearby-search.md)
- [Place details](place-details.md)
