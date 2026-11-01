---
sidebar_position: 6
title: Known limitations
---

# Known limitations

What AarxMaps 1.0.0 can't do yet, and how to work around it. Anything a builder doesn't expose can still be sent through `IMapsApiClient` with your own request; see [Custom endpoints](core/custom-endpoints.md).

## Core

- **No populated results in your own tests.** Response models (`PlaceDetails`, `RouteResult`, `RouteMatrixElement`, ...) have `internal` setters, so you can't build filled-in results when you mock `IPlacesService` or `IRoutesService`. Workaround: put your own interface in front of the SDK calls and mock that.
- **Sliding expiration can't be turned off.** `MapsCachingOptions.SlidingExpiration` always applies. Workaround: set it equal to `AbsoluteExpiration`. See [Caching](core/caching.md#options).
- **The language isn't part of the cache key.** Apps that share one distributed cache and the same `Prefix` but use different `Language` settings serve each other's responses. Workaround: give each app its own `Prefix`.
- **No built-in retries,** including on 429 and 5xx responses. Workaround: your own retry policy, honoring `MapsRateLimitException.RetryAfter`. See [Errors](core/errors.md#retries).
- **No dedicated timeout exception.** A timeout is a `MapsApiException` whose `InnerException` is an `OperationCanceledException`. See [Cancellation and timeouts](core/cancellation.md#the-timeout).
- **Errors come from the HTTP status only.** On custom endpoints, APIs that report errors inside a 200 response (several legacy web services do) aren't detected. Workaround: check the response body yourself. See [Custom endpoints](core/custom-endpoints.md#errors-and-caching).

## Language and region

Places and Routes don't send Google's `languageCode` and `regionCode` request fields. Only an `Accept-Language` header is sent, built from `Language` and `Region` in your configuration. So `Region` has no documented effect, and `Language` depends on Google honoring the header. See [Configuration](core/configuration.md#options).

## Places

- **Search results are minimal.** Text and nearby search results only carry `PlaceId`, `DisplayName` and `Types`. Workaround: a [Place details](places/place-details.md) call per place you show.
- **Details always requests the same fields.** `GetDetailsAsync` asks for a fixed field set, which Google bills as Place Details Enterprise + Atmosphere. Workaround: `GetDetailsRawAsync` with only the fields you need. See [Raw JSON](places/place-details.md#raw-json).
- **Autocomplete areas are circles only.** Google also accepts rectangles for autocomplete bias and restriction. See [Search areas](places/search-areas.md#shapes-by-feature).
- **Autocomplete match start is missing.** `PlaceSuggestion.EndOffset` only says where the first match ends, not where it starts, so you can't reliably highlight the matched text.
- **Request options the builders don't expose:**
  - [Text search](https://developers.google.com/maps/documentation/places/web-service/reference/rest/v1/places/searchText): `openNow`, `minRating`, `priceLevels`, `rankPreference`, `strictTypeFiltering`, `includePureServiceAreaBusinesses`.
  - [Nearby search](https://developers.google.com/maps/documentation/places/web-service/reference/rest/v1/places/searchNearby): `excludedTypes`, `includedPrimaryTypes`, `excludedPrimaryTypes`, `rankPreference`.
  - [Autocomplete](https://developers.google.com/maps/documentation/places/web-service/reference/rest/v1/places/autocomplete): `includedRegionCodes`, `origin`, `inputOffset`, `includeQueryPredictions`.

## Routes

- **No extra computations.** Google's `extraComputations` isn't sent, so there are no traffic speeds along the polyline and no toll information.
- **Missing route modifiers.** `avoidTunnels` and `tollPasses` aren't available on `RouteModifiers`.
- **Travel advisories are route level only.** There are none on legs or steps.
- **No geocoding results.** Google's `geocodingResults` for address waypoints isn't mapped.
- **Minimal transit details.** No headway and no agency phone number. Workaround: `GetRoutesRawAsync` with those fields. See [Raw JSON](routes/directions.md#raw-json).
- **The distance matrix isn't streamed to you.** `GetDistancesAsync` reads the whole matrix before it returns. Workaround: `SendAndEnumerateAsync` on `IMapsApiClient`. See [Distance matrix](routes/distance-matrix.md#streaming-and-caching).
- **No polyline decoder.** Paths are encoded strings in Google's [encoded polyline format](https://developers.google.com/maps/documentation/utilities/polylinealgorithm); decode them yourself or with a library.
- **Inconsistent `null` handling in the builders.** `RouteRequestBuilder.From(null)` and `To(null)` don't throw; `Build()` then reports a missing origin or destination. `WithRouteModifiers(null)` and `WithTransitPreferences(null)` clear the value instead of throwing. `AddIntermediate`, `AddOrigin` and `AddDestination` do throw on `null`. This will be made consistent in a future version. Workaround: don't pass `null`; only call these methods when you have a value.
