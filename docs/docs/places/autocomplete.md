---
sidebar_position: 3
title: Autocomplete
---

# Autocomplete

Autocomplete returns place suggestions for partial input, so you can show them while the user types. Group the calls of one search with a session token to keep billing low.

## Example

```csharp
using AarxMaps.Places.Services.Autocomplete;
using AarxMaps.Places.Services.Details;

// One token per user search: every keystroke, then one Details call.
var sessionToken = Guid.NewGuid().ToString();

var request = new AutocompleteRequestBuilder()
    .WithInput("pizza nea")
    .WithSessionToken(sessionToken)
    .Build();

var suggestions = await places.Autocomplete.SuggestPlacesAsync(request, ct);
if (suggestions.Count == 0)
    return;

// The user picks a suggestion: end the session with the same token.
var detailsRequest = new DetailsRequestBuilder()
    .WithPlaceId(suggestions[0].PlaceId)
    .WithSessionToken(sessionToken)
    .Build();

var details = await places.Details.GetDetailsAsync(detailsRequest, ct);
```

## Options

`AutocompleteRequestBuilder`:

| Method | Required | Rule |
| --- | --- | --- |
| `WithInput` | Yes | Not blank; trimmed |
| `WithIncludedPrimaryTypes` | No | 1 to 5 place types |
| `WithLocationBias` | No | Circle |
| `WithLocationRestriction` | No | Circle, not with a bias |
| `WithSessionToken` | No | See below |

- Types come from Google's [place types](https://developers.google.com/maps/documentation/places/web-service/place-types) table, for example `"restaurant"` or `"cafe"`. Only places whose primary type matches are suggested.
- Area rules are on [Search areas](search-areas.md).

## Result

`SuggestPlacesAsync` returns an `IReadOnlyList<PlaceSuggestion>`, empty when Google has no suggestions.

| Property | Type | Notes |
| --- | --- | --- |
| `PlaceId` | `string` | Pass it to the Details service |
| `Text` | `string` | Full suggestion text |
| `EndOffset` | `int?` | Where the first match ends in `Text` |
| `Types` | `IReadOnlyList<string>` | Place types |

`EndOffset` counts Unicode code points, while C# strings index UTF-16 code units. They only differ for characters such as emoji.

## Session tokens

- Without a token, Google bills every autocomplete call separately.
- Use the same token for every call of one user search, and a new token for the next search. `Guid.NewGuid().ToString()` is the recommended format.
- End the session with exactly one Details call that uses the same token. Any `IDetailsService` method counts. A token reused after that is billed as if there were none.
- A token is 1 to 36 characters from `A-Z`, `a-z`, `0-9`, `-` and `_`.
- With a cache layer, the token is part of the cache key, so cached suggestions are only reused within the same session.

Google explains the billing in [session pricing](https://developers.google.com/maps/documentation/places/web-service/session-pricing).

## Rules and errors

The builder throws `MapsInvalidRequestException` when:

- `Build()` is called without `WithInput`.
- Both a bias and a restriction are set.
- The type list is empty, has more than 5 items, or contains a blank item.
- The session token is empty, too long, or has other characters.

## See also

- [Search areas](search-areas.md)
- [Place details](place-details.md)
