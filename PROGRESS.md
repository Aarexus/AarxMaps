# AarxMaps - Progress

Last updated: 2026-10-22

## Current status

| Phase | Scope          | Status                                                            |
| ----- | -------------- | ----------------------------------------------------------------- |
| 1     | Core module    | Done, merged (#1). 611 tests passing on net8.0 and net9.0 (Phase 2 additions: `Cacheable`, `IsAarxMapsRegistered`). |
| 2     | Places module  | Done on `Phase2-Places`, ready for PR #2. Core and Places build with 0 warnings on net8.0 + net9.0; 1,032 tests passing (407 Places). |
| 3     | Routes module  | Not started. **Does not build** against the new Core.       |
| 4     | Docs + release | Not started. Docusaurus `docs/`, README rewrite, 1.0.0 publish. |

### Build state (2026-10-22)

- **Core**: builds with 0 warnings on both TFMs (analyzer backlog cleared 2026-10-22).
- **Places**: builds with 0 warnings on both TFMs. Tests done: DI, geometry, validation, autocomplete, searching, details (incl. photos).
- **Solution build** (`dotnet build AarxMaps.sln`, what CI runs on tags) fails only because of Routes (4 errors, `IMapsJsonSerializer`). Fixed in Phase 3; the publish workflow only runs on `v*` tags.
- **Routes** (not touched): `DistanceMatrixService` depends on the removed `IMapsJsonSerializer`; DI references Core's internal `MapsCoreMarker` and still says `D3lg4doMaps.Core`.

## Next steps

### Phase 2 - Places

**Done (2026-10-20 to 2026-10-22):** Autocomplete, Searching and Details reviewed and fixed; Places tests written; pre-release cleanup done. Details are in Critical decisions below.

1. [x] **Places tests** (module by module, each with a peek + plan first): DI, geometry + validation, Autocomplete, Searching, Details (5a details/reviews/raw, 5b photos). Photo concurrency tests are deterministic (30 repeated runs, 0 failures) and mutation-checked.
2. [x] **Pre-release cleanup:**
   - CA1724: `DependencyInjection` renamed to `MapsServiceCollectionExtensions` (Core, `MapsServiceCollectionExtensions.cs` + `.Caching.cs`) and `PlacesServiceCollectionExtensions` (Places). Routes is renamed in Phase 3.
   - Core analyzer backlog: CA2007 (`await using` now `ConfigureAwait(false)` via `xLifetime` locals), CA1062 (`SendAndEnumerateAsync` null check, documented), CS1591 (exception constructors documented), CA1308 (`#pragma` with reason in `RequestFingerprintCacheKeyStrategy`), CA1032 (suppressed, see Critical decisions).
   - Caching docs: `AddAarxMapsMemoryCache` / `AddAarxMapsDistributedCache` now say Google's terms restrict caching (Places: only place IDs may be stored indefinitely) and that expirations are the consumer's responsibility.
3. **Next:** open PR #2 (`Phase2: Places Module`), merge to `main`.
4. **Optional (post-v1):** rectangle bias/restriction on `AutocompleteRequestBuilder` (Google supports it); expose autocomplete match start offset; share `LocalizedTextDto` between Searching and Details.

### Places test layout (reference for Routes tests)

- `AarxMaps.Tests/Places/{Extensions,Factories,Validation}/` for cross-cutting internals; `Places/Services/{Autocomplete,Searching,Details}/` per feature. Traits: `Package=Places`, `Module=<first folder>` (`Extensions`, `Factories`, `Validation`, `Services`), `Type=<class>`. Filter one feature with `--filter "FullyQualifiedName~Places.Services.Details"`.
- One class per type (`<Type>Tests`) plus `<Feature>WireTests` for anything that needs the real `MapsApiClient` (serialized body, URL, headers, caching end to end).
- Helpers (`Places/Helpers/`): `PlacesTestServices` (`Create`, `UseMockHttp`, `BuildWire`), `ApiClientCapture` (NSubstitute `IMapsApiClient`, records request + `ct`, thread-safe), `RecordingHttp` (MockHttp that records method/URI/headers/body), `CoreErrors` (Core exceptions by name for theories), `PlacesFixtures` (inline Google responses).
- Mappers are tested through `MapsJsonSerializer` so DTO binding to Google's camelCase shape is covered too. Shared rules (`GeoFactory`, `RequestValidator`) are tested directly; builders only get wiring tests.

### Phase 3 - Routes

- Replace the `MapsCoreMarker` check in `AddAarxMapsRoutes` with `services.IsAarxMapsRegistered()` and fix its `D3lg4doMaps.Core` message.

- Migrate `DistanceMatrixService` to `SendAndEnumerateAsync<T>`; drop `IMapsJsonSerializer`.
- Audit against the same checklist as Places (cancellation, validation, mappers, tests, XML docs).

### Phase 4 - Docs + release

- Docusaurus site under `docs/` (workflow already exists).
- Rewrite README with current names (`AddAarxMaps*`, `AarxMaps.*` namespaces); drop the DelgadoMaps breaking-changes section or move it to a "history" page.
- Verify packed `.nupkg` contents (README, XML docs) before tagging `v1.0.0`.

## Known flaws (accepted for v1)

- **`languageCode` / `regionCode` are never sent to Places (found 2026-10-22).** Core only sends `Accept-Language: {Language}-{Region}` (`RequestFactory`). Google's Places (New) reference documents `languageCode` (default `en-US`) and `regionCode` as body fields (Autocomplete, Text, Nearby) and query parameters (Details), and says nothing about `Accept-Language`. So `MapsConfiguration.Region` has no documented effect on Places, and `Language` depends on undocumented header handling. Preferred fix after v1: Places services take `IOptions<MapsConfiguration>` and the factories fill both from config. Recheck the same for Routes in Phase 3.

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
- **Shared consumer-value checks** (place ID, session token, required text, type lists via `ValidateAndTrimList(values, name, max)`) live in `Internal/Validation/RequestValidator`. Builders pass their own limits (Autocomplete primary types 5, Nearby types 50). Rule tests live in `RequestValidatorTests`; builder tests only cover wiring (limit, field name, values set).
- **`TextSearchResult.NextPageToken` is null when there are no more pages (2026-10-22)**: Google sends it omitted or empty; the mapper normalizes `""` to `null` so `while (token is not null)` loops are safe.
- **`ISearchService` caching remark kept (2026-10-22)**: "returned instances may be shared" is stricter than the current behavior (mappers build fresh models per call) but stays as a safe contract.
- **`AddAarxMapsPlaces` is idempotent (2026-10-22)**: `TryAddTransient`, so a second call is a no-op and a consumer's earlier registration of a Places interface wins. Core still throws on a double `AddAarxMaps()`.
- **NaN is rejected explicitly (2026-10-22)**: `LatLng` (lat/lng) and `GeoFactory` (radius) check `double.IsNaN` before the range pattern. Infinity is already outside the ranges.
- **`DetailsMapper.ToLatLng` stays unguarded (2026-10-22)**: it builds `LatLng` from Google's response, so an out-of-range value would throw `MapsInvalidRequestException`. Not reachable in practice (JSON can't carry NaN, Google never sends out-of-range coordinates), so no defensive handling.
- **Response URIs are http/https only (2026-10-22)**: `DetailsMapper.ToUri` returns `null` for anything else (`javascript:`, `file:`, and Unix paths that `Uri.TryCreate` turns into `file:///`). Public `Uri` properties are safe to render as links.
- **CA1032 suppressed for SDK exceptions (2026-10-22)**: one constructor with optional parameters (`message, status, response, inner`) covers `new X()`, `new X(msg)` and `new X(msg, inner: ex)`; the three standard overloads would only duplicate it. Scoped rule in the root `.editorconfig` (`AarxMaps.Core/src/Public/Exceptions/*.cs`), reason in a comment.
- **DI extension class names (2026-10-22)**: `MapsServiceCollectionExtensions` (Core, partial across `.cs` and `.Caching.cs`), `PlacesServiceCollectionExtensions` (Places); Routes follows the pattern in Phase 3. Test classes keep their feature names (`DependencyInjectionTests`, `CacheDependencyInjectionTests`).
- **No `.gitattributes` (2026-10-22)**: every tracked file is LF; line endings are left to editor settings.
- **Module DI checks use `IsAarxMapsRegistered()` (2026-10-22)**: Core's marker stays `internal`; modules never reference Core internals.
- **Places tests (2026-10-22)**: Claude writes the test code for Places (exception to the reviewer role; the user reads it afterwards). One module at a time, each starting with a peek + action plan before any test is written. JSON fixtures are inline raw string literals in helper classes, not files.
- **Test layout (2026-10-22)**: one test class per type (`<Type>Tests`) plus a `<Feature>WireTests` class per feature for everything that needs the real `MapsApiClient` (serialized body, headers, URL). The `Module` trait is the test folder name.
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

**Range patterns let NaN through.** `x is < a or > b` is false for `NaN`, so the value passes validation and only fails later in the JSON serializer with an unrelated exception. Reject `double.IsNaN` first on every `double` input.

**`Uri.TryCreate(value, UriKind.Absolute, ...)` is platform-dependent and scheme-agnostic.** On Linux/macOS `"/path"` becomes `file:///path`; `javascript:`, `file:`, `mailto:` are all accepted everywhere. When a URI comes from outside and is meant as a web link, also check `Scheme` is `https`/`http`.

**Test parallel code without sleeps.** Gate each fake call on a `TaskCompletionSource` that completes once every call has started, and wrap the call under test in `WaitAsync(timeout)` so a regression fails fast instead of hanging the run.

**Moving files without updating namespaces** cascades into dozens of errors; rebuild after every structural move.
