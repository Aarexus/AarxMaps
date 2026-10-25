# AarxMaps

Strongly typed .NET SDK for Google Maps Platform, shipped as three NuGet packages: `AarxMaps.Core`, `AarxMaps.Places`, `AarxMaps.Routes`. Formerly published as `D3lg4doMaps` / `DelgadoMaps`; the codebase is being refactored package by package under the new name.

**Read `PROGRESS.md` at the start of every session.** It holds current status, next steps, critical decisions and lessons learned. Update it at the end of any session that changes status or makes a decision.

## Your role: reviewer

The user writes the code; Claude reviews it. The unit of work is a whole workflow or module (for example, all of Places Autocomplete: builders, request models, services, DTOs, mappers, DI, tests).

- **Do not write or edit code unless the user explicitly asks.** That covers source, tests and snippets. Reviews describe the problem and the fix in words, pointing to `file:line`. Doc files (`CLAUDE.md`, `PROGRESS.md`) are the exception and stay up to date.
- **Read the whole workflow before judging it.** Follow the call path end to end: builder -> request model -> service -> `IMapsApiClient` -> DTO -> mapper -> public model, plus DI registration and tests.
- **Check against:**
  - Correctness: logic bugs, edge cases, null handling, coordinate order, field masks, payload shape compared with Google's API docs.
  - Project conventions in this file: Public/Internal split, namespaces, DI contract, `ct` propagation, `ConfigureAwait(false)`, validation and exception types, XML docs.
  - Resource ownership and disposal (`JsonDocument`, `StreamResponse`), thread safety, shared or mutable static state.
  - Performance: allocations, LINQ in hot paths, repeated serialization, unnecessary buffering.
  - Simplicity: no overengineering, dead code or duplication. Readable names, and SOLID where it pays off.
  - Public API ergonomics and consistency across Places, Routes and Core.
  - Test coverage: missing cases and weak assertions. Tests must follow the conventions in this file.
  - Builds with zero warnings on both `net8.0` and `net9.0`. Run `dotnet build` / `dotnet test` to verify.
- **Report format:** findings ranked by severity (Critical / Major / Minor / Nit). Each finding gives the location, what's wrong, why it matters, and the suggested fix in words. End with a short verdict: ready, or what blocks it. Skip praise and filler.
- Push back on design choices when a clearly better option exists, with the reason.
- After each review, record decisions and lessons learned in `PROGRESS.md`.

## Status snapshot

- 1.0.0 is unpublished. The public API can still change freely; no migration notes needed until release.
- The README describes the old `DelgadoMaps` API. Don't trust it as a reference for current names.

## Commands

```bash
dotnet build                                   # whole solution, net8.0 + net9.0
dotnet build AarxMaps.Core                     # single project
dotnet test AarxMaps.Tests                     # all tests, both TFMs
dotnet test AarxMaps.Tests -f net9.0           # what CI runs
dotnet test AarxMaps.Tests --filter "Package=Core"
dotnet test AarxMaps.Tests --filter "Package=Core&Module=Http"
```

`GeneratePackageOnBuild` is on, so every build also produces `.nupkg` files.

## Solution layout

```
AarxMaps.Core/     HTTP client, request model, exceptions, JSON, caching, DI entry point
AarxMaps.Places/   Places API (New): text/nearby search, autocomplete, details, photos, reviews
AarxMaps.Routes/   Routes API: computeRoutes (directions), computeRouteMatrix (distance matrix)
AarxMaps.Tests/    Single xUnit project for all packages, split by package folder
```

Places and Routes reference Core via `ProjectReference`. They never talk to `HttpClient` directly; they only use `IMapsApiClient`.

Each project splits `src/` into:

- `src/Public/` - the consumer-facing API. **The namespace drops the `Public` segment**: `src/Public/Models/MapsApiRequest.cs` -> `AarxMaps.Core.Models`.
- **Feature folders (Places):** `src/Public/Services/<Feature>/{Builders,Models,Requests}` all share one flat namespace, `AarxMaps.Places.Services.<Feature>` (e.g. `AarxMaps.Places.Services.Searching`); subfolders don't add namespace segments. The matching internals live in `src/Internal/Services/<Feature>/` (`<Feature>Service`, `<Feature>Factory`, `<Feature>Mapper`), with wire DTOs under `DTOs/`. Shared geometry (`GeoCircle`, `GeoRectangle`, `LocationBias`, `LocationRestriction`) lives in `src/Public/Common/Models` (`AarxMaps.Places.Common.Models.Geometry`).
- **Routes layout:** shared public types (waypoints, `WaypointBuilder`, modifiers, shared enums and response types) all live in `src/Public/Common/Models/**` with one flat namespace, `AarxMaps.Routes.Common.Models`. Features use `AarxMaps.Routes.Services.Directions` / `.DistanceMatrix` like Places. Internals: `Internal/Common/` (`WireEnum<T>`, `CommonMapper`, shared DTOs), `Internal/Validation/RoutesRequestValidator` (travel-mode rules shared by both builders), `Internal/Services/<Feature>/` (`<Feature>Factory` with the field masks, `<Feature>Mapper`, `<Feature>Service`, `DTOs/`). Request bodies are always internal DTOs; enum wire values come from `WireEnum<T>` (SCREAMING_SNAKE_CASE of the member name), so renaming a public enum member changes the wire value.
- `src/Internal/` - implementation. Namespace keeps `Internal` (`AarxMaps.Core.Internal.Http`) and every type is `internal`.
- `src/InternalsVisibleTo.cs` - exposes internals to `AarxMaps.Tests` and `DynamicProxyGenAssembly2` (NSubstitute).

## Core architecture

- **`IMapsApiClient`** (`Public/Abstractions`) is the single gateway to Google:
  - `SendAsync<T>` buffers and deserializes; the client owns and disposes the response.
  - `SendStreamAsync` returns a `StreamResponse` the caller owns (`await using`). Never cached.
  - `SendAndEnumerateAsync<T>` (`MapsApiClientExtensions`) wraps the stream path for top-level JSON arrays (route matrix).
- **`MapsApiRequest`** describes a call (method, base URL, endpoint, headers, query, payload, `ApiKeyLocation`). `RequestFactory` + `RequestBuilder`/`MapsUriBuilder` turn it into an `HttpRequestMessage` and inject the API key, language and region from `MapsConfiguration`.
- **Errors:** `ExceptionHandler` maps status codes to the `MapsApiException` hierarchy (401/403 -> `MapsApiAuthException`, 400 -> `MapsInvalidRequestException`, 404 -> `MapsNotFoundException`, 429 -> `MapsRateLimitException` with `RetryAfter`). Network failures and timeouts are wrapped in `MapsApiException`; caller cancellation surfaces as `OperationCanceledException`.
- **JSON:** `MapsJsonSerializer` (internal, static) with shared `MapsJsonOptions`. Modules use it instead of creating their own `JsonSerializerOptions`. `JsonExtensions` has safe `JsonElement` helpers for raw responses.
- **Caching** is an `IMapsApiClient` decorator (`CacheMapsApiClient`). The real client is registered as a keyed service (`CoreConstants.MapsApiClient.Primary`); the unkeyed registration is overridden when a cache layer is added. Stores: `MemoryCacheStore` / `DistributedCacheStore`. Keys: `RequestFingerprintCacheKeyStrategy` (versioned; bump the version on incompatible model changes). Cache failures are logged and treated as a miss, never thrown.
- **Configuration** uses `IOptions<T>` with `IValidateOptions<T>` validators and `ValidateOnStart()`.

## DI registration contract

Order matters and is enforced with marker singletons (`MapsCoreMarker`, `MapsCachingMarker`):

```csharp
services.AddAarxMaps(o => o.ApiKey = "...");          // required, once
services.AddAarxMapsMemoryCache(o => ...);            // optional, OR AddAarxMapsDistributedCache
services.AddAarxMapsPlaces();                         // requires AddAarxMaps first
services.AddAarxMapsRoutes();                         // requires AddAarxMaps first
```

Module services are `internal` classes registered as transient behind public interfaces. Every module DI method must check `MapsCoreMarker` and fail with a clear message naming the missing call.

## Module conventions (Places / Routes)

- Google's response wire format lives in internal DTOs (`Internal/Services/<Feature>/DTOs` in Places). Mappers convert them to public models and copy collections, since cached DTOs are shared. Public request models may be serialized directly as the payload when their shape matches Google's; public models never carry `[JsonPropertyName]` for Google's schema unless they are serialized directly.
- Endpoint constants live in `Internal/Constants/<Module>Endpoints.cs`.
- Public request models have `internal` setters and are built through fluent builders (`RouteRequestBuilder`, `NearbyRequestBuilder`, ...).
- Every public async method takes a `CancellationToken ct` and passes it to `IMapsApiClient`.
- Field masks (`X-Goog-FieldMask`) are mandatory on the new Google APIs; keep them next to the service that owns the endpoint.
- Coordinates use Core's `LatLng` (`AarxMaps.Core.Models.Geometry`, constructor `(latitude, longitude)`, validates ranges). Don't redeclare coordinate types in modules.

## Code style

Follows the existing code; match it.

- File-scoped namespaces, K&R braces (`{` on the same line), `sealed` for concrete classes unless designed for inheritance.
- Class sections: `// -------------------- INIT --------------------`, `// -------------------- METHS --------------------`, `// -------------------- INNER METHS --------------------`.
- Short uppercase step comments inside methods: `// VALIDATION`, `// CONFIGURATION`, `// SERVICES`.
- `.ConfigureAwait(false)` on every `await` in library code (not in tests).
- Input validation: `ArgumentNullException.ThrowIfNull` for null arguments to infrastructure/DI; `MapsInvalidRequestException` for invalid consumer request values (empty query, coordinates out of range).
- Analyzers run at `AllEnabledByDefault`. Fix warnings; don't suppress them without a reason in a comment. Path-scoped suppressions go in the root `.editorconfig`; one-off ones use `#pragma warning disable/restore` around the statement.

## XML documentation

`GenerateDocumentationFile` is on in every packable project. Every public type and member needs XML docs. Use `IMapsApiClient.cs` as the reference standard:

- `<summary>` says what it does, not how.
- `<exception>` for every exception a consumer can see, including the `MapsApiException` subtypes that bubble up from Core.
- `<remarks>` covers ownership/disposal, cancellation scope, caching behavior and registration order whenever they apply. Use `<para><b>Topic:</b> ...</para>` blocks.
- `<param>`, `<returns>`, `<typeparamref>` on all public members.
- Internal types only need comments where the behavior is non-obvious.

## Tests

- xUnit, FluentAssertions **7.x** (do not upgrade to 8+, it's under a commercial license), NSubstitute, RichardSzalay.MockHttp, `Microsoft.Extensions.Diagnostics.Testing` for fake loggers.
- Layout mirrors the package: `AarxMaps.Tests/<Package>/<Area>/<Type>Tests.cs`, namespace `AarxMaps.Tests.<Package>.<Area>`. Shared helpers in `AarxMaps.Tests/<Package>/Helpers/`.
- Every test class carries traits: `[Trait("Package", "Core")]`, `[Trait("Module", "Http")]`, `[Trait("Type", "MapsApiClient")]`.
- No live Google calls, no real API keys. Use MockHttp handlers and JSON fixtures.
- The test project references Core, Places and Routes. Core helpers (`AarxMaps.Tests/Core/Helpers`: `TestServices` incl. `UseMockHttp`, `TestRequests`, `ApiClientCapture`, `RecordingHttp`, `CoreErrors`) are shared by every package's tests; module helpers live in `<Package>/Helpers/`.

## CI / release

- `.github/workflows/publish.yml`: on tag `v*.*.*` -> restore, build Release, test net9.0, pack, push all packages to nuget.org (`NUGET_API_KEY` secret). Versions are set per csproj (`<Version>`), so keep the three in sync before tagging.
- `.github/workflows/docs.yml`: builds a Docusaurus site from `docs/` and deploys to GitHub Pages on push to `main`. `docs/` exists but is empty; the site gets scaffolded in Phase 4.
- All packages pack the root `README.md` as their NuGet readme.

## Git workflow

- One branch per phase (`Phase2-Places`), merged to `main` via PR. PR/commit title format: `PhaseN: <Package> Module`.
- Commit and push only when asked.

## Dependencies

Ask before adding any package. Check license and known vulnerabilities first. Microsoft.Extensions.* packages are the accepted baseline.
