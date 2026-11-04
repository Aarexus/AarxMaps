# AarxMaps - Progress

Last updated: 2026-11-04

## Current status

**AarxMaps 1.0.0 is published (2026-11-04).** `AarxMaps.Core`, `AarxMaps.Places` and `AarxMaps.Routes` are on nuget.org, owned by the `Aarexus` organization, with the reserved prefix `AarxMaps` (verified mark). Docs are live at `https://aarexus.github.io/AarxMaps/`. The old `D3lg4doMaps.*` packages are deprecated (Legacy) with the matching AarxMaps package as alternate.

| Phase | Scope          | Status |
| ----- | -------------- | ------ |
| 1     | Core module    | Done, merged (#1). |
| 2     | Places module  | Done, merged (#2). Autocomplete, Searching and Details reviewed and fixed; Places tests written by Claude (407). |
| 3     | Routes module  | Done, merged (#3). Directions and DistanceMatrix written by Claude at the user's request, 252 tests, seven targeted mutations all caught. |
| 4     | Docs + release | Done (#4, #5, tag `v1.0.0` on `807f454`). Docusaurus site (plan and history in `DOC-PLAN.md`), README trimmed to a landing page, release R1-R10 (step-by-step record in `RELEASE-PLAN.md`). |

`DOC-PLAN.md` and `RELEASE-PLAN.md` are complete. Their decisions and lessons are merged below; the files stay as the detailed record.

### Build state (2026-11-04)

- **Target framework:** `net10.0` only (Core, Places, Routes, Tests). Apps on .NET 8/9 get NU1202 until they upgrade.
- **Build settings:** shared in `Directory.Build.props` (TFM, version 1.0.0, package metadata, analyzers, symbols). The csproj files keep `PackageId`, `Description`, `PackageTags` and references.
- **Dependencies:** `Microsoft.Extensions.*` 10.0.14, referenced by Core only; Places and Routes get them through Core. No vulnerable packages, transitive included.
- **Solution:** 0 warnings, 0 errors (Debug and Release). 1,284 tests pass.
- **Packages:** `dotnet pack` builds and packs (no `GeneratePackageOnBuild`). Each `.nupkg` has `lib/net10.0` (DLL + XML docs), `README.md` and `icon.png`; each `.snupkg` has the PDB with SourceLink to the release commit.
- **Docs:** all 39 site snippets compile on `net10.0` (`docs/scripts/compile-snippets.py`); `npm run build` passes with broken links set to fail.

## Next steps (post-1.0)

1.0.0 is published, so public API changes now need a planned release with semantic versioning: additive changes in a minor version, breaking changes in a major one with migration notes. The release process is in `CLAUDE.md` (CI / release).

**Known flaws to fix** (details under Known flaws; each is a behavior change, so plan the version):
1. Send `languageCode` / `regionCode` to Places and Routes from `MapsConfiguration`.
2. Routes builders: throw `MapsInvalidRequestException` on `null` in `From`/`To` (and decide on the `With*` setters).
3. Make sliding cache expiration optional.
4. Let consumers build populated response models for their own tests (model factories or `init` setters).

**Optional features** (all listed on the Known limitations page):
- Places: rectangle bias/restriction on `AutocompleteRequestBuilder`, autocomplete match start offset, the request options the builders don't expose (`openNow`, `minRating`, `rankPreference`, `excludedTypes`, `includedRegionCodes`, ...), share `LocalizedTextDto` between Searching and Details.
- Routes: `extraComputations` (traffic on polyline, tolls), `avoidTunnels` / `tollPasses`, leg/step travel advisories, `geocodingResults`, transit headway and agency phone, a streaming `IAsyncEnumerable` matrix method, a polyline decoder.
- Core: built-in retries (429/5xx with `RetryAfter`), a dedicated timeout exception, language in the cache key.

**Manual testing:** WebTestPlan (Blazor Server bench, gitignored, at `WebTests/WebTestPlan/` on the machine where it was written) has never been run against Google with a real key. Run each page in each cache mode and record any SDK bugs here.

### Test layout (Places and Routes)

- `AarxMaps.Tests/Places/{Extensions,Factories,Validation}/` for cross-cutting internals; `Places/Services/{Autocomplete,Searching,Details}/` per feature. Traits: `Package=Places`, `Module=<first folder>` (`Extensions`, `Factories`, `Validation`, `Services`), `Type=<class>`. Filter one feature with `--filter "FullyQualifiedName~Places.Services.Details"`.
- One class per type (`<Type>Tests`) plus `<Feature>WireTests` for anything that needs the real `MapsApiClient` (serialized body, URL, headers, caching end to end).
- Shared helpers (`Core/Helpers/`): `TestServices.UseMockHttp`, `ApiClientCapture` (NSubstitute `IMapsApiClient`, records request + `ct`, thread-safe; `RespondStream`/`FailStream`/`HandleStream` for `SendStreamAsync`), `RecordingHttp` (MockHttp that records method/URI/headers/body), `CoreErrors` (Core exceptions by name for theories).
- Module helpers: `Places/Helpers/` (`PlacesTestServices`, `PlacesFixtures`), `Routes/Helpers/` (`RoutesTestServices`, `RoutesFixtures`, `RoutesSamples`).
- Routes folders: `Routes/{Extensions,Common,Validation}/`, `Routes/Services/{Directions,DistanceMatrix}/`.
- Mappers are tested through `MapsJsonSerializer` so DTO binding to Google's camelCase shape is covered too. Shared rules (`GeoFactory`, `RequestValidator`) are tested directly; builders only get wiring tests.

## Known flaws (shipped in 1.0.0)

The docs page Known limitations is the complete public list (it also covers the missing request options, retries, timeouts and the cache key). The flaws below are the ones that need a code fix.

- **`languageCode` / `regionCode` are never sent (found 2026-10-22).** Core only sends `Accept-Language: {Language}-{Region}` (`RequestFactory`). Google's Places (New) reference documents `languageCode` (default `en-US`) and `regionCode` as body fields (Autocomplete, Text, Nearby) and query parameters (Details), and says nothing about `Accept-Language`. So `MapsConfiguration.Region` has no documented effect, and `Language` depends on undocumented header handling. Routes has the same flaw by decision (2026-10-24). Preferred fix: the services take `IOptions<MapsConfiguration>` and the factories fill both fields from config.
- **Response models have `internal` setters (accepted 2026-10-28).** Consumers can't build populated `PlaceDetails`, `RouteResult`, `RouteMatrixElement`, ... when mocking the services in their own tests. Fix: model factories (Azure SDK pattern) or `init` setters.
- **Inconsistent `null` handling in the Routes builders (accepted 2026-10-30).** `RouteRequestBuilder.From(null)` / `To(null)` are accepted, and `Build()` then fails with "Origin is required. Call From() before Build()", which is misleading after a `From` call. `WithRouteModifiers(null)` and `WithTransitPreferences(null)` (both builders) silently clear the value. `AddIntermediate`, `AddOrigin` and `AddDestination` throw `MapsInvalidRequestException` on `null`.
- **Sliding cache expiration can't be turned off (accepted 2026-10-28).** `MapsCachingOptions.SlidingExpiration` is a non-nullable `TimeSpan` and is always applied (`MapsCachingOptionsValidator` uses `is { } s`, which always matches).

## Critical decisions

### Package line and working model

- **Published package line (1.0.0, 2026-11-04).** Public API changes now need a planned release (semver) and migration notes for breaking changes.
- **Working model (2026-10-20)**: the user writes the code and Claude reviews each workflow or module. Claude writes code only when explicitly asked. Exceptions so far, all at the user's request: Places tests, the Routes module, WebTestPlan, the docs site and README, and the release edits (props, workflows, csproj).
- **Tracking split**: `CLAUDE.md` for stable guidance, `PROGRESS.md` for status/decisions/lessons.

### Architecture and API

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
- **Feature namespaces (2026-10-21)**: public feature types use one flat namespace per feature, `AarxMaps.Places.Services.<Feature>`, regardless of subfolders (`Builders/`, `Models/`, `Requests/`). Internals live in `Internal/Services/<Feature>/` with wire DTOs in `DTOs/`.
- **Public request models may be the wire payload** when their shape matches Google's (e.g. `AutocompleteRequest`, `LocationBias { Circle }`). Responses always go through internal DTOs and mappers.
- **Geometry inputs take `LatLng`**, not separate doubles (`WithLocationBias(LatLng center, double radius)`); radius is validated to (0, 50 000] m in `GeoFactory`.
- **`PlaceSuggestion.EndOffset` is `int?`** (first match only); no sentinel values.
- **Session tokens**: `AutocompleteRequestBuilder.WithSessionToken` and `DetailsRequestBuilder.WithSessionToken` (sent as the `sessionToken` query parameter on Details).
- **`SendAsync<JsonDocument>` is cached like everything else.** `CacheMapsApiClient` stores the raw JSON (same cache key) and returns a new `JsonDocument` per call, which the caller owns and disposes. Unparseable cached JSON is logged and treated as a miss.
- **Rectangle geometry**: `GeoRectangle { Low, High }` (south-west / north-east, serialized as Google's viewport). Builders take `(LatLng southWest, LatLng northEast)`; only latitudes are ordered, so antimeridian-crossing rectangles are allowed.
- **Paging**: `TextRequestBuilder.WithPageSize(int)` and `WithPageToken(string)` are separate; page size 1-20 (`PlacesConstants.MaxPageSize`), default 20 for both Text and Nearby (Google's default).
- **Search DTOs**: one `SearchResponse` (Places + NextPageToken) shared by Text and Nearby; `PlaceSearchInfo.PlaceId`/`DisplayName` are non-null, matching `PlaceSuggestion`.
- **Typed paths deserialize into DTOs, not `JsonDocument` (2026-10-21)**: STJ only materializes declared properties, so response size isn't a reason to avoid DTOs; `JsonDocument` keeps the whole payload, re-parses on every cache hit and hides shape mismatches. `JsonDocument` stays only for the raw methods.
- **Details API shape (2026-10-21)**: one `DetailsRequest` (place ID + session token) for every `IDetailsService` method; `GetPhotosAsync(request, PhotoOptions?)`, `GetDetailsRawAsync(request, fields)`. The session token belongs on exactly one call.
- **`MapsApiRequest.Cacheable` (Core, 2026-10-21)**: defaults to `true`; `CacheMapsApiClient` forwards non-cacheable requests untouched. Used for Places photos (details `photos` mask, media calls, raw requests whose fields include photos).
- **Photo media requests send the API key in the header**, not the query string, so it never lands in URL logs.
- **Shared consumer-value checks** (place ID, session token, required text, type lists via `ValidateAndTrimList(values, name, max)`) live in `Internal/Validation/RequestValidator`. Builders pass their own limits (Autocomplete primary types 5, Nearby types 50).
- **`TextSearchResult.NextPageToken` is null when there are no more pages (2026-10-22)**: the mapper normalizes `""` to `null` so `while (token is not null)` loops are safe.
- **`ISearchService` caching remark kept (2026-10-22)**: "returned instances may be shared" is stricter than the current behavior but stays as a safe contract.
- **Module registration is idempotent (2026-10-22)**: `AddAarxMapsPlaces` / `AddAarxMapsRoutes` use `TryAddTransient`, so a second call is a no-op and a consumer's earlier registration wins. Core still throws on a double `AddAarxMaps()`.
- **NaN is rejected explicitly (2026-10-22)**: `LatLng` and `GeoFactory` check `double.IsNaN` before the range pattern.
- **`DetailsMapper.ToLatLng` stays unguarded (2026-10-22)**: Google never sends out-of-range coordinates and JSON can't carry NaN, so no defensive handling.
- **Response URIs are http/https only (2026-10-22)**: `DetailsMapper.ToUri` returns `null` for anything else, so public `Uri` properties are safe to render as links.
- **CA1032 suppressed for SDK exceptions (2026-10-22)**: one constructor with optional parameters covers the three standard overloads. Scoped rule in the root `.editorconfig`, reason in a comment.
- **DI extension class names (2026-10-22)**: `MapsServiceCollectionExtensions` (Core, partial across `.cs` and `.Caching.cs`), `PlacesServiceCollectionExtensions`, `RoutesServiceCollectionExtensions`.
- **Module DI checks use `IsAarxMapsRegistered()` (2026-10-22)**: Core's marker stays `internal`; modules never reference Core internals.
- **No `.gitattributes` (2026-10-22)**: every tracked file is LF.
- **Routes v1 scope (2026-10-24)**: minimal `TransitDetails`; waypoints accept place ID, location or address (exactly one); no `extraComputations`, so no speed intervals, toll info, leg/step advisories or `AvoidTunnels`.
- **Routes model rules (2026-10-24)**: single-field Google wrappers are flattened (`Polyline` -> `EncodedPolyline`, `LocalizedText` -> `string`, ...). Durations are `TimeSpan`, timestamps `DateTimeOffset`. Proto3-omitted ints/bools map to non-null types. `Unknown` (value 0) only on enums Google returns.
- **Routes public API (2026-10-24)**: shared types in `AarxMaps.Routes.Common.Models`, DI in `AarxMaps.Routes.Common.Extensions`, features in `AarxMaps.Routes.Services.Directions` / `.DistanceMatrix`. `RouteResult.BestRoute` dropped (use `RouteLabel.DefaultRoute`); encoded polylines only.
- **Routes builder rules (2026-10-24)**: `RoutesRequestValidator` (shared) enforces the travel-mode rules (departure xor arrival, arrival only for Transit, routing preference only for Drive/TwoWheeler, ...). Directions: max 25 intermediates. Matrix: origins x destinations <= 625 (100 with TrafficAwareOptimal or Transit), <= 50 place ID/address waypoints. Unset travel mode counts as Drive.
- **Routes wire format (2026-10-24)**: request bodies are internal DTOs; enums go through `WireEnum<T>` (SCREAMING_SNAKE_CASE of the member name), so public enum member names are part of the wire contract and `WireEnumTests` pins every value. Timestamps are UTC `yyyy-MM-ddTHH:mm:ssZ`. `false` flags and empty lists are left out.
- **Routes field masks (2026-10-24)**: Summary / Standard / Full levels per feature; `optimizedIntermediateWaypointIndex` is in every Directions level, since Google rejects optimization without it.
- **Matrix results (2026-10-24)**: `GetDistancesAsync` buffers the stream and returns elements sorted by origin then destination. `Status` is `null` for successful elements. Never cached.
- **WebTestPlan design (2026-10-25)**: Blazor Server + plain CSS, text/tables only. Cache layer picked by `Cache:Mode`; Distributed uses `Microsoft.Extensions.Caching.StackExchangeRedis` (approved by the user). API key only from user secrets or `AarxMaps__ApiKey`.

### Docs site (Phase 4, details in `DOC-PLAN.md`)

- **Site is the source of truth; README is a landing page (2026-10-25, trimmed 2026-11-01).** The README keeps intro, packages, requirements, install, the quick start (same shape as the site's) and links to every site page. Absolute links only, no HTML, since it's the NuGet readme.
- **Docusaurus 3.10.2, classic preset, no extra npm packages (2026-10-28).** Fonts (Inter, Sora) are self-hosted, so readers make no third-party requests. `future.v4` is an object with all five flags and `fasterByDefault: false` (`true` would need `@docusaurus/faster`).
- **Hosting (2026-10-25)**: `https://aarexus.github.io/AarxMaps/` through `docs.yml`. **`trailingSlash: true` (2026-11-01)** removes the GitHub Pages 301 on every page and keeps the `index.html` layout.
- **Task-oriented structure (2026-10-25)**: one page per task, grouped Getting started / Core / Places / Routes, sidebar autogenerated. Places and Routes pages never reference each other; shared topics live under Core; only the Introduction has a combined example. Known limitations counts as shared.
- **Public API only, rewritten from source (2026-10-25).** Names are checked against `src/Public`; old pages are a topic list, never content. Billing statements only where the XML docs make them.
- **Snippets are compiled from the page text (2026-10-30).** `docs/scripts/compile-snippets.py` extracts every `csharp` block into a console and a web project and builds them with warnings as errors. Examples use ASP.NET Core minimal APIs with a bound `CancellationToken`, plus a bare `ServiceCollection` version; keys come from configuration.
- **No versioned docs, no search plugin, no emojis (2026-10-25).** No generated-index pages, because `DocCard` adds emojis; categories link to overview pages instead.
- **Branding (2026-10-25)**: Aarexus palette and wordmark; AarxMaps icon = Aarexus logo + map element. Originals in gitignored `BrandKit/`, tracked copies in `docs/static/img/` and `icon.png` (128x128 NuGet icon). PNG/ICO only, no public SVGs. Ink hero; dark surfaces that carry primary text are Ink (Vermilion on Graphite fails AA).

### Release and packaging (Phase 4, details in `RELEASE-PLAN.md`)

- **1.0.0 targets `net10.0` only (user, 2026-10-31).** .NET 8 and 9 reach end of support on 2026-11-10; .NET 10 is LTS until 2028-11-14. One TFM, one dependency line, one test run. Apps on .NET 8/9 can't install AarxMaps until they upgrade.
- **Shared settings in `Directory.Build.props` (2026-10-31).** Packaging, XML docs and analyzer settings are conditioned on `'$(MSBuildProjectName)' != 'AarxMaps.Tests'` (`IsPackable` isn't set yet when the props file is evaluated). `Version` and `PackageReleaseNotes` are updated together for each release.
- **Modules reference only Core (user, 2026-11-01).** `Microsoft.Extensions.*` versions are set in Core's csproj only.
- **No `GeneratePackageOnBuild` (user, 2026-11-01).** Packing is `dotnet pack`'s job.
- **Symbols and SourceLink (2026-10-31).** `.snupkg` packages, `EmbedUntrackedSources`, and `ContinuousIntegrationBuild` on GitHub Actions only, so published PDBs have normalized paths and local builds stay debuggable.
- **Trusted publishing, no API key (user, 2026-11-02).** nuget.org exchanges the workflow's GitHub OIDC token for a one-hour key. The policy is owned by the `Aarexus` org, created by `JSebas-11` (the `user` input), and bound to `Aarexus/AarxMaps`, `publish.yml` and the `nuget` environment.
- **`nuget` environment (2026-10-31)**: required reviewer (the user, self-review allowed), deployment rule = tag `v*.*.*` only.
- **Packages owned by the `Aarexus` nuget.org organization (user, 2026-11-02)**, prefix `AarxMaps` reserved (granted 2026-11-03).
- **Smoke test from packages, not projects (2026-10-31).** Before a release, install the packed `.nupkg` files from a local folder feed into fresh apps (isolated `NUGET_PACKAGES`), because only that catches packaging mistakes.
- **D3lg4doMaps deprecated, not unlisted (2026-11-03).** All versions marked Legacy with the AarxMaps alternate and a message pointing to the migration page. Unlisting would hide the pointer from searchers.

## Lessons learned

### Code and Google APIs

**FluentAssertions 8+ is commercially licensed.** Stay on 7.x.

**`HttpClient.Timeout` only covers up to response headers** on the streaming path; body reads need their own token.

**Distinguish timeout from cancellation**: catch `OperationCanceledException` `when (!ct.IsCancellationRequested)` to report a timeout as `MapsApiException`.

**Shared `IMemoryCache` with `SizeLimit` silently disables caching** (entries have no size). Documented on `AddAarxMapsMemoryCache`.

**Cached instances are shared** across callers; cached models must not be mutated.

**Never cache a disposable result in the memory store.** `MemoryCacheStore` returns the same instance; a caller that disposes it breaks every later hit. Map to immutable DTOs before caching.

**Check payload/response shape against Google's reference, not intuition.** Places (New) wraps geometry (`locationBias.circle`) and nests autocomplete results under `placePrediction`; both were wrong without any compile-time signal.

**Verify review claims against Google's own samples before recording them.** The reference pages are summarized by tools and can mislead; the guide samples show the real wire shape.

**Google's request fields carry cross-field rules that the reference lists per field** (e.g. `arrivalTime` only for TRANSIT and otherwise silently ignored). Read each field's full description, not just its type.

**Proto3 JSON may omit zero values.** Map value-type response fields to non-null types defaulting to `0`/`false`, never `int?`.

**DateTimeOffset's default JSON format writes 7 fractional digits**, and Google only accepts 0, 3, 6 or 9. Format timestamps explicitly.

**Range patterns let NaN through.** `x is < a or > b` is false for `NaN`. Reject `double.IsNaN` first on every `double` input.

**`Uri.TryCreate(value, UriKind.Absolute, ...)` is platform-dependent and scheme-agnostic.** On Linux/macOS `"/path"` becomes `file:///path`. When a URI is meant as a web link, also check the scheme.

**Test parallel code without sleeps.** Gate each fake call on a `TaskCompletionSource` that completes once every call has started, and wrap the call under test in `WaitAsync(timeout)`.

**Moving files without updating namespaces** cascades into dozens of errors; rebuild after every structural move.

**Razor quirks (2026-10-25):** `@code` blocks can't parse multi-line raw string literals (RZ1000), pattern variables in markup share one scope across the render method, and a page named like an `@inject` property fails with CS0542.

**Browsers block some ports (2026-10-25).** Chrome refuses 5060/5061 with `ERR_UNSAFE_PORT`; curl doesn't. WebTestPlan uses 5080.

### Docs

**Compile snippets from the page text, not a retyped copy.** It caught renamed members and wrong overloads a read-through missed. A wrapper must not add `using` lines (CS0105, or it hides a missing one), and synchronous fragments need a synchronous wrapper (CS1998).

**Read the internals behind every behavior you document.** The README claimed the cache key includes the language; it doesn't. Check claims about keys, timeouts, errors and serialization against the code.

**The old packages beat the old pages.** The 3.0.1 packages gave the exact old API surface; the old docs missed or misstated details.

**Docusaurus traps (2026-10-30):** with the v4 flags, admonition titles need brackets (`:::warning[Title]`) or they render as text without a build error; partial `future.v4` objects default missing flags to `false`; `<details>` ignore a scripted `open` (click the summary in Playwright); Infima's `.alert` restyles everything inside; CSS modules lose to Infima on `.container`; tables don't wrap code, so keep them to two or three short columns; backticked headings break the TOC; code blocks fit about 80 characters at 1440 px.

**Set `trailingSlash` explicitly for Docusaurus on GitHub Pages (2026-11-01).** Unset, every canonical and sitemap URL costs a 301, and only requests against the live host show it.

**Playwright: emulate the color scheme, don't toggle themes by hand.** Setting `data-theme` directly hides the navbar logo.

**The session scratchpad isn't durable.** Keep reusable tooling in the repo (`docs/scripts/`), only generated output in the scratchpad. Untracked files don't travel between machines either; check `git status` before switching.

**Don't `pkill -f` inside a compound command.** It matched its own shell and killed the rest of the command.

### Release

**Check support dates before picking target frameworks (2026-10-31).** `releases-index.json` gives every .NET channel's end-of-support date in one request.

**Verify a retarget with a clean build (2026-10-31).** Stale `bin/` outputs can pass for a new build. Delete `bin/`/`obj/`, rebuild, and check timestamps and package contents.

**An empty `Directory.Build.props` breaks every build (2026-10-31).** MSB4024 "Root element is missing"; even a placeholder needs `<Project></Project>`.

**Check a stash before treating it as pending work (2026-10-31).** Compare it with `git diff stash@{0}` and `git show stash@{0}:<path> | cmp - <path>`.

**Check action versions and runtime support when touching workflows (2026-10-31).** `docs.yml` ran on Node 20 (end of life) with actions three majors behind. The GitHub releases API and Node's `schedule.json` answer both quickly.

**`GeneratePackageOnBuild` changes what `dotnet pack` does (2026-11-01).** With it on, `Pack` skips `Build`, so `dotnet pack` on a clean tree fails with NU5026.

**Isolate the NuGet cache when smoke testing a local feed (2026-11-01).** A locally packed 1.0.0 in `~/.nuget/packages` would later be reused instead of the nuget.org 1.0.0. Set `NUGET_PACKAGES` to a scratch folder.

**Check environment rules through the API, not by eye (2026-11-02).** The public `deployment-branch-policies` endpoint showed a tag rule with a stray trailing dot (`v*.*.*.`) that the settings page hid, and that would have blocked the release.

**A GitHub Actions incident looks like a stuck run (2026-11-04).** The publish run sat in Queued with no "Review deployments" box; the approval only appears once a runner picks the job up. Check githubstatus.com before suspecting the workflow, and don't cancel or retag.

**nuget.org indexing lags the push (2026-11-04).** Packages were downloadable and had pages within minutes; search and the registration data that `dotnet add package` and the deprecation form use took a few hours.
