# AarxMaps - Progress

Last updated: 2026-10-20

## Current status

| Phase | Scope          | Status                                                            |
| ----- | -------------- | ----------------------------------------------------------------- |
| 1     | Core module    | Done, merged (#1). 603 tests passing on net8.0 and net9.0.        |
| 2     | Places module  | In progress on `Phase2-Places`. **Does not build.**       |
| 3     | Routes module  | Not started. **Does not build** against the new Core.       |
| 4     | Docs + release | Not started. Docusaurus `docs/`, README rewrite, 1.0.0 publish. |

### Build state (2026-10-20)

- **Places** (51 errors): Autocomplete files moved to `Internal/Services/Autocomplete/` and `Public/Services/Autocomplete/` without updating namespaces/usings; `DetailsService` doesn't implement the new `CancellationToken` overloads of `IDetailsService`; `PlacesService` uses partial properties (C# 13), which fails on `net8.0`.
- **Routes** (2 errors): `DistanceMatrixService` depends on `IMapsJsonSerializer`, which no longer exists. Core now exposes the static internal `MapsJsonSerializer` and the `SendAndEnumerateAsync<T>` extension.
- Both modules' DI error messages still say `D3lg4doMaps.Core`.

## Next steps

### Phase 2 - Places

1. **Restore the build** (both TFMs): fix Autocomplete namespaces, implement `ct` overloads in `DetailsService`, replace `PlacesService` partial properties with a plain constructor-injected aggregate exposing `IAutocompleteService`, `IDetailsService`, `ISearchService` (match the Routes `RoutesService` pattern).
2. **Fix lat/lng swap**: `GeoFactory.CreateCircle` takes `(radius, latitude, longitude)` but `AutocompleteRequestBuilder.WithLocationBias` and `NearbyRequestBuilder.WithLocationRestriction` pass longitude first. Prefer passing a `LatLng` instead of two doubles. Regression-test with unequal, nonzero values.
3. **Dispose photo `JsonDocument`** in `GetPhotoAndMapAsync` once the mapper has materialized the values.
4. **Cancellation**: every public async method takes `CancellationToken ct` and passes it to `IMapsApiClient`.
5. **Validate at the service boundary**, not only in builders: null requests, empty place IDs, `fields`, photo size/count limits. Throw `MapsInvalidRequestException`.
6. **Mapper robustness**: `DetailsMapper` assumes `photoUri` is present; partial/malformed responses should fail intentionally, not with an incidental `UriFormatException`. Absent arrays map to empty collections, absent scalars to `null`, consistently.
7. **Small cleanups**: static mutable header dictionaries -> read-only; remove empty builder constructors; decide on `PlaceSearchResult.PlaceId` (nullable) vs `PlaceSuggestion.PlaceId` (non-null); consider exposing autocomplete match start offset.
8. **Decide** whether `GetDetailsAsync` takes a configurable field mask or documents a fixed one (`GetDetailsRawAsync` is the escape hatch). Document `GetDetailsRawAsync` disposal and empty-mask behavior.
9. **Tests** (`AarxMaps.Tests/Places/`): DI with/without Core; per-operation method, endpoint, field mask, headers, payload; coordinate order and bounds; mapper fixtures (complete, partial, missing arrays, reviews without author/text); photo limits and disposal; cancellation propagation.
10. Fix DI error message naming.
11. **Autocomplete: review findings fixed (2026-10-20).** Zero warnings on both TFMs (checked in an isolated build, since Details/Search/PlacesService still break the project). Open follow-ups:
    - Wire `sessionToken` into the Details request so a session can be closed (Autocomplete already sends it).
    - Tests at the end of the module refactor: payload snapshot (`locationBias.circle.center`, unequal lat/lng, nulls left out, `sessionToken`), field mask, mapper fixtures (no `placePrediction`, no `matches`, empty `{}`), builder validation, `ct` propagation, Core `JsonDocument` cache hit returning a fresh, independently disposable document.
12. **Core warnings exist on `main`** (CA1032, CA1724, CA2007, CA1062, CS1591 in exceptions/`CachingInjection`/`MapsApiClientExtensions`). The "zero warnings" rule isn't met by Core; clean up before release.

### Phase 3 - Routes

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
- **Feature namespaces (2026-10-20)**: each Places feature exposes one flat public namespace (`AarxMaps.Places.Autocomplete`) regardless of subfolders (`Builders/`, `Models/`, `Requests/`). Internals live in `Internal/Services/<Feature>/` with wire DTOs in `Internal/Services/<Feature>/DTOs/`.
- **Public request models may be the wire payload** when their shape matches Google's (e.g. `AutocompleteRequest`, `LocationBias { Circle }`). Responses always go through internal DTOs and mappers.
- **Geometry inputs take `LatLng`**, not separate doubles (`WithLocationBias(LatLng center, double radius)`); radius is validated to (0, 50 000] m in `GeoFactory`.
- **`PlaceSuggestion.EndOffset` is `int?`** (first match only); no sentinel values.
- **Autocomplete session tokens** are supported on the builder (`WithSessionToken`); Details integration pending.
- **`SendAsync<JsonDocument>` is cached like everything else.** `CacheMapsApiClient` stores the raw JSON (same cache key) and returns a new `JsonDocument` per call, which the caller owns and disposes. Unparseable cached JSON is logged and treated as a miss.
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
