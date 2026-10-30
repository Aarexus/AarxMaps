# AarxMaps

Strongly typed .NET SDK for the Google Maps Platform: **Places API (New)** and **Routes API**, with fluent request builders, dependency injection, typed exceptions and optional response caching.

| Package           | What it gives you                                                                  |
| ----------------- | ---------------------------------------------------------------------------------- |
| `AarxMaps.Core`   | Configuration, DI entry point, HTTP client, exceptions, caching, `LatLng`          |
| `AarxMaps.Places` | Autocomplete, Text Search, Nearby Search, Place Details, Photos, Reviews           |
| `AarxMaps.Routes` | Directions (`computeRoutes`) and Distance Matrix (`computeRouteMatrix`)            |

Runs on **.NET 8** and later (built for .NET 8 and .NET 9; .NET 10+ apps use the .NET 9 build). MIT licensed.

---

## Contents

- [Requirements](#requirements)
- [Installation](#installation)
- [Quick start](#quick-start)
- [Configuration](#configuration)
- [Registration rules](#registration-rules)
- [Caching](#caching)
- [Places](#places)
- [Routes](#routes)
- [Errors](#errors)
- [Cancellation and timeouts](#cancellation-and-timeouts)
- [Calling other Google endpoints](#calling-other-google-endpoints)
- [Usage notes](#usage-notes)
- [Known limitations](#known-limitations)
- [Namespace reference](#namespace-reference)

---

## Requirements

- .NET 8 or later. Apps on .NET 10 or newer use the .NET 9 build; the full test suite passes on .NET 10.
- A Google Cloud API key with the APIs you use enabled: **Places API (New)** for `AarxMaps.Places`, **Routes API** for `AarxMaps.Routes`. The legacy Places API is not used.
- Billing enabled on the Google Cloud project. Every call in this SDK is a billed Google request.

## Installation

```bash
dotnet add package AarxMaps.Places   # pulls in AarxMaps.Core
dotnet add package AarxMaps.Routes   # pulls in AarxMaps.Core
```

Install `AarxMaps.Core` on its own only if you want the HTTP client and caching without a feature module.

## Quick start

ASP.NET Core (`Program.cs`):

```csharp
using AarxMaps.Core.Extensions;
using AarxMaps.Core.Models.Geometry;
using AarxMaps.Places.Common.Extensions;
using AarxMaps.Places.Services;
using AarxMaps.Places.Services.Searching;
using AarxMaps.Routes.Common.Extensions;
using AarxMaps.Routes.Common.Models;
using AarxMaps.Routes.Services;
using AarxMaps.Routes.Services.Directions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAarxMaps(o => o.ApiKey = builder.Configuration["GoogleMaps:ApiKey"]!);
builder.Services.AddAarxMapsPlaces();
builder.Services.AddAarxMapsRoutes();

var app = builder.Build();

app.MapGet("/coffee", async (IPlacesService places, CancellationToken ct) => {
    var request = new TextRequestBuilder()
        .WithTextQuery("coffee in Seattle")
        .WithPageSize(5)
        .Build();

    var result = await places.Search.SearchByTextAsync(request, ct);
    return result.Places.Select(p => new { p.PlaceId, p.DisplayName });
});

app.MapGet("/route", async (IRoutesService routes, CancellationToken ct) => {
    var request = new RouteRequestBuilder()
        .From(new WaypointBuilder().FromLocation(new LatLng(47.6205, -122.3493)).Build())
        .To(new WaypointBuilder().FromAddress("Seattle-Tacoma International Airport").Build())
        .Build();

    var result = await routes.Directions.GetRoutesAsync(request, ct: ct);
    var route = result.Routes.FirstOrDefault();
    return route is null ? Results.NotFound() : Results.Ok(new { route.DistanceMeters, route.Duration });
});

app.Run();
```

Console app or worker without a host:

```csharp
var services = new ServiceCollection()
    .AddAarxMaps(o => o.ApiKey = Environment.GetEnvironmentVariable("GOOGLE_MAPS_API_KEY")!)
    .AddAarxMapsPlaces();

await using var provider = services.BuildServiceProvider();
var places = provider.GetRequiredService<IPlacesService>();
```

You can inject the aggregate services (`IPlacesService`, `IRoutesService`) or the individual ones (`IAutocompleteService`, `ISearchService`, `IDetailsService`, `IDirectionsService`, `IDistanceMatrixService`).

---

## Configuration

`AddAarxMaps(Action<MapsConfiguration>)` configures every module.

| Property         | Type       | Default | Notes                                                                               |
| ---------------- | ---------- | ------- | ----------------------------------------------------------------------------------- |
| `ApiKey`         | `string`   | none    | Required. Sent as the `X-Goog-Api-Key` header, never in the URL.                    |
| `Language`       | `string`   | `"en"`  | Sent as `Accept-Language: {Language}-{Region}`. See [Known limitations](#known-limitations). |
| `Region`         | `string`   | `"US"`  | Only used to build the `Accept-Language` header.                                    |
| `RequestTimeout` | `TimeSpan` | 30 s    | `HttpClient.Timeout`. Must be positive or `Timeout.InfiniteTimeSpan`.               |

Binding from `appsettings.json`:

```json
{
  "AarxMaps": {
    "ApiKey": "...",
    "Language": "es",
    "Region": "CO",
    "RequestTimeout": "00:00:15"
  }
}
```

```csharp
builder.Services.AddAarxMaps(o => builder.Configuration.GetSection("AarxMaps").Bind(o));
```

Keep the API key out of source control (user secrets, environment variables or a secret store). Restrict the key in Google Cloud to the APIs you use.

**Validation:** the configuration is validated when the host starts. A missing `ApiKey` or a non-positive timeout throws `OptionsValidationException` at startup. Without a host (a bare `ServiceCollection`), validation runs the first time a service that needs the options is resolved.

## Registration rules

```csharp
services.AddAarxMaps(o => ...);              // 1. required, exactly once
services.AddAarxMapsMemoryCache(o => ...);   // 2. optional, OR AddAarxMapsDistributedCache, never both
services.AddAarxMapsPlaces();                // 3. any order after step 1
services.AddAarxMapsRoutes();
```

| Mistake                                                     | Result                                     |
| ----------------------------------------------------------- | ------------------------------------------ |
| `AddAarxMapsPlaces()` / `AddAarxMapsRoutes()` before `AddAarxMaps()` | `MapsApiException` naming the missing call |
| A cache method before `AddAarxMaps()`                       | `MapsApiException`                         |
| `AddAarxMaps()` twice                                       | `InvalidOperationException`                |
| Two cache layers (memory + distributed, or either twice)    | `MapsCacheException`                       |
| `AddAarxMapsDistributedCache()` before an `IDistributedCache` is registered | `MapsCacheException`       |

`AddAarxMapsPlaces()` and `AddAarxMapsRoutes()` are idempotent: calling them again is a no-op, and an implementation you registered earlier for one of their interfaces wins.

All module services are **transient** and stateless, so they are safe to inject anywhere, including singletons.

---

## Caching

Caching is off by default. When enabled, it sits in front of the HTTP client and is shared by every module.

```csharp
// In-memory (uses the host's IMemoryCache)
services.AddAarxMapsMemoryCache(o => {
    o.AbsoluteExpiration = TimeSpan.FromMinutes(10);
    o.SlidingExpiration  = TimeSpan.FromMinutes(5);
});

// Distributed (Redis, SQL Server, ...). Register the IDistributedCache first.
services.AddStackExchangeRedisCache(o => o.Configuration = "localhost:6379");
services.AddAarxMapsDistributedCache(o => o.Prefix = "my-app:maps");
```

`MapsCachingOptions`:

| Property             | Default      | Rule                                               |
| -------------------- | ------------ | -------------------------------------------------- |
| `Prefix`             | `"aarxMaps"` | Not empty. Use it to isolate apps on a shared cache. |
| `AbsoluteExpiration` | 30 min       | Positive.                                          |
| `SlidingExpiration`  | 30 min       | Positive and not greater than `AbsoluteExpiration`. |

Options are validated at startup.

**How it behaves**

- The key is a fingerprint of the full request (method, URL, query, payload and the relevant headers, including the field mask and language). Identical requests hit the same entry; anything different is a miss.
- Cache failures (Redis down, serialization error) are logged and treated as a miss. They never fail your request.
- **Memory cache returns the same object instance to every caller.** Treat every returned model as read-only.
- If your app configures `MemoryCacheOptions.SizeLimit`, the memory cache layer silently stores nothing (SDK entries carry no size). Use the distributed cache or drop the size limit.

**Never cached**

- Distance Matrix (`GetDistancesAsync`), because the response is streamed.
- Place photos (`GetPhotosAsync`) and raw Details requests whose fields include photos (`"photos"`, `"photos.*"`, `"*"`), because photo names expire.

**Google's terms.** Google Maps Platform restricts caching of its content. The Places policies forbid storing Places content beyond what the terms allow; place IDs are exempt and can be stored indefinitely. Check the terms for each API you call and set the expirations to match. Enabling a cache layer is your decision and your responsibility.

**Traffic.** A route request without a departure time means "now". A cached traffic-aware duration is as old as the cache entry.

---

## Places

Entry point: `IPlacesService` with `.Autocomplete`, `.Search` and `.Details`. Every request is built with a fluent builder, and every invalid value is rejected with `MapsInvalidRequestException` before anything is sent.

### Coordinates

All coordinates use `LatLng` from `AarxMaps.Core.Models.Geometry`. The constructor order is **latitude, longitude**:

```csharp
var timesSquare = new LatLng(40.7580, -73.9855);
```

Latitude must be in [-90, 90] and longitude in [-180, 180]; `NaN` and out-of-range values throw `MapsInvalidRequestException`. Circle radii must be in (0, 50 000] meters.

### Autocomplete

```csharp
var request = new AutocompleteRequestBuilder()
    .WithInput("pizza nea")
    .WithLocationBias(new LatLng(40.7580, -73.9855), radius: 5000)
    .WithIncludedPrimaryTypes(["restaurant"])
    .WithSessionToken(sessionToken)
    .Build();

IReadOnlyList<PlaceSuggestion> suggestions = await places.Autocomplete.SuggestPlacesAsync(request, ct);
```

| Builder method                                 | Required | Rules                                                     |
| ---------------------------------------------- | -------- | --------------------------------------------------------- |
| `WithInput(string)`                            | yes      | Not empty. Trimmed.                                       |
| `WithIncludedPrimaryTypes(IEnumerable<string>)`| no       | 1 to 5 [place types](https://developers.google.com/maps/documentation/places/web-service/place-types). |
| `WithLocationBias(LatLng center, double radius)` | no     | Circle. Favors the area, doesn't restrict to it.          |
| `WithLocationRestriction(LatLng center, double radius)` | no | Circle. Only results inside the area.                 |
| `WithSessionToken(string)`                     | no       | Up to 36 chars of `A-Z a-z 0-9 - _`. Use `Guid.NewGuid().ToString()`. |

Bias and restriction are mutually exclusive.

`PlaceSuggestion`: `PlaceId`, `Text`, `Types`, and `EndOffset` (`int?`, where the first match of the input ends in `Text`, for highlighting). Google counts the offset in Unicode code points, so it differs from a .NET string index only when the text has characters outside the BMP (for example emoji).

**Session tokens and billing.** Without a token, Google bills every keystroke as a separate request. Use one token for all the autocomplete calls of a single user search, then end the session with exactly one Details call that carries the same token. Start a new token for the next search.

```csharp
var session = Guid.NewGuid().ToString();

var suggestions = await places.Autocomplete.SuggestPlacesAsync(
    new AutocompleteRequestBuilder().WithInput(userInput).WithSessionToken(session).Build(), ct);

// The user picks a suggestion: this call closes the session
var details = await places.Details.GetDetailsAsync(
    new DetailsRequestBuilder().WithPlaceId(suggestions[0].PlaceId).WithSessionToken(session).Build(), ct);
```

### Text Search

```csharp
var request = new TextRequestBuilder()
    .WithTextQuery("vegan restaurants in Bogotá")
    .WithIncludedType("restaurant")
    .WithPageSize(10)
    .Build();

TextSearchResult page = await places.Search.SearchByTextAsync(request, ct);
```

| Builder method                                    | Required | Rules                                         |
| ------------------------------------------------- | -------- | --------------------------------------------- |
| `WithTextQuery(string)`                           | yes      | Not empty. Trimmed.                           |
| `WithIncludedType(string)`                        | no       | One place type.                               |
| `WithPageSize(int)`                               | no       | 1 to 20. Default 20.                          |
| `WithPageToken(string)`                           | no       | The `NextPageToken` of the previous page.     |
| `WithLocationBias(LatLng center, double radius)`  | no       | Circle bias.                                  |
| `WithLocationBias(LatLng southWest, LatLng northEast)` | no  | Rectangle bias.                               |
| `WithLocationRestriction(LatLng southWest, LatLng northEast)` | no | Rectangle only (Google's rule for Text Search). |

Bias and restriction are mutually exclusive. Rectangles need `southWest.Latitude <= northEast.Latitude`; a south-west longitude greater than the north-east one is a rectangle that crosses the 180th meridian.

**Pagination.** `NextPageToken` is `null` on the last page. Every parameter except the page size must stay the same between pages, so reuse the builder:

```csharp
var builder = new TextRequestBuilder().WithTextQuery("bookstores in Madrid");
var page = await places.Search.SearchByTextAsync(builder.Build(), ct);
var all = new List<PlaceSearchInfo>(page.Places);

while (page.NextPageToken is not null) {
    page = await places.Search.SearchByTextAsync(builder.WithPageToken(page.NextPageToken).Build(), ct);
    all.AddRange(page.Places);
}
```

### Nearby Search

```csharp
var request = new NearbyRequestBuilder()
    .WithLocationRestriction(new LatLng(48.8584, 2.2945), radius: 1000)
    .WithIncludedTypes(["cafe", "bakery"])
    .WithMaxResults(10)
    .Build();

NearbySearchResult result = await places.Search.SearchNearbyAsync(request, ct);
```

| Builder method                                     | Required | Rules                         |
| -------------------------------------------------- | -------- | ----------------------------- |
| `WithLocationRestriction(LatLng center, double radius)` | yes | Circle.                       |
| `WithIncludedTypes(IEnumerable<string>)`           | no       | 1 to 50 place types.          |
| `WithMaxResults(int)`                              | no       | 1 to 20. Default 20.          |

Nearby Search returns a single page; there is no token.

Both searches return `PlaceSearchInfo` items with `PlaceId`, `DisplayName` and `Types` only. Use Details for anything else.

### Place Details

All Details methods take a `DetailsRequest`:

```csharp
var request = new DetailsRequestBuilder()
    .WithPlaceId("ChIJN1t_tDeuEmsRUsoyG83frY4")
    .Build();   // .WithSessionToken(token) only on the call that ends an autocomplete session
```

| Method                                          | Returns          | Google calls                               | Cached |
| ----------------------------------------------- | ---------------- | ------------------------------------------ | ------ |
| `GetDetailsAsync(request, ct)`                  | `PlaceDetails`   | 1 Details (Enterprise + Atmosphere tier)   | yes    |
| `GetReviewsAsync(request, ct)`                  | `PlaceReviews`   | 1 Details (Enterprise + Atmosphere tier)   | yes    |
| `GetPhotosAsync(request, options?, ct)`         | `PlacePhotos`    | 1 Details + 1 Place Photos per photo       | never  |
| `GetDetailsRawAsync(request, fields, ct)`       | `JsonDocument`   | 1 Details, billed by the fields you ask for | yes, unless photos are requested |

**`PlaceDetails`** contains `PlaceId`, `DisplayName`, `Types`, `FormattedAddress`, `GlobalCode` (plus code), `Location`, `PostalAddress` (`RegionCode`, `PostalCode`, `AdministrativeArea`, `Locality`, `AddressLines`), `TimeZone` (IANA id), `NationalPhoneNumber`, `InternationalPhoneNumber`, `PriceLevel` (`Free` to `VeryExpensive`), `Rating`, `UserRatingCount`, `OpeningHours` (one localized line per weekday), `WebsiteUri`, `GoogleMapsLinks` (maps, directions, place, write-a-review, reviews and photos URIs), `PaymentOptions` and `ParkingOptions`. Anything Google doesn't return for the place is `null`, or empty for lists.

**`PlaceReviews`** contains up to 5 `ReviewInfo` (Google's limit: `Text`, `LanguageCode`, `Rating`, `AuthorName`, `AuthorUri`, `PublishTime`), plus an optional Gemini `ReviewSummary` and `ReviewsUri`.

**Photos.**

```csharp
var options = new PhotoOptionsBuilder()
    .WithMaxPhotos(3)                                 // 1 to 10, default 10
    .WithResolution(maxHeightPx: 600, maxWidthPx: 800) // 1 to 4800 each, default 480 x 480
    .Build();

PlacePhotos photos = await places.Details.GetPhotosAsync(request, options, ct);
foreach (var photo in photos.Photos)
    Console.WriteLine($"{photo.Uri} by {photo.AuthorName}");
```

Each photo is a separate, billed Place Photos request, sent in parallel. If one fails, the rest are cancelled and the exception is rethrown. `PlacePhotoInfo` gives you `Uri` (the sized image), `Name`, `AuthorName`, `AuthorUri` and the original `HeightPx` / `WidthPx`. Photo names and URIs expire: fetch them when you need them, don't store them.

**Raw JSON.** For fields the SDK doesn't map, or to pay a cheaper tier, ask for exactly the fields you want (names as in Google's [Place fields](https://developers.google.com/maps/documentation/places/web-service/place-details#fieldmask), no `places.` prefix):

```csharp
using var json = await places.Details.GetDetailsRawAsync(request, ["displayName", "rating", "currentOpeningHours"], ct);

var name   = json.RootElement.GetObject("displayName")?.GetStringValue("text");
var rating = json.RootElement.GetDoubleValue("rating");
```

You own the returned `JsonDocument`; dispose it. The `JsonElement` helpers (`GetObject`, `GetStringValue`, `GetInt`, `GetFloat`, `GetDoubleValue`, `GetBool`, `GetArray`, `GetArrayStringValues`) live in `AarxMaps.Core.Extensions` and return `null` or empty instead of throwing when a property is missing or has the wrong type.

**Attribution.** Google requires you to show the photo author with each photo, the `DisclosureText` with a review summary (and offer its `FlagContentUri`), and the author with each review.

---

## Routes

Entry point: `IRoutesService` with `.Directions` and `.DistanceMatrix`.

### Waypoints

Every origin, destination and stop is a `Waypoint`, defined by **exactly one** of a place ID, a location or an address:

```csharp
var byPlaceId  = new WaypointBuilder().FromPlaceId("ChIJN1t_tDeuEmsRUsoyG83frY4").Build();
var byLocation = new WaypointBuilder().FromLocation(new LatLng(-33.8568, 151.2153), heading: 90).Build();
var byAddress  = new WaypointBuilder().FromAddress("Sydney Opera House").Build();   // plus codes work too
```

| Builder method                          | Notes                                                                 |
| --------------------------------------- | --------------------------------------------------------------------- |
| `FromPlaceId(string)`                   | Best option when you have one (for example from Places).              |
| `FromLocation(LatLng, int? heading)`    | Heading 0 to 360 (0 = north). Drive / TwoWheeler only.                |
| `FromAddress(string)`                   | Google geocodes it on every request.                                  |
| `AsVia()`                               | Pass through without stopping (no new leg). Directions intermediates only. |
| `AsVehicleStopover()`                   | Pickup / drop-off point. Requires `FromLocation`. Drive / TwoWheeler only. |
| `WithSideOfRoad()`                      | Stop on the waypoint's side of the road. Drive / TwoWheeler only.     |

### Directions

```csharp
var request = new RouteRequestBuilder()
    .From(new WaypointBuilder().FromAddress("Puerta del Sol, Madrid").Build())
    .To(new WaypointBuilder().FromAddress("Toledo, Spain").Build())
    .WithTravelMode(TravelMode.Drive)
    .WithRoutingPreference(RoutingPreference.TrafficAware)
    .WithRouteModifiers(new RouteModifiers(avoidTolls: true, avoidFerries: true))
    .WithDepartureTime(DateTimeOffset.UtcNow.AddHours(1))
    .WithAlternativeRoutes()
    .WithUnits(Units.Metric)
    .Build();

RouteResult result = await routes.Directions.GetRoutesAsync(request, RouteDetailLevel.Full, ct);

var main = result.Routes.FirstOrDefault(r => r.Labels.Contains(RouteLabel.DefaultRoute));
```

| Builder method                                  | Notes                                                                     |
| ----------------------------------------------- | ------------------------------------------------------------------------- |
| `From(Waypoint)` / `To(Waypoint)`               | Required. Cannot be via waypoints.                                        |
| `AddIntermediate(Waypoint)` / `WithIntermediates(IEnumerable<Waypoint>)` | Up to 25, in visiting order. With intermediates, Google returns no alternatives. |
| `WithTravelMode(TravelMode)`                    | `Drive` (default), `Bicycle`, `Walk`, `TwoWheeler`, `Transit`.            |
| `WithRoutingPreference(RoutingPreference)`      | `TrafficUnaware` (Google's default, cheapest), `TrafficAware`, `TrafficAwareOptimal`. |
| `WithTrafficModel(TrafficModel)`                | `BestGuess`, `Pessimistic`, `Optimistic`.                                 |
| `WithRouteModifiers(RouteModifiers)`            | Avoid tolls / highways / ferries / indoor, vehicle emission type.         |
| `WithTransitPreferences(TransitPreferences)`    | Allowed transit kinds and less walking vs fewer transfers.                |
| `WithDepartureTime(DateTimeOffset)` / `WithArrivalTime(DateTimeOffset)` | Mutually exclusive. Sent in UTC, whole seconds.   |
| `WithPolylineQuality(PolylineQuality)`          | `Overview` (default) or `HighQuality`.                                    |
| `WithUnits(Units)`                              | `Metric` / `Imperial`. Affects display text only, never meters or durations. |
| `WithAlternativeRoutes()`                       | Up to 2 extra routes.                                                     |
| `WithOptimizedWaypointOrder()`                  | Google reorders intermediates. Result in `Route.OptimizedWaypointOrder`. Higher billing rate. |

**Detail level** (the second argument) picks what Google returns and what you're billed for:

| Level              | Filled on `Route`                                                                                              |
| ------------------ | -------------------------------------------------------------------------------------------------------------- |
| `Summary`          | `Labels`, `DistanceMeters`, `Duration`, `StaticDuration`, `Description`, `Warnings`, `TravelAdvisory`, `LocalizedValues`, `OptimizedWaypointOrder`, plus `RouteResult.FallbackInfo` |
| `Standard` (default) | Summary + `EncodedPolyline`, `Viewport`, and per leg: distance, durations, start/end location, localized values |
| `Full`             | Summary + `EncodedPolyline`, `Viewport`, `RouteToken`, and complete legs: polylines, `Steps` (navigation instructions, maneuvers, transit details) and `Segments` |

Fields outside the level are empty, zero or `null`.

**Result model**

```
RouteResult
├── Routes: Route[]                     (empty when no route exists; up to 3 with alternatives)
│   ├── Labels, DistanceMeters, Duration, StaticDuration, Description, Warnings
│   ├── EncodedPolyline, Viewport (Low / High), RouteToken
│   ├── OptimizedWaypointOrder          (indices into your intermediates, e.g. [2, 0, 1])
│   ├── TravelAdvisory                  (RouteRestrictionsPartiallyIgnored, TransitFare: Money)
│   ├── LocalizedValues                 ("12.3 km", "25 mins", fare text)
│   └── Legs: RouteLeg[]                (one per pair of consecutive non-via waypoints)
│       ├── DistanceMeters, Duration, StaticDuration, StartLocation, EndLocation, EncodedPolyline
│       ├── Segments: MultiModalSegment[]   (transit: runs of steps with the same travel mode)
│       └── Steps: RouteStep[]
│           ├── DistanceMeters, StaticDuration, Start/EndLocation, EncodedPolyline, TravelMode
│           ├── NavigationInstruction   (Maneuver, Instructions text)
│           └── TransitDetails          (stops, UTC + local times, headsign, stop count, line, agencies)
└── FallbackInfo                        (set when Google couldn't honor the routing preference)
```

- `Duration` includes traffic when a traffic-aware preference was requested; otherwise it equals `StaticDuration`.
- Polylines are Google's [encoded polyline format](https://developers.google.com/maps/documentation/utilities/polylinealgorithm). The SDK doesn't decode them; Google Maps JS and most map libraries accept them directly.
- `LocalizedValues` are display strings. Don't parse them; use the numeric properties.
- When `FallbackInfo` is set, durations reflect `FallbackInfo.RoutingMode`, not what you asked for.

**Transit example**

```csharp
var request = new RouteRequestBuilder()
    .From(new WaypointBuilder().FromAddress("Union Station, Los Angeles").Build())
    .To(new WaypointBuilder().FromAddress("Santa Monica Pier").Build())
    .WithTravelMode(TravelMode.Transit)
    .WithArrivalTime(new DateTimeOffset(2026, 11, 3, 9, 0, 0, TimeSpan.FromHours(-8)))
    .WithTransitPreferences(new TransitPreferences(
        allowedTravelModes: [TransitTravelMode.Rail, TransitTravelMode.Bus],
        routingPreference: TransitRoutingPreference.FewerTransfers))
    .Build();

var result = await routes.Directions.GetRoutesAsync(request, RouteDetailLevel.Full, ct);

foreach (var step in result.Routes[0].Legs[0].Steps.Where(s => s.TransitDetails is not null)) {
    var t = step.TransitDetails!;
    Console.WriteLine($"{t.Line?.ShortName} from {t.DepartureStop?.Name} at {t.DepartureTimeText} ({t.StopCount} stops)");
}
```

Google requires you to show transit agency names and links (`TransitLine.Agencies`) wherever you display transit directions.

**Raw JSON.** `GetRoutesRawAsync(request, fields, ct)` returns Google's response limited to the [field mask](https://developers.google.com/maps/documentation/routes/choose_fields) you pass (for example `"routes.duration"`, `"routes.legs.steps"`). If the request optimizes the waypoint order, include `"routes.optimizedIntermediateWaypointIndex"` or Google rejects it. You own and must dispose the `JsonDocument`.

### Distance Matrix

Computes a route for every origin x destination pair.

```csharp
var warehouses = new[] { "ChIJ...A", "ChIJ...B" }
    .Select(id => new WaypointBuilder().FromPlaceId(id).Build());

var request = new DistanceRequestBuilder()
    .WithOrigins(warehouses, new RouteModifiers(avoidHighways: true))
    .AddDestination(new WaypointBuilder().FromLocation(new LatLng(51.5074, -0.1278)).Build())
    .AddDestination(new WaypointBuilder().FromAddress("Oxford, UK").Build())
    .WithRoutingPreference(RoutingPreference.TrafficAware)
    .Build();

IReadOnlyList<RouteMatrixElement> elements = await routes.DistanceMatrix.GetDistancesAsync(request, ct: ct);

foreach (var e in elements) {
    if (e.Status is not null) {
        Console.WriteLine($"[{e.OriginIndex},{e.DestinationIndex}] failed: {e.Status.Message}");
        continue;
    }
    if (e.Condition == RouteMatrixElementCondition.RouteExists)
        Console.WriteLine($"[{e.OriginIndex},{e.DestinationIndex}] {e.DistanceMeters} m, {e.Duration}");
}
```

| Builder method                                                     | Notes                                                |
| ------------------------------------------------------------------ | ---------------------------------------------------- |
| `AddOrigin(Waypoint, RouteModifiers?)` / `WithOrigins(IEnumerable<Waypoint>, RouteModifiers?)` | At least one. Modifiers apply to every route from that origin. |
| `AddDestination(Waypoint)` / `WithDestinations(IEnumerable<Waypoint>)` | At least one.                                    |
| `WithTravelMode`, `WithRoutingPreference`, `WithTrafficModel`, `WithTransitPreferences`, `WithDepartureTime`, `WithArrivalTime`, `WithUnits` | Same as Directions. |

**Limits** (checked by `Build()`):

- origins x destinations <= **625**, or <= **100** with `TrafficAwareOptimal` or `Transit`.
- At most **50** origins and destinations together given as a place ID or an address (coordinates don't count).
- No via waypoints.

**Results**

- Elements are returned sorted by `OriginIndex`, then `DestinationIndex` (zero-based indices into your request).
- One element failing doesn't fail the call: it carries `Status` (gRPC `Code` and `Message`), and the rest are still returned. `Status` is `null` for successful elements.
- Metrics are meaningful only when `Condition == RouteExists`. An origin equal to its destination has zero distance and duration.

| Level              | Filled on `RouteMatrixElement`                                               |
| ------------------ | ---------------------------------------------------------------------------- |
| `Summary`          | `OriginIndex`, `DestinationIndex`, `Condition`, `Status`, `DistanceMeters`, `Duration`, `FallbackInfo` |
| `Standard` (default) | Summary + `StaticDuration`, `LocalizedValues`                              |
| `Full`             | Standard + `TravelAdvisory`                                                  |

The matrix is streamed from Google and never cached.

### Option rules by travel mode

Both route builders validate these in `Build()` and throw `MapsInvalidRequestException` when broken, so you don't pay for a request Google would reject or silently ignore. An unset travel mode counts as `Drive`.

| Option                                         | Allowed with                                         |
| ---------------------------------------------- | ---------------------------------------------------- |
| `WithRoutingPreference`                        | `Drive`, `TwoWheeler`                                |
| `WithTrafficModel`                             | `Drive` + `RoutingPreference.TrafficAwareOptimal`    |
| `WithArrivalTime`                              | `Transit` (and never together with a departure time) |
| `WithTransitPreferences`                       | `Transit`                                            |
| Waypoint heading, `AsVehicleStopover`, `WithSideOfRoad` | `Drive`, `TwoWheeler`                       |
| `AsVia`                                        | Directions intermediates only, and not with `WithOptimizedWaypointOrder` |
| `WithOptimizedWaypointOrder`                   | At least one intermediate                            |

Not validated (Google enforces them): departure times in the past (only `Transit` accepts them), the transit window of 7 days back and 100 days ahead, and route modifiers that don't apply to the travel mode (Google ignores them: `AvoidTolls`, `AvoidHighways`, `AvoidFerries` and `EmissionType` apply to `Drive` / `TwoWheeler`, `AvoidIndoor` to `Walk`). Avoid options are preferences; check `TravelAdvisory.RouteRestrictionsPartiallyIgnored`.

---

## Errors

Every SDK exception derives from `MapsApiException` (`AarxMaps.Core.Exceptions`), which carries `StatusCode` (`HttpStatusCode?`) and `ResponseBody` (Google's raw error JSON, when there is one).

| Exception                     | When                                                                                  |
| ----------------------------- | ------------------------------------------------------------------------------------- |
| `MapsInvalidRequestException` | A builder rejected a value (no `StatusCode`), or Google answered 400.                 |
| `MapsApiAuthException`        | 401 / 403: missing, invalid or restricted API key, or the API isn't enabled.          |
| `MapsNotFoundException`       | 404: unknown place ID, expired photo.                                                 |
| `MapsRateLimitException`      | 429. `RetryAfter` (`TimeSpan?`) holds Google's `Retry-After` when sent.               |
| `MapsCacheException`          | Cache registration errors only. Runtime cache failures are never thrown.              |
| `MapsApiException`            | Any other status, network failure, `RequestTimeout` expiry, empty or unreadable body, interrupted stream. |

Outside that hierarchy:

- `OperationCanceledException`: your `CancellationToken` was cancelled.
- `ArgumentNullException`: a `null` request object passed to a service method.
- `OptionsValidationException`: invalid `MapsConfiguration` / `MapsCachingOptions` at startup.

```csharp
try {
    var details = await places.Details.GetDetailsAsync(request, ct);
}
catch (MapsNotFoundException) {
    return Results.NotFound();
}
catch (MapsRateLimitException ex) {
    await Task.Delay(ex.RetryAfter ?? TimeSpan.FromSeconds(1), ct);
    // retry
}
catch (MapsApiException ex) {
    logger.LogError(ex, "Google Maps call failed ({Status}): {Body}", ex.StatusCode, ex.ResponseBody);
    throw;
}
```

The SDK doesn't retry on its own. Add your own policy if you need one.

## Cancellation and timeouts

- Every service method takes an optional `CancellationToken ct` as its last parameter. Cancelling it throws `OperationCanceledException`.
- `RequestTimeout` limits each HTTP call. A timeout is reported as `MapsApiException`, not as a cancellation, so you can tell them apart.
- For the Distance Matrix, `ct` also covers reading the streamed response.
- `GetPhotosAsync` sends its photo requests in parallel; cancelling `ct` cancels all of them.

## Calling other Google endpoints

`IMapsApiClient` (`AarxMaps.Core.Abstractions`) is the same gateway the modules use. Inject it to call any Google Maps endpoint with the configured API key, language header, timeout, error mapping and cache:

```csharp
public sealed class AddressValidator(IMapsApiClient client) {
    public async Task<JsonDocument> ValidateAsync(string address, CancellationToken ct) {
        var request = new MapsApiRequest {
            Method   = HttpMethod.Post,
            BaseUrl  = new Uri("https://addressvalidation.googleapis.com/"),
            Endpoint = "v1:validateAddress",
            Payload  = new { address = new { addressLines = new[] { address } } },
            Cacheable = false
        };
        return await client.SendAsync<JsonDocument>(request, ct);   // caller disposes
    }
}
```

| `MapsApiRequest` property | Default  | Notes                                                                 |
| ------------------------- | -------- | --------------------------------------------------------------------- |
| `Method`                  | `GET`    |                                                                       |
| `BaseUrl`, `Endpoint`     | required | Combined into the request URL.                                        |
| `Headers`, `Query`        | `null`   | Extra headers (for example `X-Goog-FieldMask`) and query parameters.  |
| `Payload`                 | `null`   | Serialized as camelCase JSON, `null` properties omitted.              |
| `ApiKeyLocation`          | `Header` | `Header` sends `X-Goog-Api-Key`; `Query` sends `?key=` for endpoints that require it. |
| `Cacheable`               | `true`   | `false` bypasses the cache layer for this request.                    |

- `SendAsync<T>(request, ct)` buffers and deserializes the body (case-insensitive, camelCase). Use a typed model or `JsonDocument` (which you must dispose).
- `SendAndEnumerateAsync<T>(request, ct)` (extension in `AarxMaps.Core.Extensions`) streams a top-level JSON array element by element. Each enumeration sends a new request; it is never cached.
- `SendStreamAsync(request, ct)` returns a `StreamResponse` you own (`await using`). Low-level; prefer the two above.
- Errors are mapped from the HTTP status only. APIs that report errors inside a 200 body (several legacy web services do) are not detected.

## Usage notes

- **Builders** are mutable, not thread-safe, and reusable: each `Build()` returns a new request, and later builder changes don't affect requests already built. Request objects can't be modified after `Build()`.
- **Results** may come from the cache and be shared between callers. Don't mutate them; collections are read-only.
- **Nullability**: response values Google may omit are nullable (`string?`, `float?`, `Uri?`, ...). Lists are never `null`, only empty. Numbers Google omits when zero (distances, durations, counts in Routes) are non-nullable and default to `0`.
- **Unknown enum values**: response enums (`TravelMode`, `Maneuver`, `RouteLabel`, `TransitVehicleType`, ...) map values the SDK doesn't recognize to `Unknown` instead of failing. `TravelMode.Unknown` is rejected in requests.
- **URIs** in responses are always `http` or `https`; anything else is dropped to `null`, so they are safe to render as links.
- **Testing your code**: every service is an interface (`IPlacesService`, `ISearchService`, `IDirectionsService`, ...), so mock them. Builders run without DI or network.

## Known limitations

- **Language and region**: only sent as the `Accept-Language: {Language}-{Region}` header. Google documents `languageCode` / `regionCode` body fields for Places and Routes, which the SDK doesn't send yet, so `Region` has no documented effect on results.
- **Search results** carry only `PlaceId`, `DisplayName` and `Types`. Call Details for the rest.
- **Place Details** requests a fixed field set (Enterprise + Atmosphere tier). Use `GetDetailsRawAsync` to pick fields.
- **Autocomplete** location bias / restriction accept circles only.
- **Routes** doesn't request toll info, traffic on polylines or travel advisories at leg and step level, has no `AvoidTunnels`, and doesn't decode polylines.

## Namespace reference

| Namespace                                   | Types                                                                                        |
| ------------------------------------------- | -------------------------------------------------------------------------------------------- |
| `AarxMaps.Core.Extensions`                  | `AddAarxMaps`, `AddAarxMapsMemoryCache`, `AddAarxMapsDistributedCache`, `SendAndEnumerateAsync`, `JsonElement` helpers |
| `AarxMaps.Core.Configuration`               | `MapsConfiguration`, `MapsCachingOptions`                                                    |
| `AarxMaps.Core.Exceptions`                  | `MapsApiException` and subtypes                                                              |
| `AarxMaps.Core.Models.Geometry`             | `LatLng`                                                                                     |
| `AarxMaps.Core.Abstractions` / `.Models` / `.Enums` | `IMapsApiClient`, `MapsApiRequest`, `StreamResponse`, `ApiKeyLocation`               |
| `AarxMaps.Places.Common.Extensions`         | `AddAarxMapsPlaces`                                                                          |
| `AarxMaps.Places.Services`                  | `IPlacesService`                                                                             |
| `AarxMaps.Places.Services.Autocomplete`     | `IAutocompleteService`, `AutocompleteRequestBuilder`, `AutocompleteRequest`, `PlaceSuggestion` |
| `AarxMaps.Places.Services.Searching`        | `ISearchService`, `TextRequestBuilder`, `NearbyRequestBuilder`, `TextSearchResult`, `NearbySearchResult`, `PlaceSearchInfo` |
| `AarxMaps.Places.Services.Details`          | `IDetailsService`, `DetailsRequestBuilder`, `PhotoOptionsBuilder`, `PlaceDetails`, `PlacePhotos`, `PlaceReviews`, ... |
| `AarxMaps.Places.Common.Models.Geometry`    | `LocationBias`, `LocationRestriction`, `GeoCircle`, `GeoRectangle`                           |
| `AarxMaps.Routes.Common.Extensions`         | `AddAarxMapsRoutes`                                                                          |
| `AarxMaps.Routes.Common.Models`             | `WaypointBuilder`, `Waypoint`, `RouteModifiers`, `TransitPreferences`, `TravelMode`, `RoutingPreference`, `RouteDetailLevel`, `Units`, shared response types |
| `AarxMaps.Routes.Services`                  | `IRoutesService`                                                                             |
| `AarxMaps.Routes.Services.Directions`       | `IDirectionsService`, `RouteRequestBuilder`, `RouteResult`, `Route`, `RouteLeg`, `RouteStep`, transit types |
| `AarxMaps.Routes.Services.DistanceMatrix`   | `IDistanceMatrixService`, `DistanceRequestBuilder`, `RouteMatrixElement`, ...                |

---

## License

MIT. See [LICENSE](https://github.com/Aarexus/AarxMaps/blob/main/LICENSE).

Not affiliated with or endorsed by Google. Use of Google Maps Platform is subject to [Google's terms of service](https://cloud.google.com/maps-platform/terms).
