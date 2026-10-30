---
sidebar_position: 7
title: Coming from DelgadoMaps
---

# Coming from DelgadoMaps

AarxMaps is the new name of D3lg4doMaps, whose packages were `D3lg4doMaps.*` and whose namespaces were `DelgadoMaps.*`. This page maps every public name in D3lg4doMaps 3.0.1 to its AarxMaps 1.0.0 equivalent. A type that isn't listed kept its name and only moved to a new namespace (see the namespace tables).

On 1.x or 2.x? `AddD3lg4doMaps*` is now `AddAarxMaps*`, `D3lg4doMaps.*.Public.*` namespaces follow the namespace tables below, and `RequestTimeout` is a `TimeSpan`.

## Example

The same task in both versions: find two places by text, then get the default route between them.

```csharp title="Before (DelgadoMaps 3.0.1)"
using DelgadoMaps.Core.Extensions;
using DelgadoMaps.Places.Abstractions;
using DelgadoMaps.Places.Extensions;
using DelgadoMaps.Routes.Abstractions;
using DelgadoMaps.Routes.Builders;
using DelgadoMaps.Routes.Extensions;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddDelgadoMaps(opts => opts.ApiKey = "YOUR_API_KEY");
services.AddDelgadoMapsPlaces();
services.AddDelgadoMapsRoutes();

using var provider = services.BuildServiceProvider();
var places = provider.GetRequiredService<IPlacesService>();
var routes = provider.GetRequiredService<IRoutesService>();

var from = await places.Search.SearchByTextAsync("medellin, co");
var to = await places.Search.SearchByTextAsync("bogota, co");

var request = new RouteRequestBuilder()
    .From(new WaypointBuilder().FromPlaceId(from[0].PlaceId!).Build())
    .To(new WaypointBuilder().FromPlaceId(to[0].PlaceId!).Build())
    .Build();

var result = await routes.Directions.GetRoutesAsync(request);
Console.WriteLine(result.BestRoute?.Duration);
```

```csharp title="After (AarxMaps 1.0.0)"
using AarxMaps.Core.Extensions;
using AarxMaps.Places.Common.Extensions;
using AarxMaps.Places.Services;
using AarxMaps.Places.Services.Searching;
using AarxMaps.Routes.Common.Extensions;
using AarxMaps.Routes.Common.Models;
using AarxMaps.Routes.Services;
using AarxMaps.Routes.Services.Directions;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddAarxMaps(opts => opts.ApiKey = "YOUR_API_KEY");
services.AddAarxMapsPlaces();
services.AddAarxMapsRoutes();

using var provider = services.BuildServiceProvider();
var places = provider.GetRequiredService<IPlacesService>();
var routes = provider.GetRequiredService<IRoutesService>();

// In an app, pass the caller's token (for example HttpContext.RequestAborted).
var ct = CancellationToken.None;

var from = await places.Search.SearchByTextAsync(
    new TextRequestBuilder().WithTextQuery("medellin, co").Build(), ct);
var to = await places.Search.SearchByTextAsync(
    new TextRequestBuilder().WithTextQuery("bogota, co").Build(), ct);

var request = new RouteRequestBuilder()
    .From(new WaypointBuilder().FromPlaceId(from.Places[0].PlaceId).Build())
    .To(new WaypointBuilder().FromPlaceId(to.Places[0].PlaceId).Build())
    .Build();

var result = await routes.Directions.GetRoutesAsync(request, ct: ct);
var route = result.Routes.FirstOrDefault(
    r => r.Labels.Contains(RouteLabel.DefaultRoute));
Console.WriteLine(route?.Duration);
```

## Changes that affect most code

- **Cancellation.** Every async method takes a `CancellationToken`. It's optional on the Places and Routes services, and required on `IMapsApiClient`.
- **Requests are built.** Text search and every Details call take a request object from a builder, instead of a string.
- **Coordinates are `LatLng`.** Builders take `new LatLng(latitude, longitude)` instead of separate doubles. Where a radius is involved, the order is now center first, then radius.
- **Durations are `TimeSpan`.** Route, leg, step and matrix durations used to be Google's strings (`"123s"`).
- **No `BestRoute`.** Pick the route whose `Labels` contains `RouteLabel.DefaultRoute`.
- **Nullability changed in places.** Some properties became nullable or non-nullable (for example `ReviewInfo.Text` is now nullable, and `PlaceSearchInfo.PlaceId` is not). Follow the compiler warnings.
- **Enum integer values changed.** `Unknown` was removed from the request enums and moved to the first position in the response enums. If you stored enum values as integers, map them again.

## Packages and registration

| DelgadoMaps | AarxMaps |
| --- | --- |
| `D3lg4doMaps.Core` package | `AarxMaps.Core` package |
| `D3lg4doMaps.Places` package | `AarxMaps.Places` package |
| `D3lg4doMaps.Routes` package | `AarxMaps.Routes` package |
| `AddDelgadoMaps` | `AddAarxMaps` |
| `AddDelgadoMapsMemoryCache` | `AddAarxMapsMemoryCache` |
| `AddDelgadoMapsDistributedCache` | `AddAarxMapsDistributedCache` |
| `AddDelgadoMapsPlaces` | `AddAarxMapsPlaces` |
| `AddDelgadoMapsRoutes` | `AddAarxMapsRoutes` |

The registration methods now live in `AarxMaps.Core.Extensions`, `AarxMaps.Places.Common.Extensions` and `AarxMaps.Routes.Common.Extensions`. The classes that hold them, `DependencyInjection` in each package, are now `MapsServiceCollectionExtensions`, `PlacesServiceCollectionExtensions` and `RoutesServiceCollectionExtensions`.

## Core

Namespaces only change their root, from `DelgadoMaps.Core.*` to `AarxMaps.Core.*`. The one exception is `StreamResponse`, which moved from `DelgadoMaps.Core.Models.Http` to `AarxMaps.Core.Models`.

### Configuration

- `MapsConfiguration.Logging` and `MapsLoggingOptions` → removed. Logging goes through your app's `ILogger` setup. The only output is warnings when the cache fails.
- `MapsCachingOptions.Prefix` default `"d3lg4doMaps"` → `"aarxMaps"`. Entries already in a distributed cache aren't reused; they expire on their own.

### IMapsApiClient and custom requests

- `IMapsJsonSerializer`, `IRequestFactory` → removed (now internal). To read a streamed JSON array, use `SendAndEnumerateAsync`.
- `SendAsync<T>(request)` → `SendAsync<T>(request, ct)`. The token is required.
- `SendStreamAsync(request)` → `SendStreamAsync(request, ct)`. The token is required.
- `MapsApiRequest.BaseUrl` is a `Uri` instead of a `string`.

### LatLng

- No public constructor → `new LatLng(latitude, longitude)`, which validates the ranges.

### Exceptions

- `MapsApiException.Status` (`string`) → `StatusCode` (`HttpStatusCode?`).
- `MapsApiException.RawResponse` → `ResponseBody`.
- Constructors `(message, string status, raw, inner)` → `(message, HttpStatusCode? status, response, inner)`. This only matters if you throw them yourself, for example in test fakes.

### JsonExtensions

- `GetArray` returns `JsonElement.ArrayEnumerator` instead of `IEnumerable<JsonElement>`. It still works with `foreach` and LINQ.
- `GetArrayStringValues` returns `IReadOnlyList<string>` instead of `List<string>`.

## Places

The old namespaces were grouped by kind (`Abstractions`, `Builders`, `Models.*`). The new ones are grouped by feature.

- `AarxMaps.Places.Services`: `IPlacesService`.
- `AarxMaps.Places.Services.Autocomplete`: `IAutocompleteService`, `AutocompleteRequestBuilder`, `AutocompleteRequest`, `PlaceSuggestion`.
- `AarxMaps.Places.Services.Searching`: `ISearchService`, `TextRequestBuilder`, `NearbyRequestBuilder`, `TextRequest`, `NearbyRequest`, `TextSearchResult`, `NearbySearchResult`, `PlaceSearchInfo`.
- `AarxMaps.Places.Services.Details`: `IDetailsService`, `DetailsRequestBuilder`, `PhotoOptionsBuilder`, `DetailsRequest`, `PhotoOptions`, `PlaceDetails` and its parts, photo and review models, `PriceLevel`.
- `AarxMaps.Places.Common.Models.Geometry`: `GeoCircle`, `GeoRectangle`, `LocationBias`, `LocationRestriction`.

### Renamed types

| DelgadoMaps | AarxMaps |
| --- | --- |
| `PlaceSearchResult` | `PlaceSearchInfo` |
| `PhotoRequest` | `PhotoOptions` |
| `PhotoRequestBuilder` | `PhotoOptionsBuilder` |
| `PlacePhoto` | `PlacePhotoInfo` |
| `Review` | `ReviewInfo` |

### ISearchService

- `SearchByTextAsync(string textQuery)` → `SearchByTextAsync(TextRequest, ct)`. Build the request with `new TextRequestBuilder().WithTextQuery(...)`. It returns a `TextSearchResult` (`Places`, `NextPageToken`) instead of a list.
- `SearchByNearbyAsync(NearbyRequest)` → `SearchNearbyAsync(NearbyRequest, ct)`. It returns a `NearbySearchResult` (`Places`) instead of a list.

### IAutocompleteService

- `SuggestPlacesAsync(request)` → `SuggestPlacesAsync(request, ct)`.

### IDetailsService

Every method takes a `DetailsRequest` instead of a place ID. Build it once with `new DetailsRequestBuilder().WithPlaceId(...)` and reuse it for all four methods.

- `GetDetailsAsync(string placeId)` → `GetDetailsAsync(DetailsRequest, ct)`.
- `GetDetailsRawAsync(string placeId, params string[] fields)` → `GetDetailsRawAsync(DetailsRequest, IEnumerable<string> fields, ct)`. Pass the fields as a collection.
- `GetPhotosAsync(string placeId, PhotoRequest?)` → `GetPhotosAsync(DetailsRequest, PhotoOptions?, ct)`. It returns `PlacePhotos` (`Photos`) instead of a list.
- `GetReviewsAsync(string placeId)` → `GetReviewsAsync(DetailsRequest, ct)`.

### Builders

- `AutocompleteRequestBuilder.WithLocationBias(radius, latitude, longitude)` → `WithLocationBias(LatLng center, double radius)`. The argument order changed.
- `NearbyRequestBuilder.WithTypes(types)` → `WithIncludedTypes(types)`. It's now optional.
- `NearbyRequestBuilder.WithLocationRestriction(radius, latitude, longitude)` → `WithLocationRestriction(LatLng center, double radius)`. The argument order changed.
- `PhotoRequestBuilder.WithMaximumPhotos(maxPhotos)` → `PhotoOptionsBuilder.WithMaxPhotos(maxPhotos)`.
- `PhotoRequestBuilder.WithResolution(maxHeight, maxWidth)` → `PhotoOptionsBuilder.WithResolution(maxHeightPx, maxWidthPx)`. The limit went from 4096 to 4800 px.

### Models

- `PlaceSuggestion.EndOffset` is `int?` (`null` when missing) instead of `int` (`-1` when missing).
- `PlaceDetails.PriceLevel` is a `PriceLevel?` enum instead of a `string`.
- `PlaceDetails.RegularOpeningHoursDaysDescriptions` → `PlaceDetails.OpeningHours`.
- `GoogleMapsLinks.WebsiteUri` → `PlaceDetails.WebsiteUri`.
- `GoogleMapsLinks` properties are `Uri?` instead of `string`, and `PlaceDetails.GoogleMapsLinks` is never null now.
- `PlaceReviews.ReviewsUri` is a `Uri?` instead of a `string`.

## Routes

- `AarxMaps.Routes.Services`: `IRoutesService`.
- `AarxMaps.Routes.Services.Directions`: `IDirectionsService`, `RouteRequestBuilder`, `RouteRequest`, `PolylineQuality`, `RouteResult`, `Route`, `RouteLeg`, `RouteStep`, `RouteLabel`, `NavigationInstruction`, `Maneuver`, `MultiModalSegment`, `Viewport`, the leg and step localized values, transit details.
- `AarxMaps.Routes.Services.DistanceMatrix`: `IDistanceMatrixService`, `DistanceRequestBuilder`, `DistanceRequest`, `RouteMatrixOrigin`, `RouteMatrixElement`, `RouteMatrixElementStatus`, `RouteMatrixElementCondition`.
- `AarxMaps.Routes.Common.Models`: `WaypointBuilder`, `Waypoint`, `Location`, `RouteModifiers`, `TransitPreferences`, the shared enums (`TravelMode`, `RoutingPreference`, `TrafficModel`, `Units`, `RouteDetailLevel`, `VehicleEmissionType`, transit and fallback enums), `FallbackInfo`, `Money`, `RouteLocalizedValues`, `RouteTravelAdvisory`.

### Renamed types

| DelgadoMaps | AarxMaps |
| --- | --- |
| `ComputeRoute` | `Route` |
| `Segment` | `MultiModalSegment` |
| `Status` | `RouteMatrixElementStatus` |
| `RouteElementCondition` | `RouteMatrixElementCondition` |
| `RoutingMode` | `FallbackRoutingMode` |
| `Reason` | `FallbackReason` |

### Removed types

- `StepsOverview` → its segments are now `RouteLeg.Segments`.
- `Polyline` → routes, legs and steps have an `EncodedPolyline` string.
- `PolylineEncoding` → polylines are always encoded strings.
- `VehicleInfo` → use the `emissionType` argument of `RouteModifiers`.
- `RouteMatrixDestination` → destinations are plain `Waypoint` values.
- `Speed`, `SpeedInterval`, `RouteLegTravelAdvisory` → not in 1.0.0.

### Services

- `GetRoutesAsync(request, detailLevel)` → `GetRoutesAsync(request, detailLevel, ct)`.
- `GetRoutesRawAsync(request, params string[] fields)` → `GetRoutesRawAsync(request, IEnumerable<string> fields, ct)`. Pass the fields as a collection.
- `GetDistancesAsync(request, detailLevel)` → `GetDistancesAsync(request, detailLevel, ct)`.

### WaypointBuilder

- `FromLocation(latitude, longitude, heading)` → `FromLocation(LatLng location, int? heading)`.
- `WithVia()` → `AsVia()`.
- `WithStopover()` → `AsVehicleStopover()`.

### RouteRequestBuilder and RouteRequest

- `WithPolyline(quality, encoding)` → `WithPolylineQuality(quality)`. `RouteRequest.PolylineEncoding` is gone.
- `OptimizeWaypointOrder()` → `WithOptimizedWaypointOrder()`.

### RouteModifiers and TransitPreferences

- `RouteModifiers.Create().NoTolls().NoHighways()` → `new RouteModifiers(avoidTolls: true, avoidHighways: true)`. The same goes for `NoFerries` and `NoIndoor`.
- `.WithVehicleInfo(new VehicleInfo(type))` → `new RouteModifiers(emissionType: type)`. `RouteModifiers.VehicleInfo` → `RouteModifiers.EmissionType`.
- `new TransitPreferences(List<TransitTravelMode>, preference)` → `new TransitPreferences(IEnumerable<TransitTravelMode>?, preference)`. `null` or empty allows every transit mode.

### DistanceRequestBuilder

- `new RouteMatrixOrigin(waypoint, modifiers)` + `AddOrigin(origin)` → `AddOrigin(waypoint, modifiers)`. `RouteMatrixOrigin` has no public constructor.
- `WithOrigins(IEnumerable<RouteMatrixOrigin>)` → `WithOrigins(IEnumerable<Waypoint>, RouteModifiers?)`. The modifiers apply to every origin in the call.
- `AddDestination(RouteMatrixDestination)` → `AddDestination(Waypoint)`, and the same for `WithDestinations`.

### Results

- `RouteResult.BestRoute` → removed. Use the route whose `Labels` contains `RouteLabel.DefaultRoute`.
- `ComputeRoute.RouteLabels` (`string`) → `Route.Labels` (`RouteLabel` enum).
- `Duration`, `StaticDuration` are `TimeSpan` instead of `string`, on routes, legs, steps and matrix elements.
- `.Polyline.EncodedPolyline` → `.EncodedPolyline`, directly on the route, leg or step.
- `RouteLeg.Overview.Segments` → `RouteLeg.Segments`.
- `Segment.StartIndex`, `EndIndex` → `MultiModalSegment.StepStartIndex`, `StepEndIndex`.
- `RouteLeg.TravelAdvisory`, `RouteStep.TravelAdvisory` and `RouteTravelAdvisory.SpeedReadingIntervals` → removed.
- `NavigationInstruction.Maneuver` is a `Maneuver` enum instead of a `string`.
- `NavigationInstruction.Instruction` → `Instructions`.
- `Money.Units` and `Money.Nanos` → `Money.Amount` (`decimal`).
- `RouteMatrixElement.DistanceMeters`, `OriginIndex` and `DestinationIndex` are `int` instead of `int?`.

### Enums

- `Unknown` was removed from `RoutingPreference`, `TrafficModel`, `Units`, `PolylineQuality`, `TransitTravelMode`, `TransitRoutingPreference` and `VehicleEmissionType`.
- `TravelMode`, `FallbackRoutingMode`, `FallbackReason` and `RouteMatrixElementCondition` keep `Unknown`, now as the first member. An unset value reads as `Unknown`.

## See also

- [Quick start](getting-started/quick-start.md)
- [Configuration and registration](core/configuration.md), [Caching](core/caching.md), [Errors](core/errors.md), [Cancellation and timeouts](core/cancellation.md), [Coordinates](core/coordinates.md), [Custom endpoints](core/custom-endpoints.md)
- [Places overview](places/overview.md), [Autocomplete](places/autocomplete.md), [Text search](places/text-search.md), [Nearby search](places/nearby-search.md), [Place details](places/place-details.md), [Photos and reviews](places/photos-and-reviews.md)
- [Routes overview](routes/overview.md), [Waypoints](routes/waypoints.md), [Directions](routes/directions.md), [Route results](routes/route-results.md), [Distance matrix](routes/distance-matrix.md)
- [Known limitations](known-limitations.md)
