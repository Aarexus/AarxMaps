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

1. **Restore the build** (both TFMs): fix Autocomplete namespaces, implement `ct` overloads in `DetailsService`, replace `PlacesService` partial properties with a plain constructor-injected aggregate exposing `IAutocompleteService`, `IDetailsService`, `ISearchService` (match the Routes `RoutesService` pattern).
2. **Fix lat/lng swap**: `GeoFactory.CreateCircle` takes `(radius, latitude, longitude)` but `AutocompleteRequestBuilder.WithLocationBias` and `NearbyRequestBuilder.WithLocationRestriction` pass longitude first. Prefer passing a `LatLng` instead of two doubles. Regression-test with unequal, nonzero values.
3. **Dispose photo `JsonDocument`** in `GetPhotoAndMapAsync` once the mapper has materialized the values.
4. **Cancellation**: every public async method takes `CancellationToken ct` and passes it to `IMapsApiClient`.
5. **Validate at the service boundary**, not only in builders: null requests, empty place IDs, `fields`, photo size/count limits. Throw `MapsInvalidRequestException`.
6. **Mapper robustness**: `DetailsMapper` assumes `photoUri` is present; partial/malformed responses should fail intentionally, not with an incidental `UriFormatException`. Absent arrays map to empty collections, absent scalars to `null`, consistently.
7. **Small cleanups**: mostly done in the Autocomplete and Searching passes (read-only headers, no empty builder constructors, non-null `PlaceId` everywhere). Still open: consider exposing the autocomplete match start offset.
8. **Decided (2026-10-21):** `GetDetailsAsync` keeps a fixed field mask, documented as billed Enterprise + Atmosphere; `GetDetailsRawAsync(request, fields)` is the escape hatch.
9. **Tests** (`AarxMaps.Tests/Places/`): DI with/without Core; per-operation method, endpoint, field mask, headers, payload; coordinate order and bounds; mapper fixtures (complete, partial, missing arrays, reviews without author/text); photo limits and disposal; cancellation propagation.
10. DI error message naming: fixed in Places (2026-10-21); Routes still says `D3lg4doMaps.Core`.
11. **Autocomplete: review findings fixed (2026-10-20).** Zero warnings on both TFMs (checked in an isolated build, since Details/Search/PlacesService still break the project). Open follow-ups:
    - Wire `sessionToken` into the Details request so a session can be closed (Autocomplete already sends it).
    - Tests at the end of the module refactor: payload snapshot (`locationBias.circle.center`, unequal lat/lng, nulls left out, `sessionToken`), field mask, mapper fixtures (no `placePrediction`, no `matches`, empty `{}`), builder validation, `ct` propagation, Core `JsonDocument` cache hit returning a fresh, independently disposable document.
12. **Core warnings exist on `main`** (CA1032, CA1724, CA2007, CA1062, CS1591 in exceptions/`CachingInjection`/`MapsApiClientExtensions`). The "zero warnings" rule isn't met by Core; clean up before release.
13. **Searching: review findings fixed (2026-10-21).** Zero warnings in the isolated build. Text restriction is rectangle-only, Nearby restriction circle-only, Text bias accepts both. Open follow-up: Autocomplete also accepts rectangles for bias/restriction (not exposed yet).
14. **Details: review findings fixed (2026-10-21).** Zero warnings in the isolated build. Typed methods deserialize into `PlaceDetailsDto`/`PhotoMediaDto`; photo calls are never cached (`MapsApiRequest.Cacheable = false`) and send the API key in the header; reviews are callable again.
15. **Core registration check (2026-10-22):** public `services.IsAarxMapsRegistered()` in Core replaces direct `MapsCoreMarker` checks (Core's `AddAarxMaps`, cache registration, Places DI). **Places builds on both TFMs.** Routes DI still references the internal marker (fix in Phase 3).
16. **`CA1724`**: `DependencyInjection` clashes with `Microsoft.Extensions.DependencyInjection` in Places and Core (`CachingInjection.cs`). Rename to `*ServiceCollectionExtensions` across all packages before release.
17. **Test project now references Places** and Places exposes internals to `DynamicProxyGenAssembly2`; Places tests are the next session's work (see item 9).
18. **Optional:** share `LocalizedTextDto` between Searching and Details (currently one per feature).

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
- **Autocomplete session tokens** are supported on the builder (`WithSessionToken`); Details integration pending.
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
