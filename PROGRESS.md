# AarxMaps - Progress

Last updated: 2026-10-22

## Current status

| Phase | Scope          | Status                                                            |
| ----- | -------------- | ----------------------------------------------------------------- |
| 1     | Core module    | Done, merged (#1). 611 tests passing on net8.0 and net9.0 (Phase 2 additions: `Cacheable`, `IsAarxMapsRegistered`). |
| 2     | Places module  | In progress on `Phase2-Places`. Builds on net8.0 + net9.0; tests pending. |
| 3     | Routes module  | Not started. **Does not build** against the new Core.       |
| 4     | Docs + release | Not started. Docusaurus `docs/`, README rewrite, 1.0.0 publish. |

### Build state (2026-10-22)

- **Core**: builds; 611 tests passing on net8.0 and net9.0. Pre-existing analyzer warnings remain (item 12).
- **Places**: builds on both TFMs. Only warning in the real build is CA1724 (item 16). No tests yet.
- **Routes** (not touched): `DistanceMatrixService` depends on the removed `IMapsJsonSerializer`; DI references Core's internal `MapsCoreMarker` and still says `D3lg4doMaps.Core`.

## Next steps

### Phase 2 - Places

**Done (2026-10-20 to 2026-10-22):** Autocomplete, Searching and Details reviewed and fixed; Places builds on both TFMs. Details are in Critical decisions below. Everything left for the phase:

1. **Places tests** - next session. Plan below.
2. **Before release** (not blocking tests):
   - CA1724: rename `DependencyInjection` to `*ServiceCollectionExtensions` in Places and Core (and Routes in Phase 3).
   - Core's pre-existing analyzer warnings (CA1032, CA2007, CA1062, CS1591 in exceptions and `MapsApiClientExtensions`); the zero-warnings rule isn't met by Core yet.
   - Caching docs: Google's terms restrict caching most Places content beyond place IDs; say so on `AddAarxMaps*Cache`.
3. **Optional:** rectangle bias/restriction on `AutocompleteRequestBuilder` (Google supports it); expose autocomplete match start offset; share `LocalizedTextDto` between Searching and Details.

### Places test plan

**Setup.** The test project already references Places, and Places exposes internals to `DynamicProxyGenAssembly2`. Reuse the Core helpers (`TestServices.Create()`, `TestRequests`, `TestServices.ValidConfig`) and put Places-only helpers and JSON fixtures under `AarxMaps.Tests/Places/Helpers/`. Traits: `Package=Places`, `Module=Autocomplete|Searching|Details|Common|Extensions`, `Type=<class>`.

**Two levels:**
- **Unit (NSubstitute `IMapsApiClient`)**: capture the `MapsApiRequest` and return DTOs. Assert method, endpoint, headers, query, `Cacheable` and `ct` propagation, plus mapping.
- **Wire (MockHttp through the real `MapsApiClient`, built via `TestServices.Create().AddAarxMapsPlaces()` plus `services.AddHttpClient(nameof(MapsApiClient)).ConfigurePrimaryHttpMessageHandler(() => mock)`, the pattern in `CacheDependencyInjectionTests.BuildEndToEnd`)**: needed where the public request model is the payload (Autocomplete, Text, Nearby). Assert the serialized JSON body, not the C# object, plus JSON fixtures -> public model. Also the only way to prove the photo API key travels in the `X-Goog-Api-Key` header and never in the URL.

**What to assert** (values verified against Google's reference; see Critical decisions):
- **DI (`AddAarxMapsPlaces`)**: without `AddAarxMaps()` it throws `MapsApiException` naming `AddAarxMaps()`; with Core it registers the 4 services as transient; `BuildStrict` resolves `IPlacesService` and its three properties.
- **Geometry (`GeoFactory` via builders)**: unequal, non-zero lat/lng land in the right fields (regression for the old swap); radius bounds (0, 50 000]; null center or corners; rectangle south-west latitude <= north-east latitude, with antimeridian-crossing longitudes allowed.
- **Shared validation (`RequestValidator` via builders)**: session token 1-36 chars of `[A-Za-z0-9_-]`; required text trimmed, empty or whitespace rejected.
- **Autocomplete**:
  - Request: POST `places:autocomplete`, the field mask from `AutocompleteFactory`, body `{"input":..,"locationBias":{"circle":{"center":{"latitude":..,"longitude":..},"radius":..}}}`, nulls omitted, `sessionToken` sent.
  - Builder: bias + restriction together throws; up to 5 primary types, copied and trimmed (mutating the caller's list after `Build()` doesn't change the request).
  - Mapper: entries without `placePrediction` (query predictions) skipped; `EndOffset` from `text.matches[0].endOffset` or null; `{}` response -> empty list.
- **Searching**:
  - Text: POST `places:searchText`, mask with `nextPageToken`; restriction serializes as `rectangle {low, high}`; bias accepts circle or rectangle; `pageSize` default 20, range 1-20; empty page token rejected.
  - Nearby: POST `places:searchNearby`, mask without `nextPageToken`; `locationRestriction.circle`; `maxResultCount` default 20.
  - Mapper: places missing `id` or `displayName.text` skipped; `Types` is a copy (not the DTO's list).
- **Details**:
  - Endpoint: GET `places/{escaped id}` (an ID containing `/` or `?` is escaped); `sessionToken` as a query parameter only when set.
  - Caching: details and reviews requests `Cacheable = true`; the photos request and every media request `Cacheable = false`; raw requests not cacheable when the fields contain `photos`, `photos.*` or `*`.
  - Raw fields: trimmed and de-duplicated; null, empty or blank field throws `MapsInvalidRequestException`.
  - Photos: media request `GET {photo.name}/media?skipHttpRedirect=true&maxHeightPx=..&maxWidthPx=..`; `MaxPhotos` caps the media calls; nameless photos skipped; when one media call fails, the others see their token cancelled and the exception propagates; defaults 10 photos / 480 px, limits 1-10 and 1-4800.
  - Mapper: `timeZone.id` -> `TimeZone`; `priceLevel` enum strings -> `PriceLevel` (unknown -> null); invalid URI strings -> null (no `UriFormatException`); rating-only reviews kept with `Text = null`; entries with neither text nor rating skipped; `publishTime` parsed, invalid -> null; review summary maps `disclosureText` and `flagContentUri`; absent arrays -> empty, absent objects -> null; `GoogleMapsLinks` never null.
- **Every service**: null request -> `ArgumentNullException`; the caller's `ct` reaches `IMapsApiClient`; Core exceptions (404 -> `MapsNotFoundException`, ...) propagate unchanged.

### Phase 3 - Routes

- Replace the `MapsCoreMarker` check in `AddAarxMapsRoutes` with `services.IsAarxMapsRegistered()` and fix its `D3lg4doMaps.Core` message.

- Migrate `DistanceMatrixService` to `SendAndEnumerateAsync<T>`; drop `IMapsJsonSerializer`.
- Audit against the same checklist as Places (cancellation, validation, mappers, tests, XML docs).

### Phase 4 - Docs + release

- Docusaurus site under `docs/` (workflow already exists).
- Rewrite README with current names (`AddAarxMaps*`, `AarxMaps.*` namespaces); drop the DelgadoMaps breaking-changes section or move it to a "history" page.
- Verify packed `.nupkg` contents (README, XML docs) before tagging `v1.0.0`.

## Critical decisions

- **Fresh package line.** AarxMaps 1.0.0 is unpublished; public API is free to change until release. After 1.0.0, public API changes need a planned breaking release and migration notes.
- **Single HTTP gateway.** Modules only use `IMapsApiClient`; no direct `HttpClient` use outside Core.
- **Caching is a decorator over a keyed primary client**, applied at the HTTP layer and shared by all modules. Streaming (`SendStreamAsync`) is never cached.
- **Cache failures never fail a request** - logged and treated as a miss.
- **Cache keys are versioned request fingerprints**; headers are trimmed, query values are not.
- **Response ownership is explicit**: `SendAsync` owns and disposes; `SendStreamAsync` transfers ownership via `StreamResponse`.
- **Exceptions carry `StatusCode` (`HttpStatusCode`)**, not a string; `MapsRateLimitException` exposes `RetryAfter`.
- **Configuration via `IOptions<T>` + validators + `ValidateOnStart()`**; `RequestTimeout` is a `TimeSpan`.
- **DI order enforced by marker singletons**; double registration of Core or of two cache layers throws.
- **`LatLng` lives in Core** (`AarxMaps.Core.Models.Geometry`) so Places and Routes share one validated coordinate type.
- **Wire DTOs stay internal**; public models are decoupled from Google's JSON schema through mappers.
- **Working model (2026-10-20)**: the user writes the code and Claude reviews each workflow or module. Claude writes code only when explicitly asked.
- **Feature namespaces (2026-10-20, namespace superseded 2026-10-21)**: each Places feature exposes one flat public namespace regardless of subfolders (`Builders/`, `Models/`, `Requests/`). Internals live in `Internal/Services/<Feature>/` with wire DTOs in `Internal/Services/<Feature>/DTOs/`.
- **Public request models may be the wire payload** when their shape matches Google's (e.g. `AutocompleteRequest`, `LocationBias { Circle }`). Responses always go through internal DTOs and mappers.
- **Geometry inputs take `LatLng`**, not separate doubles (`WithLocationBias(LatLng center, double radius)`); radius is validated to (0, 50 000] m in `GeoFactory`.
- **`PlaceSuggestion.EndOffset` is `int?`** (first match only); no sentinel values.
- **Session tokens**: `AutocompleteRequestBuilder.WithSessionToken` and `DetailsRequestBuilder.WithSessionToken` (sent as the `sessionToken` query parameter on Details).
- **`SendAsync<JsonDocument>` is cached like everything else.** `CacheMapsApiClient` stores the raw JSON (same cache key) and returns a new `JsonDocument` per call, which the caller owns and disposes. Unparseable cached JSON is logged and treated as a miss.
- **Namespace rule (2026-10-21)**: public feature types use `AarxMaps.Places.Services.<Feature>` (flat, no `.Builders/.Models/.Requests`). Supersedes the earlier `AarxMaps.Places.<Feature>` note.
- **Rectangle geometry**: `GeoRectangle { Low, High }` (south-west / north-east, serialized as Google's viewport). Builders take `(LatLng southWest, LatLng northEast)`; only latitudes are ordered, so antimeridian-crossing rectangles are allowed.
- **Paging**: `TextRequestBuilder.WithPageSize(int)` and `WithPageToken(string)` are separate; page size 1-20 (`PlacesConstants.MaxPageSize`), default 20 for both Text and Nearby (Google's default).
- **Search DTOs**: one `SearchResponse` (Places + NextPageToken) shared by Text and Nearby; `PlaceSearchInfo.PlaceId`/`DisplayName` are non-null, matching `PlaceSuggestion`.
- **Typed paths deserialize into DTOs, not `JsonDocument` (decided 2026-10-21)**: STJ only materializes declared properties, so response size isn't a reason to avoid DTOs; `JsonDocument` keeps the whole payload, re-parses on every cache hit (raw-JSON caching) and hides shape mismatches (e.g. `timeZone`). `JsonDocument` stays only for `GetDetailsRawAsync`.
- **Details API shape (2026-10-21)**: one `DetailsRequest` (place ID + session token) for every `IDetailsService` method; `GetPhotosAsync(request, PhotoOptions?)`, `GetDetailsRawAsync(request, fields)`. The session token belongs on exactly one call.
- **`MapsApiRequest.Cacheable` (Core, 2026-10-21)**: defaults to `true`; `CacheMapsApiClient` forwards non-cacheable requests untouched. Used for Places photos (details `photos` mask, media calls, raw requests whose fields include photos).
- **Photo media requests send the API key in the header**, not the query string, so it never lands in URL logs.
- **Shared consumer-value checks** (place ID, session token) live in `Internal/Validation/RequestValidator`.
- **Module DI checks use `IsAarxMapsRegistered()` (2026-10-22)**: Core's marker stays `internal`; modules never reference Core internals.
- **Tracking split**: `CLAUDE.md` for stable guidance, `PROGRESS.md` for status/decisions/lessons.

## Lessons learned

**FluentAssertions 8+ is commercially licensed.** Stay on 7.x.

**Partial properties need C# 13** and break the `net8.0` target. Check language features against the lowest TFM.

**`HttpClient.Timeout` only covers up to response headers** on the streaming path; body reads need their own token.

**Distinguish timeout from cancellation**: catch `OperationCanceledException` `when (!ct.IsCancellationRequested)` to report a timeout as `MapsApiException`.

**Shared `IMemoryCache` with `SizeLimit` silently disables caching** (entries have no size). Documented on `AddAarxMapsMemoryCache`.

**Cached instances are shared** across callers; cached models must not be mutated.

**Never cache a disposable result in the memory store.** `MemoryCacheStore` returns the same instance; a caller that disposes it (`using var json = SendAsync<JsonDocument>`) breaks every later hit. Map to immutable DTOs before caching.

**Check payload/response shape against Google's reference, not intuition.** Places (New) wraps geometry (`locationBias.circle`) and nests autocomplete results under `placePrediction`; both were wrong without any compile-time signal.

**Moving files without updating namespaces** cascades into dozens of errors; rebuild after every structural move.
