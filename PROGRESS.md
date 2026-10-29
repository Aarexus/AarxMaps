# AarxMaps - Progress

Last updated: 2026-10-28

## Current status

| Phase | Scope          | Status                                                                                                                                  |
| ----- | -------------- | --------------------------------------------------------------------------------------------------------------------------------------- |
| 1     | Core module    | Done, merged (#1). 611 tests passing on net8.0 and net9.0 (Phase 2 additions: `Cacheable`, `IsAarxMapsRegistered`).                 |
| 2     | Places module  | Done on `Phase2-Places`, ready for PR #2. Core and Places build with 0 warnings on net8.0 + net9.0; 1,032 tests passing (407 Places). |
| 3     | Routes module  | Implemented on `Phase3-RoutesModule` (2026-10-24), awaiting the user's read-through. Solution builds with 0 warnings on net8.0 + net9.0; 1,284 tests passing (252 Routes). |
| 4     | Docs + release | In progress. README rewritten from the public API (2026-10-25), awaiting the user's read-through. Docusaurus `docs/` and 1.0.0 publish pending. |

### Build state (2026-10-24)

- **Solution build** (`dotnet build AarxMaps.sln`, what CI runs on tags): 0 warnings, 0 errors on both TFMs.
- **Core**, **Places**: unchanged since 2026-10-22.
- **Routes**: rewritten in Phase 3 (Directions + DistanceMatrix). The user's pre-rewrite sources were untracked in git; a copy was kept in the session scratchpad only, so it is gone once the session ends.

## Next steps

### Phase 2 - Places

**Done (2026-10-20 to 2026-10-22):** Autocomplete, Searching and Details reviewed and fixed; Places tests written; pre-release cleanup done. Details are in Critical decisions below.

1. [X] **Places tests** (module by module, each with a peek + plan first): DI, geometry + validation, Autocomplete, Searching, Details (5a details/reviews/raw, 5b photos). Photo concurrency tests are deterministic (30 repeated runs, 0 failures) and mutation-checked.
2. [X] **Pre-release cleanup:**
    - CA1724: `DependencyInjection` renamed to `MapsServiceCollectionExtensions` (Core, `MapsServiceCollectionExtensions.cs` + `.Caching.cs`) and `PlacesServiceCollectionExtensions` (Places). Routes is renamed in Phase 3.
    - Core analyzer backlog: CA2007 (`await using` now `ConfigureAwait(false)` via `xLifetime` locals), CA1062 (`SendAndEnumerateAsync` null check, documented), CS1591 (exception constructors documented), CA1308 (`#pragma` with reason in `RequestFingerprintCacheKeyStrategy`), CA1032 (suppressed, see Critical decisions).
    - Caching docs: `AddAarxMapsMemoryCache` / `AddAarxMapsDistributedCache` now say Google's terms restrict caching (Places: only place IDs may be stored indefinitely) and that expirations are the consumer's responsibility.

3. **Next:** open PR #2 (`Phase2: Places Module`), merge to `main`.
4. **Optional (post-v1):** rectangle bias/restriction on `AutocompleteRequestBuilder` (Google supports it); expose autocomplete match start offset; share `LocalizedTextDto` between Searching and Details.

### Test layout (Places and Routes)

- `AarxMaps.Tests/Places/{Extensions,Factories,Validation}/` for cross-cutting internals; `Places/Services/{Autocomplete,Searching,Details}/` per feature. Traits: `Package=Places`, `Module=<first folder>` (`Extensions`, `Factories`, `Validation`, `Services`), `Type=<class>`. Filter one feature with `--filter "FullyQualifiedName~Places.Services.Details"`.
- One class per type (`<Type>Tests`) plus `<Feature>WireTests` for anything that needs the real `MapsApiClient` (serialized body, URL, headers, caching end to end).
- Shared helpers (`Core/Helpers/`, moved there from `Places/Helpers/` on 2026-10-24 so Routes can reuse them): `TestServices.UseMockHttp`, `ApiClientCapture` (NSubstitute `IMapsApiClient`, records request + `ct`, thread-safe; `RespondStream`/`FailStream`/`HandleStream` for `SendStreamAsync`), `RecordingHttp` (MockHttp that records method/URI/headers/body), `CoreErrors` (Core exceptions by name for theories).
- Module helpers: `Places/Helpers/` (`PlacesTestServices`, `PlacesFixtures`), `Routes/Helpers/` (`RoutesTestServices`, `RoutesFixtures`, `RoutesSamples`).
- Routes folders: `Routes/{Extensions,Common,Validation}/`, `Routes/Services/{Directions,DistanceMatrix}/`.
- Mappers are tested through `MapsJsonSerializer` so DTO binding to Google's camelCase shape is covered too. Shared rules (`GeoFactory`, `RequestValidator`) are tested directly; builders only get wiring tests.

### Phase 3 - Routes

**Done (2026-10-24):** public API review, model spec, then the full module implemented by Claude at the user's request (see Critical decisions): Directions and DistanceMatrix end to end (public models, builders, shared validator, DTOs, mappers, field masks, services, DI) plus 252 tests. Seven targeted mutations (ordering, false flags in the body, timestamp format, arrival-time rule, empty status, shared lists, place ID/address counting) were all caught.

1. **Next:** user reads the Routes code and tests; fix anything found.
2. Open PR #3 (`Phase3: Routes Module`) once PR #2 is merged.
3. **Optional (post-v1):** `extraComputations` (`TRAFFIC_ON_POLYLINE` -> speed intervals, `TOLLS` -> toll info), `AvoidTunnels`, `geocodingResults` for address waypoints, transit headway/agency phone, a streaming `IAsyncEnumerable` matrix method, encoded-polyline decoding helper.

### Phase 4 - Docs + release

- Docusaurus site under `docs/` (workflow already exists). **Plan in `DOC-PLAN.md` (2026-10-25):** structure, style, branding, hosting and work steps are decided; open questions there block Step 1. The site will become the source of truth, and the README gets trimmed afterwards (supersedes "README scope" below once done).
- [X] **README rewrite (2026-10-25):** written by Claude from the public API only (all `src/Public` types of the three packages). Covers install, quick start, configuration, registration rules, caching, every Places/Routes builder and result model, detail levels, travel-mode rules, errors, cancellation, `IMapsApiClient` for custom endpoints, usage notes, known limitations and a namespace table. Every code snippet was compiled against the projects in a scratch app (0 warnings). DelgadoMaps breaking-changes and credits sections dropped.
- **Next:** user reads the README. Findings from the README pass (status 2026-10-28):
    1. Accepted as a v1 limitation: public response models (`PlaceDetails`, `RouteResult`, `RouteMatrixElement`, ...) have `internal` setters, so consumers can't build populated results when mocking `IPlacesService`/`IRoutesService`. Post-v1 options: public `PlacesModelFactory` / `RoutesModelFactory` (Azure SDK pattern) or `init` setters.
    2. [X] Fixed: `IMapsApiClient.SendStreamAsync` remarks now point to `SendAndEnumerateAsync` (cref).
    3. [X] Fixed: `LatLng` remarks now say consumers create instances through the validating constructor.
    4. Minor: `docs/docusaurus.config.ts` still uses `JSebas-11.github.io` / `D3lg4doMaps`. Covered by `DOC-PLAN.md` Step 3.
    5. Accepted as a v1 limitation: `MapsCachingOptionsValidator` uses `o.SlidingExpiration is { } s` on a non-nullable `TimeSpan` (always matches); sliding expiration can't be turned off.
    6. [X] Fixed (2026-10-28): `AddAarxMapsPlaces` XML docs now document `ArgumentNullException` (thrown by `IsAarxMapsRegistered`), like `AddAarxMapsRoutes`.
- Verify packed `.nupkg` contents (README, XML docs) before tagging `v1.0.0`.
- [X] **WebTestPlan (2026-10-25):** manual test bench written by Claude at the user's request, at `WebTests/WebTestPlan/` (gitignored, not in `AarxMaps.sln`). Blazor Server (interactive), plain CSS, `ProjectReference` to the three SDK projects, one page per area (Autocomplete, Search, Details, Directions, Distance Matrix, `IMapsApiClient`). Builds with 0 warnings; every page renders (prerender) in all three cache modes with a dummy key. Interactive flows and real Google calls not run yet. See `WebTests/WebTestPlan/README.md` to run it.
- **Next:** user runs WebTestPlan with a real key against each page and cache mode; record any SDK bugs found here.

## Known flaws (accepted for v1)

- **`languageCode` / `regionCode` are never sent to Places (found 2026-10-22).** Core only sends `Accept-Language: {Language}-{Region}` (`RequestFactory`). Google's Places (New) reference documents `languageCode` (default `en-US`) and `regionCode` as body fields (Autocomplete, Text, Nearby) and query parameters (Details), and says nothing about `Accept-Language`. So `MapsConfiguration.Region` has no documented effect on Places, and `Language` depends on undocumented header handling. Preferred fix after v1: Places services take `IOptions<MapsConfiguration>` and the factories fill both from config. Routes has the same flaw by decision (2026-10-24): its reference also documents `languageCode`/`regionCode` as body fields, and they are not sent.

- **Response models have `internal` setters (accepted 2026-10-28).** Consumers can't build populated `PlaceDetails`, `RouteResult`, `RouteMatrixElement`, ... when mocking the services in their own tests. Post-v1: model factories or `init` setters.
- **Sliding cache expiration can't be turned off (accepted 2026-10-28).** `MapsCachingOptions.SlidingExpiration` is a non-nullable `TimeSpan` and is always applied.

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
- **Routes v1 scope (2026-10-24)**: transit steps carry a minimal `TransitDetails` (stops, UTC + localized times, headsign, stop count, line name/short name/colors/vehicle type, agencies). Waypoints accept place ID, location or address (exactly one). No `extraComputations` in v1, so `SpeedReadingIntervals`, `TollInfo`, leg/step travel advisories and `AvoidTunnels` are dropped. `languageCode`/`regionCode` are not sent as body fields; Routes relies on Core's `Accept-Language` header like Places (same known flaw, fix both post-v1).
- **Routes model rules (2026-10-24)**: single-field Google wrappers are flattened (`Polyline` -> `EncodedPolyline`, `StepsOverview` -> `Segments`, `VehicleInfo` -> `EmissionType`, `LocalizedText` -> `string`). Durations are `TimeSpan`, timestamps `DateTimeOffset`. Proto3-omitted ints/bools map to non-null types defaulting to 0/false. `Unknown` (first member, value 0) only on enums Google returns; request-only enums have no `Unknown`. Shared request rules (travel-mode dependent fields) live in an internal Routes validator used by both builders.
- **Routes code ownership (2026-10-24)**: the user asked Claude to write and own the Routes module code (exception to the reviewer role, like the Places tests). The user reads it afterwards.
- **Routes public API (2026-10-24)**: shared types live in one namespace, `AarxMaps.Routes.Common.Models` (waypoints, `WaypointBuilder`, `RouteModifiers`, `TransitPreferences`, shared enums and response types); DI in `AarxMaps.Routes.Common.Extensions` (`RoutesServiceCollectionExtensions`, idempotent `TryAddTransient` like Places); features in `AarxMaps.Routes.Services.Directions` / `.DistanceMatrix`. `ComputeRoute` renamed `Route`; `RouteResult.BestRoute` dropped (use `RouteLabel.DefaultRoute`); `PolylineEncoding` dropped (encoded only); `RouteMatrixDestination` flattened to `Waypoint`; `RouteMatrixOrigin` built only by the builder (`AddOrigin(waypoint, modifiers?)`, `WithOrigins(waypoints, modifiers?)`).
- **Routes builder rules (2026-10-24)**: `RoutesRequestValidator` (shared) enforces: departure xor arrival; arrival only for Transit; routing preference only for Drive/TwoWheeler; traffic model only for Drive + TrafficAwareOptimal; transit preferences only for Transit; heading/stopover/side of road only for Drive/TwoWheeler; no via on origins/destinations. Directions adds: max 25 intermediates, no via with waypoint optimization, optimization needs intermediates. Matrix adds: origins x destinations <= 625 (100 with TrafficAwareOptimal or Transit), <= 50 waypoints given as place ID or address. Unset travel mode counts as Drive. Not validated (documented only): past departure times (clock-dependent), transit 7-day/100-day window, modifiers that Google ignores for the mode.
- **Routes wire format (2026-10-24)**: request bodies are internal DTOs (`ComputeRoutesPayload`, `ComputeRouteMatrixPayload`), never the public models. Enums go through `WireEnum<T>` (SCREAMING_SNAKE_CASE of the member name; unknown values read as `Unknown`; undefined values throw `MapsInvalidRequestException`), so public enum member names are part of the wire contract and `WireEnumTests` pins every value. Timestamps are sent as UTC `yyyy-MM-ddTHH:mm:ssZ`. `false` flags and empty lists are left out of the body.
- **Routes field masks (2026-10-24)**: Directions Summary = route totals + labels/description/warnings/advisory/localized values + `optimizedIntermediateWaypointIndex` (in every level, since Google rejects optimization without it) + `fallbackInfo`; Standard adds polyline, viewport and per-leg totals; Full adds the route token and whole legs. Matrix Summary = indices, status, condition, distance, duration, fallback; Standard adds static duration and localized values; Full adds the travel advisory.
- **Matrix results (2026-10-24)**: `GetDistancesAsync` buffers the stream and returns elements sorted by origin then destination. `Status` is `null` for successful elements (Google sends `"status": {}`). Never cached (streaming path).
- **README scope (2026-10-25)**: the README is the full usage guide (enough to use the SDK without reading code) and is packed as the NuGet readme, so it uses absolute links and plain Markdown only. No history/breaking-changes section for the 1.0.0 line.
- **WebTestPlan design (2026-10-25)**: Blazor Server + plain CSS (no UI library), text/tables only (no Maps JavaScript API). Cache layer picked by `Cache:Mode` (`None`/`Memory`/`Distributed`); Distributed uses `Microsoft.Extensions.Caching.StackExchangeRedis` 10.0.7 (MIT, no known vulnerabilities, approved by the user). API key only from user secrets or `AarxMaps__ApiKey`. The UI doesn't pre-validate, so invalid input reaches the builders and their rules can be tested. Session token on Details is consumed by the first call.
- **Docs site (2026-10-28)**: plan in `DOC-PLAN.md`. Claude writes the Docusaurus pages and the user reads them (exception to the reviewer role). The user handles all git work for Phase 4 (branch `Phase4-Docs`). No versioned docs.
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

**Proto3 JSON may omit zero values.** Google's protobuf-backed APIs can drop `0`, `false` and `""` from responses (e.g. `stepStartIndex` of the first segment). Map value-type response fields to non-null types defaulting to `0`/`false`, never `int?`. Correction (2026-10-24): matrix `originIndex`/`destinationIndex` are sent explicitly even when 0 (Google's sample shows them), so the review's "index 0 is lost" claim was wrong; the non-null `int` mapping is still right.

**Verify review claims against Google's own samples before recording them.** The reference pages are summarized by tools and can mislead; the guide samples show the real wire shape.

**DateTimeOffset's default JSON format writes 7 fractional digits**, and Google only accepts 0, 3, 6 or 9. Format timestamps explicitly before sending them.

**Google's request fields carry cross-field rules that the reference lists per field** (e.g. `routingPreference` only for DRIVE/TWO_WHEELER, `arrivalTime` only for TRANSIT and otherwise silently ignored, `via` only on intermediates). Read each field's full description, not just its type.

**Compile README snippets before publishing them.** A scratch project referencing the packages catches renamed members and wrong overloads that a read-through misses.

**Razor quirks (2026-10-25):** `@code` blocks can't parse multi-line raw string literals (RZ1000; use verbatim `@"..."`), pattern variables in markup (`@if (x is { } p)`) share one scope across the whole render method, and a page named like an `@inject` property (`Client.razor` + `Client`) fails with CS0542.

**Browsers block some ports (2026-10-25).** Chrome refuses 5060/5061 (SIP) with `ERR_UNSAFE_PORT`; curl doesn't, so a curl smoke test won't catch it. WebTestPlan uses 5080.

**Moving files without updating namespaces** cascades into dozens of errors; rebuild after every structural move.
