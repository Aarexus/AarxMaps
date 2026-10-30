---
sidebar_position: 7
title: Photos and reviews
---

# Photos and reviews

The Details service also returns a place's photos, ready to show as images, and its reviews with Google's AI summary. Both take the same `DetailsRequest` as [Place details](place-details.md).

## Photos

```csharp
using AarxMaps.Places.Services.Details;

var request = new DetailsRequestBuilder().WithPlaceId(placeId).Build();

var options = new PhotoOptionsBuilder()
    .WithMaxPhotos(3)
    .WithResolution(maxHeightPx: 800, maxWidthPx: 1200)
    .Build();

var result = await places.Details.GetPhotosAsync(request, options, ct);

foreach (var photo in result.Photos)
    Console.WriteLine($"{photo.Uri} by {photo.AuthorName}");
```

### Photo options

`PhotoOptionsBuilder`, optional (pass `null` for the defaults):

| Method | Rule | Default |
| --- | --- | --- |
| `WithMaxPhotos` | 1 to 10 | 10 |
| `WithResolution` | 1 to 4,800 px per side | 480 x 480 |

`WithResolution` takes the height first, then the width. Google scales each image down to fit both, keeping its aspect ratio.

### Photo results

`PlacePhotos` has the `PlaceId` and `Photos`, in Google's order and empty when the place has none. Each `PlacePhotoInfo`:

| Property | Type | Notes |
| --- | --- | --- |
| `Uri` | `Uri` | The image, at the requested size |
| `AuthorName` | `string?` | Show it with the photo |
| `AuthorUri` | `Uri?` | Link to the author |
| `HeightPx` | `int?` | Original height |
| `WidthPx` | `int?` | Original width |
| `Name` | `string` | Google's photo name; it expires |

### How photos are fetched

- One Details call returns the photo names, then one photo call per photo fetches its image link. The photo calls run in parallel, and each one is billed.
- If one photo call fails, the remaining ones are cancelled and the exception is rethrown. Cancelling `ct` cancels all of them.
- Photos are never cached: Google doesn't allow caching photo names, and they expire. Fetch photos when you show them, and don't store the names.
- An expired photo returns `MapsNotFoundException` (404).

## Reviews

```csharp
using AarxMaps.Places.Services.Details;

var request = new DetailsRequestBuilder().WithPlaceId(placeId).Build();
var result = await places.Details.GetReviewsAsync(request, ct);

foreach (var review in result.Reviews)
    Console.WriteLine($"{review.Rating} by {review.AuthorName}: {review.Text}");

if (result.ReviewSummary is { } summary)
    Console.WriteLine($"{summary.Text} ({summary.DisclosureText})");
```

### Review results

`PlaceReviews`:

| Property | Type | Notes |
| --- | --- | --- |
| `Reviews` | `IReadOnlyList<ReviewInfo>` | Up to 5, Google's limit |
| `ReviewSummary` | `ReviewSummary?` | AI summary, when available |
| `ReviewsUri` | `Uri?` | All reviews in Google Maps |
| `DisplayName` | `string?` | Place name |
| `PlaceId` | `string` | |

<details>
<summary>ReviewInfo and ReviewSummary</summary>

`ReviewInfo`: `Text` (`string?`, `null` for a rating-only review), `LanguageCode` (`string?`), `Rating` (`float?`), `AuthorName` (`string?`), `AuthorUri` (`Uri?`), `PublishTime` (`DateTimeOffset?`). Entries with neither text nor rating are skipped.

`ReviewSummary`: `Text` (`string`), `LanguageCode` (`string?`), `DisclosureText` (`string?`, for example "Summarized with Gemini") and `FlagContentUri` (`Uri?`, where users report a problem with the summary).

</details>

- Reviews are billed as Place Details Enterprise + Atmosphere.
- With a cache layer, reviews are cached like other Details calls.

:::warning[Attribution is required]
Google's [Places policies](https://developers.google.com/maps/documentation/places/web-service/policies) require showing the author with each photo and review. Show a review summary together with its `DisclosureText`, and offer its `FlagContentUri` so users can report it.
:::

## Rules and errors

- `WithMaxPhotos` outside 1 to 10, or a `WithResolution` side outside 1 to 4,800: `MapsInvalidRequestException` from the builder.
- No place with that ID: `MapsNotFoundException`, for both methods.
- A session token on the request ends the autocomplete session, as on any Details call. See [Autocomplete](autocomplete.md#session-tokens).

## See also

- [Place details](place-details.md)
- [Caching](../core/caching.md)
