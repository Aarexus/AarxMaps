---
sidebar_position: 6
title: Place details
---

# Place details

Place details returns everything the SDK maps about one place: address, location, contact data, rating, opening hours, links and amenities. You need its place ID, from a search or an autocomplete suggestion.

## Example

```csharp
using AarxMaps.Places.Services.Details;

var request = new DetailsRequestBuilder()
    .WithPlaceId("ChIJN1t_tDeuEmsRUsoyG83frY4")
    .Build();

var details = await places.Details.GetDetailsAsync(request, ct);

Console.WriteLine($"{details.DisplayName}, {details.FormattedAddress}");
Console.WriteLine($"Rating {details.Rating} from {details.UserRatingCount}");
```

## Options

`DetailsRequestBuilder` builds the `DetailsRequest` that every Details method takes, including photos and reviews:

| Method | Required | Rule |
| --- | --- | --- |
| `WithPlaceId` | Yes | Not blank; trimmed |
| `WithSessionToken` | No | Ends an autocomplete session |

Set the session token on exactly one Details call: the one that ends the user's search. See [Autocomplete](autocomplete.md#session-tokens).

## Result

`PlaceDetails`, most-used properties:

| Property | Type | Notes |
| --- | --- | --- |
| `DisplayName` | `string?` | Name in the configured language |
| `FormattedAddress` | `string?` | One-line address |
| `Location` | `LatLng?` | See [Coordinates](../core/coordinates.md) |
| `Rating` | `float?` | Average rating |
| `UserRatingCount` | `int?` | Number of ratings |
| `OpeningHours` | `IReadOnlyList<string>` | One line per weekday |
| `WebsiteUri` | `Uri?` | |
| `InternationalPhoneNumber` | `string?` | |

<details>
<summary>All properties</summary>

| Property | Type |
| --- | --- |
| `PlaceId` | `string` |
| `DisplayName` | `string?` |
| `Types` | `IReadOnlyList<string>` |
| `FormattedAddress` | `string?` |
| `GlobalCode` | `string?` (plus code) |
| `Location` | `LatLng?` |
| `PostalAddress` | `PostalAddress?` |
| `TimeZone` | `string?` (IANA ID) |
| `NationalPhoneNumber` | `string?` |
| `InternationalPhoneNumber` | `string?` |
| `PriceLevel` | `PriceLevel?` |
| `Rating` | `float?` |
| `UserRatingCount` | `int?` |
| `OpeningHours` | `IReadOnlyList<string>` |
| `WebsiteUri` | `Uri?` |
| `GoogleMapsLinks` | `GoogleMapsLinks` |
| `PaymentOptions` | `PaymentOptions?` |
| `ParkingOptions` | `ParkingOptions?` |

</details>

<details>
<summary>Nested types</summary>

`PostalAddress`: `RegionCode`, `PostalCode`, `AdministrativeArea`, `Locality` (all `string?`) and `AddressLines` (`IReadOnlyList<string>`).

`GoogleMapsLinks` is never `null`, but each of its links can be: `GoogleMapsUri`, `DirectionsUri`, `PlaceUri`, `WriteAReviewUri`, `ReviewsUri`, `PhotosUri` (all `Uri?`).

`PaymentOptions`: `AcceptsCreditCards`, `AcceptsDebitCards`, `AcceptsCashOnly`, `AcceptsNfc` (all `bool?`).

`ParkingOptions`: `FreeParkingLot`, `FreeStreetParking` (both `bool?`).

`PriceLevel` (enum): `Free`, `Inexpensive`, `Moderate`, `Expensive`, `VeryExpensive`. `PlaceDetails.PriceLevel` is `null` when Google doesn't report one.

</details>

- Every `Uri` is `http` or `https`; anything else is `null`, so links are safe to render.

## Billing

`GetDetailsAsync` always asks Google for the same field set, which includes contact, rating, opening hours and amenity fields. Google bills that as Place Details Enterprise + Atmosphere, its highest tier. If you need fewer fields, use the raw method below with only those fields to pay a lower tier. See Google's [data fields](https://developers.google.com/maps/documentation/places/web-service/data-fields) page.

## Raw JSON

`GetDetailsRawAsync` returns Google's JSON for the fields you name, including fields the SDK doesn't map:

```csharp
using AarxMaps.Core.Extensions;
using AarxMaps.Places.Services.Details;

var request = new DetailsRequestBuilder().WithPlaceId(placeId).Build();

using var json = await places.Details.GetDetailsRawAsync(
    request, ["displayName", "rating", "editorialSummary"], ct);

var rating = json.RootElement.GetDoubleValue("rating");
var summary = json.RootElement
    .GetObject("editorialSummary")?
    .GetStringValue("text");
```

- Field names are Google's Place fields in camelCase, with dots for nested fields. `"*"` asks for every field, at the highest price.
- Names are trimmed and duplicates removed. A `null` or empty list, or a blank name, throws `MapsInvalidRequestException`; an unknown name is rejected by Google with a 400.
- The `JsonElement` helpers (`GetDoubleValue`, `GetObject`, ...) are described on [Custom endpoints](../core/custom-endpoints.md#json-helpers).
- Responses are cached like other calls, unless the fields include photos (`"photos"`, `"photos.*"` or `"*"`).

:::warning[Dispose the document]
The caller owns the `JsonDocument`. Use `using var json = ...`. Every call returns a new document, even from the cache.
:::

## Rules and errors

- No place with that ID: `MapsNotFoundException` (404).
- A malformed place ID: `MapsInvalidRequestException` (400 from Google).
- `Build()` without `WithPlaceId`, or a blank ID: `MapsInvalidRequestException` from the builder.

## See also

- [Photos and reviews](photos-and-reviews.md)
- [Autocomplete](autocomplete.md)
- [Errors](../core/errors.md)
