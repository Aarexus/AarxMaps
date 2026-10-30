# AarxMaps - Documentation Plan

Last updated: 2026-10-30

Plan for the Docusaurus site under `docs/`, which replaces the site inherited from D3lg4doMaps. This file is self-contained: the docs work will happen on a different machine, so everything needed to resume is written here. Phase 4 in `PROGRESS.md` points here. Once the site is live, this file's decisions get merged into `PROGRESS.md`.

Read order in a new session: `CLAUDE.md` -> `PROGRESS.md` -> this file.

## Current status

- **Planning done (2026-10-25, answers 2026-10-28).** Every decision is listed under Critical decisions.
- **Code fixes done (2026-10-28).** These were XML docs only; the solution builds with 0 warnings on net8.0 + net9.0.
  - `IMapsApiClient.SendStreamAsync` remarks now link to `SendAndEnumerateAsync`.
  - `LatLng` remarks now say consumers create instances.
  - `AddAarxMapsPlaces` now documents `ArgumentNullException`.
- **`docs/` is untouched.** It still holds the D3lg4doMaps site: Docusaurus 3.9.2, 23 pages, emoji-heavy, organized by code layout.
- **Step 0 checked on the docs machine (2026-10-29, laptop, Linux Mint 22.2).**
  - `docs/` and `.github/` were copied over by hand and are still **untracked** on `Phase4-Docs`. `README.md` and the planning commit (`8ef933c`) are in git.
  - `BrandKit/` is present: `brand-kit.md` (palette, fonts, logo rules), `AarxMaps.ico` (7 sizes, 16-256), `PNGs/` (AarxMaps icon 256/512, Aarexus wordmark light/dark 1260x160, Aarexus avatar 1024). **No SVGs**, although `brand-kit.md` lists SVG exports.
  - Tooling: .NET SDK 8.0.427, 9.0.314 (and 10.0.300); Node 20.20.2, npm 10.8.2. No `rsvg-convert` or ImageMagick; system Python has Pillow 10.2.
  - `dotnet build AarxMaps.sln`: 0 warnings, 0 errors.
  - Blueprint site: `npm ci && npm run build` passes on 3.9.2. `npm audit --omit=dev` reports 52 vulnerabilities (18 critical, 18 high), all in the Docusaurus/webpack build and dev-server chain (`ws`, `websocket-driver`, ...). The site is static, so nothing ships to readers, but the dev server is exposed while running. Recheck after the 3.10.2 upgrade in Step 3.
  - `@docusaurus/core` dist-tags on 2026-10-29: `latest` 3.10.2, `canary` 4.0.0-canary. The 3.10.2 target still holds.
  - Playwright plugin: installed 2026-10-29. Browser binaries download on first use.
- **Blocked:** nothing.
- **Step 2 done (2026-10-30).** `docs/docs/coming-from-delgadomaps.md` maps every public name of D3lg4doMaps 3.0.1 to AarxMaps 1.0.0.
  - Ground truth: the 3.0.1 packages in the local NuGet cache (`~/.nuget/packages/d3lg4domaps.*`), dumped by reflection in a scratch app, compared with the same dump of the current projects. The old pages were only used to cross-check.
  - Both example snippets compile with `-warnaserror` (the before against the 3.0.1 packages, the after against the projects). So does every new-API form named in the lists.
  - The blueprint site still builds with the page added (`npm run build`). Playwright check at 1440, 1016 and 375 px: no page-level horizontal scroll.
  - No links yet: the target pages don't exist until Step 3, and `onBrokenLinks: 'throw'` would fail the build. "See also" links get added in Step 8.
- **Step 3 done (2026-10-30).** `docs/` now has the new structure: 4 categories, 22 placeholder pages ("This page is being written."), and `coming-from-delgadomaps.md`.
  - **Upgrade:** Docusaurus 3.9.2 -> 3.10.2, all five `@docusaurus/*` packages pinned exact, lockfile regenerated. `npm ci && npm run build` passes, `npm run typecheck` passes.
  - **Config:** applied as in Site configuration below, plus `blog: false` (there is no blog). `future.v4` is now an object, see Critical decisions.
  - **Removed:** old pages, `migration/`, homepage components, `markdown-page.md`, the six feature SVGs and the D3lg4doMaps logos.
  - **Assets pulled forward from Step 4:** `static/img/favicon.ico` (copy of `BrandKit/AarxMaps.ico`) and `static/img/aarxmaps-icon.png` (copy of the 256 PNG), so the config never points at missing files. `img/social-card.png` is still missing until Step 4; it's only a meta tag, so the build doesn't fail.
  - **Landing:** `src/pages/index.tsx` is a placeholder (title, tagline, "Get started" button). `index.module.css` and `custom.css` are untouched until Steps 4 and 8.
  - **npm audit:** `npm audit fix` (no `--force`, lockfile only) took it from 40 to 26 advisories (15 critical, 1 high). Everything left is inside Docusaurus's own build chain: `tinypool` 1.1.1 in `@docusaurus/core` and `serialize-javascript` 6.0.2 via the webpack copy/minify plugins. Those only process our own files at build time, nothing ships to readers, and npm's suggested "fix" is a downgrade to 3.7.0. Recheck when Docusaurus ships a patch.
  - **Checks:** name audit (only `coming-from-delgadomaps.md` hits, as expected), isolation audit (clean), Playwright light/dark at 1440 px, no page-level horizontal scroll at 375 px.
- **Category links changed (2026-10-30, after Step 3).** Places and Routes link to their `overview` page, and Core and Getting started have `"link": null`. No `/docs/category/*` pages remain. The build passes, and the overview page is no longer listed as a separate sidebar entry: clicking "Places" or "Routes" opens it.
- **Step 4 done (2026-10-30).**
  - **Palette (`src/css/custom.css`):** Crimson primary in light mode on white. Vermilion primary in dark mode, with page, navbar and sidebar on Ink, Bone text, Mist secondary text, and Graphite only for table stripes and the footer's top border. The footer is Ink in both themes. Shades come from the Docusaurus generator formula (lightness ±10/15/30 %).
  - **Fonts:** Inter (body, variable 400-700) and Sora (headings, variable 600-700), self-hosted as latin-subset woff2 in `static/fonts/` with their SIL OFL 1.1 license files. Headings are 600, and h1 plus the hero title are 700. Docusaurus rewrites the CSS `url('/fonts/...')` to hashed `/AarxMaps/assets/fonts/...` files.
  - **Social card:** `static/img/social-card.png` (1200x630, Ink background, AarxMaps icon, "AarxMaps" in Sora 700, the tagline in Inter, and the Bone Aarexus wordmark bottom right).
  - **NuGet icon:** `icon.png` (128x128, resized from the 256 PNG) at the repo root, packed by all three csproj (`PackageIcon` plus a `None` item next to the README lines). `dotnet build` gives 0 warnings, and each `.nupkg` contains `icon.png` with `<icon>icon.png</icon>` in the nuspec.
  - **Rendering:** `BrandKit/scripts/render_brand.py` (system Pillow) builds both images. Run it from the repo root with `python3 -I BrandKit/scripts/render_brand.py . BrandKit/fonts`. The Sora and Inter variable TTFs it uses (from `google/fonts`, with OFL texts) are in `BrandKit/fonts/`. `BrandKit/` is gitignored, so copy it between machines with the rest of the kit.
  - **Checks:** Playwright in light and dark mode, both fonts reported as loaded, contrast as in Critical decisions; `npm run build` and `typecheck` pass.
- **Step 5 done (2026-10-30).** Nine pages written from `src/Public`: `intro`, `getting-started/{installation,quick-start}`, `core/{configuration,caching,errors,cancellation,coordinates,custom-endpoints}`. The page template is applied where it fits (Example, Options/Rules, See also). Getting started uses Tabs only on Quick start (Places / Routes).
  - **Snippets:** all 16 C# blocks compile with `-warnaserror` (0 warnings, 0 errors), extracted from the pages by script, not retyped. Two scratch projects in the session scratchpad: a console project (console implicit usings) for the intro, console, class-style and `IMapsApiClient` snippets, and a `Microsoft.NET.Sdk.Web` project for the `WebApplication` snippets plus `Microsoft.Extensions.Caching.StackExchangeRedis` 10.0.7 (scratch only, for the Redis line on Caching). Fragments are wrapped in methods with their `using` lines hoisted; class snippets are used verbatim.
  - **Site:** `npm ci && npm run build` passes (no broken links or anchors).
  - **Playwright:** light and dark mode (`browser_emulate_media`). At 1440 px, no table and no code block scrolls sideways on any of the nine pages. At 375 px, there is no page-level horizontal scroll; tables and code scroll inside their own boxes. Admonitions render (3 in total, at most 2 per page). `.playwright-mcp/` deleted.
  - **Audits:** name grep (only `coming-from-delgadomaps.md` hits), isolation grep (clean), no emojis, no backticks in headings, no `DocCardList`. Every backticked identifier on the nine pages exists in `src/Public` or is a framework/HTTP name. The three external Google links (API security, service-specific terms, Places policies) return 200.
  - **Facts the pages state that came from reading Core internals** (recheck if Core changes): the cache key covers type, method, URL, query, payload and only the `Accept-Language`, `Content-Type` and `X-Goog-FieldMask` headers from `MapsApiRequest.Headers`; `ResponseBody` holds at most 4,096 characters; a timeout is a `MapsApiException` with no `StatusCode` and an `OperationCanceledException` inner exception; payloads are camelCase with nulls omitted, and responses are read case-insensitively with numbers allowed as strings.
- **Findings from Step 5 (for the user, no code changed):**
  1. Minor (cache): the configured `Language`/`Region` is added by `RequestFactory` as a default header, so it never reaches `RequestFingerprintCacheKeyStrategy` (which only sees `MapsApiRequest.Headers`). Two apps that share one distributed cache and `Prefix` but have different `Language` settings serve each other's localized responses. Documented on Caching (use a separate `Prefix`). Fix post-v1 by putting the configured language into the key, or bump the key version when the language fix from Known flaws lands.
  2. Minor (README): "All module services are transient and stateless, so they are safe to inject anywhere, including singletons" is misleading. They sit on a typed `HttpClient`, and a singleton that holds one keeps the same handler forever and misses DNS changes (Microsoft's typed-client guidance). The Configuration page says so instead; drop the README line in the Step 11 trim.
  3. Nit (API): a timeout can only be told apart by `InnerException is OperationCanceledException`. A `MapsTimeoutException : MapsApiException` would be a cleaner signal; post-v1, non-breaking.
- **.NET 10 compatibility checked (2026-10-30).** The packages target `net8.0;net9.0`, so a .NET 10 app gets the `net9.0` build. Checked two ways: the whole test suite (1,284 tests, `net9.0` build) passes on the .NET 10.0.8 runtime (`DOTNET_ROLL_FORWARD=LatestMajor dotnet test AarxMaps.Tests -f net9.0`; the testhost diag log confirms 10.0.8), and every Step 5 snippet compiles with `-warnaserror` in `net10.0` console and Web apps (which resolve the `net9.0` build). Installation now has a "Supported .NET versions" table, the Introduction says ".NET 8 and later", and the README's two version lines say the same.
- **Step 6 done (2026-10-30).** Seven Places pages written from `src/Public` and checked against the internals (`GeoFactory`, `RequestValidator`, the three factories' field masks, the mappers and `DetailsService`): `overview`, `search-areas`, `autocomplete`, `text-search`, `nearby-search`, `place-details`, `photos-and-reviews`.
  - **Snippets:** all 27 C# blocks on the site so far (16 from Step 5, 11 Places) compile with `-warnaserror` against `net9.0`, 0 warnings, 0 errors. Places fragments are wrapped in a method taking `IPlacesService places, string placeId, CancellationToken ct`, matching the overview's note that examples assume an injected `places` and a `ct`.
  - **Site:** `npm run build` passes (no broken links or anchors).
  - **Playwright:** light and dark. At 1440 px no table or code block scrolls sideways, including the tables inside `<details>`; at 375 px no page-level horizontal scroll. Collapsed sections were opened by clicking (Docusaurus's `Details` component ignores a scripted `open`). `.playwright-mcp/` deleted.
  - **Audits:** isolation grep (no `Routes` under `places/`), name grep (only the DelgadoMaps page), no emojis, no code in headings, at most one admonition per page. Every backticked identifier exists in Core or Places `src/Public`. Links leave the package only for Core pages (9) and Known limitations (1). All seven Google links used return 200.
  - **Scratchpad reset mid-session:** the Step 5 compose script and projects were gone. The script was rebuilt as one generic `compose.py` (per-page wrapping rules, fully qualified types in wrapper signatures so the wrapper never adds a `using` that could hide a missing one). Moved into the repo afterwards as `docs/scripts/compile-snippets.py`, at the user's request.
- **Notes for Step 8 (from Step 6):**
  - Done 2026-10-30: the Known limitations content list (below) now has every limitation. It previously missed three Places items the README has: search results carry only `PlaceId`, `DisplayName` and `Types`; `GetDetailsAsync` requests a fixed field set (Enterprise + Atmosphere); autocomplete bias and restriction are circles only. Also missing request options Google supports but the builders don't expose (for example `rankPreference`, `excludedTypes`, `openNow`, `minRating`, `includedRegionCodes`) and the autocomplete match start offset.
  - `<details>` boxes use Infima's default info colors (light blue, teal in dark mode), not the brand palette. The user wants them restyled in a separate commit: Step 6 part 2.
  - Docusaurus's automatic previous/next links cross packages (the last Places page links "Next: Routes overview"). That's site navigation, not page content, so it was left as is.
- **Step 6 part 2 done (2026-10-30): `<details>` restyled.** One block in `src/css/custom.css` (see "Details styling"); nothing else changed.
  - **Result:** light mode is a white panel with a 1px border in the table border color (`#dadde1`), no shadow, no thick left accent, Ink text, a Crimson arrow and a neutral divider. Dark mode is an Ink panel with the dark table border (`#606770`), Bone text and a Vermilion arrow. Summaries are weight 600. Code inside keeps the normal code background, and links inside are primary-colored again (Infima's `.alert` had turned them into underlined text color).
  - **Checks:** `npm run build` and `typecheck` pass. Playwright, with the sections opened by clicking: computed styles as above; the `:::warning` admonition is unchanged (own background, 5px left border). Contrast inside the panel: dark text 17.29:1 and a test link 4.98:1 (Vermilion on Ink); light links are Crimson on white, 6.22:1. The 19-row "All properties" table is visible and doesn't scroll at 1440 px; at 375 px the panel fits (328 px) with no page-level scroll. `.playwright-mcp/` deleted.
  - **Correction to Step 6:** its "tables inside `<details>` don't scroll" result came from a check that set `open` from script, which Docusaurus ignores, so those tables were hidden when measured. Re-measured here with real clicks: they don't scroll.
- **Step 7 done (2026-10-30).** Six Routes pages written from `src/Public` and checked against the internals (`RoutesRequestValidator`, both factories' field masks, both mappers and services, `WireEnum`, `CommonMapper`, `RoutesConstants`): `overview`, `waypoints`, `directions`, `route-results`, `distance-matrix`, `travel-mode-rules`.
  - **Spec check:** the code matches every Routes entry in `PROGRESS.md` Critical decisions (v1 scope, model rules, public API, builder rules, wire format, field masks, matrix results). No mismatch found.
  - **Snippets:** all 39 C# blocks on the site (27 earlier, 12 Routes) compile with `-warnaserror` against `net9.0`. New `PAGE_RULES` entries in `docs/scripts/compile-snippets.py`, plus two shared wrappers: `ROUTES_WAYPOINTS` (sync, `origin` + `destination`) and `ROUTES_REQUEST` (async, `routes` + `request` + `ct`). Fragments without an `await` need a sync wrapper (`void Run(...)`), or the async wrapper fails with CS1998.
  - **Name clash checked:** in the Web project, `Route` alone fails with CS0104 (`Microsoft.AspNetCore.Routing.Route`), and `using Route = AarxMaps.Routes.Services.Directions.Route;` builds clean. Route results says so in a warning admonition.
  - **Site:** `npm run build` passes (no broken links or anchors).
  - **Playwright:** the layout check at 1440 px gives `wideTables=0 wideCode=0 pageScroll=false hiddenDetailTables=0 raw=false` on all six pages, with every `<details>` opened (1/1 on Directions, 3/3 on Route results). At 375 px no page-level scroll. Admonitions: at most 2 per page. Two screenshots: Option rules in light mode, Route results in dark mode with the transit section open. `.playwright-mcp/` deleted.
  - **Audits:** isolation grep (no `Places` under `routes/`), name grep, emojis, code in headings, code lines over 80 characters: all clean. Every backticked identifier exists in Core or Routes `src/Public`, except a snippet variable, a Google field path, `SCREAMING_SNAKE_CASE` and the "`Add`/`With`" prose. Links leave the package only for Core pages and Known limitations. The three Google links used return 200; `transitDetails.headway` was checked against Google's `computeRoutes` reference.
  - **Claim dropped:** the plan's "past departure times fail" isn't in Google's reference, which only says a past departure time is valid for Transit. The pages say that and don't claim a 400.
- **Findings from Step 7 (for the user, no code changed):**
  1. Nit (API consistency): `RouteRequestBuilder.From(null)` / `To(null)` are accepted, and `Build()` then says "Origin is required. Call From() before Build()", which is misleading after a `From` call. `WithRouteModifiers(null)` and `WithTransitPreferences(null)` (both builders) silently clear the value. `AddIntermediate`, `AddOrigin` and `AddDestination` throw on `null` instead. Pick one behavior: throwing `MapsInvalidRequestException` on `null` in `From`/`To` is the consistent fix. **Accepted for v1 (user, 2026-10-30):** it's in the Known limitations content list and in `PROGRESS.md` Known flaws, to be fixed in a future version.
  2. Nit: enum arguments are only checked at send time (`WireEnum.ToWire` throws `MapsInvalidRequestException` from the service), so `WithRoutingPreference((RoutingPreference)99)` passes `Build()`. It's documented on the service and on the Directions page; fine for v1.
- **Step 8 done (2026-10-30).** Known limitations, landing page and the DelgadoMaps links are written; every page on the site now has content.
  - **Known limitations** (`docs/docs/known-limitations.md`): every item of the content list, grouped Core / Language and region / Places / Routes, each as "what you can't do" plus the workaround, including the `null`-handling item. Every Google field name was checked against the `searchText`, `searchNearby`, `autocomplete`, `computeRoutes`, `RouteModifiers` and `computeRouteMatrix` reference pages (all present, `avoidTunnels` included). Added `tollPasses` next to `avoidTunnels` (it exists in Google's `RouteModifiers`, and the SDK doesn't expose it). Builder method lists were checked against the Places builders. Dropped the plan's "test against fixture JSON through `IMapsApiClient`" workaround for `internal` setters: not verified, so the page only suggests mocking your own interface.
  - **Coming from DelgadoMaps:** "See also" section with links to every page. No source changed since the map was built (last `src` commit `8ef933c`, before Step 2), so there's nothing to recheck. One code line in the "After" block was split to 80 characters (a line break inside an argument list; that page is skipped by the compile script, so this change wasn't compiled).
  - **Landing page** (`src/pages/index.tsx`, `index.module.css`): an Ink hero (icon, title, tagline, "Get started" + "GitHub"), Install (`dotnet add package` for Places and Routes) with a minimal-API Places snippet in a built-in `CodeBlock`, and three package cards. The Core card links to Configuration and registration, not to the Introduction (the footer keeps the Introduction): "Get started" already covers the general entry point, and the card describes Core's features. Confirmed by the user (2026-10-30).
  - **Snippets:** `compile-snippets.py` now also reads the `EXAMPLE` constant from `index.tsx` (Web project, `Run(string[] args)` wrapper). All 40 snippets compile with `-warnaserror`.
  - **Revised after review (user, 2026-10-30): no code on the landing page.** The Install + example section was removed, because code right on the landing page can overwhelm new readers. The landing page is now the hero followed by the three cards. The `EXAMPLE` hook was removed from `compile-snippets.py` too, so it reads Markdown only again (39 snippets, all compile). The `.section .narrow` rule went with the section. `npm run typecheck` and `npm run build` pass, and the 1440 px screenshot shows the hero followed by the cards. Install and code start at "Get started" (Quick start).
  - **Site:** `npm run typecheck` and `npm run build` pass.
  - **Playwright:** Known limitations and the DelgadoMaps page: no wide tables or code at 1440 px, no page scroll at 375 px. Landing: no page scroll at 1440 or 375 px, no code scroll at 1440 px, and the snippet column is 832 px. Contrast on the Ink hero: title and GitHub button 17.29:1, tagline 7.02:1; "Get started" is white on Crimson (6.22:1) in light mode and Ink on Vermilion (4.98:1) in dark mode; card titles are Vermilion on Ink in dark mode (4.98:1) and Crimson on white in light mode (6.22:1). Screenshots: dark at 1440 px (hero and cards), light at 375 px. `.playwright-mcp/` deleted.
  - **Audits (whole site):** old names only on the DelgadoMaps page, isolation clean, no emojis, no code in headings, no placeholders left. Two code lines in `core/errors.md` (Step 5) are 81-82 characters; Step 5's rendered check found no scroll on them, so they were left alone.
- **Step 9 done (2026-10-31).** The user read the whole site and had no findings: everything they wanted changed was already handled during Steps 5-8.
- **Docs work is done.** The release continues in `RELEASE-PLAN.md` (added 2026-10-31), starting at R1. It replaces Work steps 10 and 11.

## Step 8 inputs (done 2026-10-30)

Written 2026-10-30. Steps 0-7 are done; `docs/docs/known-limitations.md` and `src/pages/index.tsx` are still placeholders. Git stays with the user (the `Dotnet 10 target` and `Csproj metadata` stashes stay unapplied; the docs describe `net8.0;net9.0`).

**Inputs, all in this file:**

1. **Known limitations:** the "Known limitations page content" list (Site structure section). Write each item as what the reader can't do plus the workaround. Then re-read `PROGRESS.md` Known flaws for anything newer, and check every Google field name in the list against Google's reference (they were written from memory). Use the `curl -sL <reference> | sed 's/<[^>]*>//g' | grep ...` trick from Lessons learned. This page may name both Places and Routes; it's shared.
2. **Landing page:** the "Landing page" section (hero, install + 8-12 line snippet, three cards) and the Step 8 note in Work steps about the hero color. `src/components/` is already gone; `index.module.css` still has the template's hero styles.
3. **Coming from DelgadoMaps:** add "See also" links to the new pages (all now exist), and recheck the map against `src/Public` for anything renamed since 2026-10-30.

**Hero style: decided (user, 2026-10-30): Ink hero.** Replace `hero--primary` with an Ink background in both themes and Bone text (Mist for the tagline). Crimson or Vermilion only on the "Get started" button, never as a fill behind Bone text (3.47:1). Check the button's text contrast in both themes.

**Verification gaps to close:**

- `compile-snippets.py` only reads `docs/docs/**/*.md`, so it won't see the landing snippet in `index.tsx`. Compile that snippet by hand in the scratch console project (`--out` dir) with `-warnaserror`, or keep it identical to a page snippet that is already compiled (Quick start), and say which in the Step 8 entry.
- Layout check: change the `pages` list to `['known-limitations', 'coming-from-delgadomaps']`. Check the landing page separately (it has no `article`) at 1440 and 375 px in both themes, by resizing and navigating, plus one screenshot per theme.
- Then all five Verification checks, as the Work steps line says.

## Step 7 procedure (done 2026-10-30)

Written 2026-10-30 for a fresh session. Kept because the verification order and the layout check apply to Step 8 too (change the `pages` list). Everything Step 7 needs is in this file; this section only collects the Routes-specific inputs and the procedure that worked for Steps 5 and 6.

**State.** Steps 0-6 and Step 6 part 2 are done and committed (last commit `1a38211`). The six files under `docs/docs/routes/` are placeholders. Git work is the user's, including two stashes they parked on purpose: `Dotnet 10 target` (`net10.0` added to every csproj, verified with 0 warnings and 1,284 tests per TFM, `publish.yml` installing SDKs 8/9/10, and the docs, README, CLAUDE.md and PROGRESS.md updated to match) and `Csproj metadata`. Don't apply them; the docs must describe the code as it is (`net8.0;net9.0`).

**Read first.**

1. The Routes rows of the Page contents table (Site structure section) and the six placeholder files.
2. All of `AarxMaps.Routes/src/Public` with XML docs, the `<remarks>` in particular.
3. The internals behind any behavior a page states: `Internal/Validation/RoutesRequestValidator.cs` (the travel-mode rules), `Internal/Services/Directions/DirectionsFactory.cs` and `DistanceMatrix/DistanceMatrixFactory.cs` (field masks per `RouteDetailLevel`), both mappers and services, `Internal/Common/WireEnum.cs` and `CommonMapper.cs`, `Internal/Constants/RoutesConstants.cs`.
4. `PROGRESS.md` Critical decisions starting with "Routes" (v1 scope, model rules, public API, builder rules, wire format, field masks, matrix results). They are the spec; check the code against them, and report any mismatch to the user instead of documenting around it.

**Conventions already set** (see Critical decisions and Writing rules for the reasons):

- The overview shows registration plus one full class with an injected `IRoutesService`, then says the other pages assume an injected `routes` and a `CancellationToken ct`. Feature snippets stay short and still include their `using` lines.
- Options tables have three columns (`Method | Required | Rule`, default inside Rule); result tables are `Property | Type | Notes`. Long member lists and secondary types go in `<details>`. Enums get a short table on the page that uses them, with the full member list collapsed (Old page -> new page table).
- Plain-text headings, code lines of at most 80 characters, admonitions as `:::warning[Title]` and at most two per page, no emojis, no `DocCardList`.
- Isolation: no "Places" anywhere under `routes/`; links go only to `routes/`, `core/` and `known-limitations.md`.
- Billing or quota statements only where the XML docs make them; link to Google for the rest: https://developers.google.com/maps/documentation/routes. Check every Google URL with `curl -sL -o /dev/null -w '%{http_code}'`.

**Watch-outs found while writing Steps 5 and 6.**

- `GetRoutesAsync(request, detailLevel = Standard, ct)` and `GetDistancesAsync(request, detailLevel = Standard, ct)` take the detail level second, so pass the token as `ct: ct` or pass both.
- `Route` collides with `Microsoft.AspNetCore.Routing.Route` in ASP.NET Core projects (implicit usings). Use `var` in snippets, or say how to disambiguate if a snippet must name the type.
- `TravelMode.Unknown` is rejected by `WithTravelMode`; an unset travel mode counts as Drive for every rule.
- The distance matrix is streamed, buffered by `GetDistancesAsync`, sorted by origin then destination, never cached, and `Status` is `null` on success. The Core Caching and Cancellation pages already say "never cached" and "the timeout stops at the headers" for it; link to them instead of repeating.
- Polylines are encoded strings, durations `TimeSpan`, timestamps `DateTimeOffset`, and proto3-omitted numbers default to 0 (Critical decisions, Routes model rules).

**Verification, in this order.**

1. `python3 -I docs/scripts/compile-snippets.py --out <scratchpad>/compile`. Routes fragments default to a method taking `IRoutesService routes, CancellationToken ct`. Add a `PAGE_RULES` entry for the overview's registration block (`WEB_BUILDER`, like `places/overview.md`) and for any fragment that needs another variable (for example a prebuilt `Waypoint`); the script stops and names any block it can't place.
2. `cd docs && npm run build` (broken links and anchors fail it).
3. `npm run serve -- --port 3210 --no-open` in the background, then Playwright. **Screenshots are the expensive part:** in Steps 5 and 6 they took most of the session's context. Use the layout check below for every page at 1440 and 375 px, and take only two viewport-sized JPEG screenshots (one page in light mode, one long page in dark mode with a `<details>` opened by clicking). Use `browser_emulate_media` for dark mode. Close the browser, stop the server and delete `.playwright-mcp/` afterwards.
4. Greps: isolation (`grep -rn 'Places' docs/docs/routes/`), the old-name audit in Verification step 4, emojis, backticks in headings, and every backticked identifier against Core + Routes `src/Public`.
5. Record results, decisions and lessons in this file (a "Step 7 done" status entry like Steps 5 and 6), tick Step 7 in Work steps, and update the Phase 4 row in `PROGRESS.md`.

**Layout check** (paste into `browser_evaluate` on any page of the served site; it opens each page in an off-screen iframe at both widths and clicks every `<summary>` before measuring, since Docusaurus ignores a scripted `open`):

```js
async () => {
  const pages = ['overview', 'waypoints', 'directions', 'route-results',
    'distance-matrix', 'travel-mode-rules'].map(p => 'routes/' + p);
  const wait = ms => new Promise(r => setTimeout(r, ms));
  const check = async (width) => {
    const out = [];
    for (const p of pages) {
      const f = document.createElement('iframe');
      Object.assign(f.style, { width: width + 'px', height: '900px',
        position: 'absolute', left: '-5000px' });
      document.body.appendChild(f);
      await new Promise(r => { f.onload = r; f.src = '/AarxMaps/docs/' + p; });
      await wait(400);
      const d = f.contentDocument;
      d.querySelectorAll('article summary').forEach(s => s.click());
      await wait(400);
      const hidden = [...d.querySelectorAll('article details table')]
        .filter(t => t.offsetHeight === 0).length;
      const wideTables = [...d.querySelectorAll('article table')]
        .filter(t => t.scrollWidth > t.clientWidth + 1).length;
      const wideCode = [...d.querySelectorAll('article pre')]
        .filter(x => x.scrollWidth > x.clientWidth + 1).length;
      const text = d.querySelector('article')?.innerText ?? '';
      out.push(`${p}: wideTables=${wideTables} wideCode=${wideCode}` +
        ` pageScroll=${d.documentElement.scrollWidth > d.documentElement.clientWidth}` +
        ` hiddenDetailTables=${hidden} raw=${/:::|<\/?(details|summary)>/.test(text)}` +
        ` admonitions=${d.querySelectorAll('.theme-admonition').length}` +
        ` emoji=${/\p{Extended_Pictographic}/u.test(text)}` +
        ` openDetails=${d.querySelectorAll('article details[data-collapsed="false"]').length}` +
        `/${d.querySelectorAll('article details').length}`);
      f.remove();
    }
    return out.join('\n');
  };
  return '1440:\n' + await check(1440) + '\n\n375:\n' + await check(375);
}
```

Tested 2026-10-30 on the Places pages (it opened 2/2 and 1/1 sections and matched the Step 6 results). Expect at 1440 px: `wideTables=0 wideCode=0 pageScroll=false hiddenDetailTables=0 raw=false` and `openDetails` equal on both sides of the slash. At 375 px only `pageScroll=false` matters; tables and code scroll inside their own boxes on phones.

## Step 0 - Moving to the docs machine

The user handles all git work. Before switching machines:

1. **Commit the untracked files.** As of 2026-10-28, `docs/`, `.github/` and `README.md` are **untracked** (`git status` shows `??`). The other machine won't have the blueprint site, the workflows or the README unless they are committed. `DOC-PLAN.md`, `PROGRESS.md`, `CLAUDE.md`, `.gitignore` and the three XML doc fixes are staged but not committed.
2. **Copy `BrandKit/` manually** (USB, cloud drive). It is gitignored on purpose.
3. **Install on the docs machine:**
   - **.NET SDK 8 and 9.** Needed for `dotnet build` and for compiling doc snippets.
   - **Node.js 20 or later, with npm.** Needed for Docusaurus. CI uses Node 20. Fedora: `sudo dnf install nodejs`; Mint: use the NodeSource repo or `nvm`, because the distro package may be older than 20.
   - **Git** and access to `github.com/Aarexus/AarxMaps`.
4. **Claude Code plugin (recommended, optional):** `playwright` from `claude-plugins-official`. See Tooling.

## Goals

1. **Public API only.** No internal types, DTOs, mappers, `WireEnum`, field-mask strings or cache decorator internals. If a consumer can't type it, it's not in the docs.
2. **Minimal and easy to follow.** One page per task. A reader should get a working call in under a minute, and full property lists stay collapsed.
3. **Package isolation.** Places pages never mention Routes, and Routes pages never mention Places. Shared topics live under Core. The Introduction page is the only exception: one combined example.
4. **Aarexus branding** with an AarxMaps product mark.

## The blueprint (inherited D3lg4doMaps site)

The old site is a **starting point, not a replica**. It has the right overall shape, but the content is wrong and too heavy.

**Kept from it:**

- Docusaurus classic preset, the `docs/` folder layout (`docs/docs`, `src/pages`, `src/css`, `static/img`) and the `docs.yml` deploy workflow.
- Sections per package (Core, Places, Routes) with an overview page each.
- An autogenerated sidebar with one `_category_.json` per folder and `sidebar_position` front matter per page.
- The navbar (Docs + GitHub), the three-column footer, light/dark mode with `respectPrefersColorScheme`, and the `prism` setup (`github` / `dracula` themes, `csharp`).
- A landing page with a hero and a "Get started" button.
- Topic coverage: every topic below is covered again, unless it's marked as dropped.

**Changed:**

- **Content is rewritten from `src/Public`.** The old pages describe the old API: `AddDelgadoMaps`, `SearchByTextAsync(string)`, no `CancellationToken`, `MapsLoggingOptions`, public `IMapsJsonSerializer`/`IRequestFactory`, `PolylineEncoding`, `ComputeRoute`. Old pages are a list of topics, never a source of content.
- **Pages are organized by task.** One page per thing a user wants to do, instead of one page per code category (abstractions/models/enums/extensions).
- **No emojis.** Not on headings, bullets or links.
- **Smaller landing page.** The six feature cards with SVG illustrations and the "Why" block are gone.
- **No migration pages and no version banners.** One "Coming from DelgadoMaps" name map replaces them.
- **No "Services" subfolder.** Each package has five to seven flat pages, which is short enough without the extra level.

### Old page -> new page

| Old page | Goes to | Notes |
| --- | --- | --- |
| `intro.md` | `intro.md`, `getting-started/*` | Drop the v2/v3 banners, emoji headers and "Design philosophy". |
| `core/overview.md` | `getting-started/quick-start.md`, Core category index | |
| `core/configuration.md` | `core/configuration.md`, `core/caching.md` | `MapsLoggingOptions` no longer exists: drop it. |
| `core/abstractions.md` | `core/custom-endpoints.md` | Only `IMapsApiClient` is public now. `IMapsJsonSerializer` and `IRequestFactory` are internal: drop them. |
| `core/models.md` | `core/coordinates.md` (`LatLng`), `core/custom-endpoints.md` (`MapsApiRequest`, `StreamResponse`) | |
| `core/enums.md` | `core/custom-endpoints.md` (`ApiKeyLocation`) | |
| `core/exceptions.md` | `core/errors.md` | |
| `core/extensions.md` | `core/configuration.md` (DI), `core/caching.md` (cache DI), `core/custom-endpoints.md` (`JsonExtensions`, `SendAndEnumerateAsync`) | |
| `migration/v2.md`, `migration/v3.md` | dropped | Replaced by `coming-from-delgadomaps.md`. |
| `places/overview.md`, `places/extensions.md`, `places/abstractions.md` | `places/overview.md` + each feature page | Each service interface is shown on its own feature page. |
| `places/services/autocomplete.md` | `places/autocomplete.md` | |
| `places/services/search.md` | `places/text-search.md`, `places/nearby-search.md`, `places/search-areas.md` | |
| `places/services/details.md` | `places/place-details.md`, `places/photos-and-reviews.md` | |
| `routes/overview.md`, `routes/extensions.md`, `routes/abstractions.md` | `routes/overview.md` + each feature page | |
| `routes/routing-components.md` | `routes/waypoints.md`, `routes/directions.md` (modifiers, transit preferences) | `VehicleInfo` is flattened to `EmissionType`, and `RouteMatrixDestination` is now a plain `Waypoint`. |
| `routes/route-metadata.md` | `routes/route-results.md` | `SpeedInterval` and leg travel advisories are not in v1: drop them. |
| `routes/enums.md` | Inline on the page that uses each enum | Each page has a short table, with the full member list collapsed. `PolylineEncoding` and `Speed` no longer exist. |
| `routes/services/directions.md` | `routes/directions.md` | |
| `routes/services/distanceMatrix.md` | `routes/distance-matrix.md` | |

## Site structure

Paths are relative to `docs/docs/`. Each folder gets a `_category_.json` (label, position, link), and each page sets `sidebar_position`. Links: Places -> `places/overview`, Routes -> `routes/overview` (as `{ "type": "doc", "id": ... }`); Core and Getting started -> `null`, so they are plain groups. No generated-index pages (see Critical decisions).

```
intro.md                          Introduction                 (position 1)
getting-started/                  Getting started              (position 2)
  installation.md
  quick-start.md
core/                             Core                         (position 3)
  configuration.md                Configuration and registration
  caching.md                      Caching
  errors.md                       Errors
  cancellation.md                 Cancellation and timeouts
  coordinates.md                  Coordinates (LatLng)
  custom-endpoints.md             Custom endpoints (IMapsApiClient)
places/                           Places                       (position 4)
  overview.md
  search-areas.md                 Bias vs restriction, circle vs rectangle
  autocomplete.md
  text-search.md
  nearby-search.md
  place-details.md
  photos-and-reviews.md
routes/                           Routes                       (position 5)
  overview.md
  waypoints.md
  directions.md                   Request side
  route-results.md                RouteResult -> Route -> RouteLeg -> RouteStep, transit, localized values
  distance-matrix.md
  travel-mode-rules.md            Option x travel mode table
known-limitations.md              Known limitations            (position 6)
coming-from-delgadomaps.md        Coming from DelgadoMaps      (position 7)
```

### Page contents

Every name below must be checked against `src/Public` while writing.

| Page | Covers | Source types |
| --- | --- | --- |
| Introduction | What AarxMaps is (three packages, one line each), the one combined example (Places text search -> place ID -> Routes directions), links to Getting started. | README intro |
| Installation | Requirements (.NET 8/9, a Google Cloud key with Places API (New) and/or Routes API enabled), `dotnet add package AarxMaps.Core / .Places / .Routes`, and the fact that Places and Routes each depend on Core. | csproj |
| Quick start | `AddAarxMaps` + a module, resolve or inject the service, one call. Uses Docusaurus `Tabs`: Places / Routes, with an independent example in each. | `MapsServiceCollectionExtensions`, `AddAarxMapsPlaces`, `AddAarxMapsRoutes`, `IPlacesService`, `IRoutesService` |
| Configuration | `MapsConfiguration` table: `ApiKey` (required), `Language` (default `"en"`), `Region` (default `"US"`), `RequestTimeout` (default 30 s). Also: binding from `appsettings`/user secrets, validation on start, registration order, double `AddAarxMaps()` throws, modules are idempotent, `IsAarxMapsRegistered`. | `MapsConfiguration`, `AddAarxMaps` |
| Caching | `AddAarxMapsMemoryCache` vs `AddAarxMapsDistributedCache` (only one, after `AddAarxMaps`). `MapsCachingOptions` table: `Prefix` (`"aarxMaps"`), `AbsoluteExpiration` (30 min), `SlidingExpiration` (30 min, always on). Also: never cached (streaming/matrix, photos), cache failures act as a miss, Google's terms (only place IDs may be stored indefinitely), the shared-`IMemoryCache` `SizeLimit` pitfall. | `MapsCachingOptions`, cache DI methods |
| Errors | Hierarchy table: `MapsApiException` (base: network, timeout, other statuses), `MapsApiAuthException` (401/403), `MapsInvalidRequestException` (400 and builder validation), `MapsNotFoundException` (404), `MapsRateLimitException` (429, `RetryAfter`), `MapsCacheException`. `StatusCode` is an `HttpStatusCode`. One `catch` example. | `Exceptions/*` |
| Cancellation and timeouts | Every method takes `ct`. `RequestTimeout` surfaces as `MapsApiException`; caller cancellation surfaces as `OperationCanceledException`. | `MapsConfiguration`, exceptions |
| Coordinates | `new LatLng(latitude, longitude)`: argument order, ranges (-90..90, -180..180), NaN rejected with `MapsInvalidRequestException`. | `LatLng` |
| Custom endpoints | Calling any Google endpoint through `IMapsApiClient`: `MapsApiRequest`, `SendAsync<T>` (a `JsonDocument` result is caller-owned), `SendStreamAsync` + `StreamResponse` (`await using`), `SendAndEnumerateAsync`, `JsonExtensions`, `ApiKeyLocation`, `Cacheable`. | Core `Abstractions`, `Models`, `Extensions`, `Enums` |
| Places overview | `AddAarxMapsPlaces`, `IPlacesService` (`Autocomplete`, `Search`, `Details`), namespace table, and which feature to use when. | `IPlacesService` |
| Search areas | `LocationBias` vs `LocationRestriction`, circle vs rectangle, radius (0, 50 000] m, rectangle given as (south-west, north-east), antimeridian allowed. Linked from the three search pages. | `GeoCircle`, `GeoRectangle`, `LocationBias`, `LocationRestriction` |
| Autocomplete | `AutocompleteRequestBuilder`, session tokens (one token: the autocomplete calls, then one Details call), `SuggestPlacesAsync`, `PlaceSuggestion`. | `Services.Autocomplete` |
| Text search | `TextRequestBuilder`, page size 1-20 (default 20), paging loop (`NextPageToken` is null at the end), `TextSearchResult`, `PlaceSearchInfo`. | `Services.Searching` |
| Nearby search | `NearbyRequestBuilder`, type filters (max 50), `SearchNearbyAsync`, `NearbySearchResult`. | `Services.Searching` |
| Place details | `DetailsRequestBuilder`, `GetDetailsAsync`, `PlaceDetails` (common properties shown, full list collapsed), sub-models (`PostalAddress`, `GoogleMapsLinks`, `PaymentOptions`, `ParkingOptions`, `PriceLevel`), `GetDetailsRawAsync` with disposal. Response URIs are http/https only. | `Services.Details` |
| Photos and reviews | `GetPhotosAsync` + `PhotoOptionsBuilder` (never cached), `GetReviewsAsync`, `ReviewInfo`, `ReviewSummary`. | `PlacePhotos`, `PlaceReviews`, `PhotoOptions` |
| Routes overview | `AddAarxMapsRoutes`, `IRoutesService` (`Directions`, `DistanceMatrix`), namespace table, `RouteDetailLevel` (Summary / Standard / Full: what each returns). | `IRoutesService`, `RouteDetailLevel` |
| Waypoints | `WaypointBuilder`: place ID, `LatLng` location or address (exactly one); heading, side of road, via, stopover. | `WaypointBuilder`, `Waypoint`, `Location` |
| Directions | `RouteRequestBuilder` (common options shown, full table collapsed), intermediates (max 25) and optimization, `RouteModifiers`, `TransitPreferences`, departure/arrival time, units, `PolylineQuality`, `GetRoutesAsync`, `GetRoutesRawAsync`. | `Services.Directions`, `Common.Models` |
| Route results | `RouteResult`, `Route`, `RouteLabel` (use `DefaultRoute`, not a "best route"), `RouteLeg`, `RouteStep`, `NavigationInstruction`/`Maneuver`, `MultiModalSegment`, `TransitDetails` and its parts, `Viewport`, localized values, `RouteTravelAdvisory`, `Money`, `FallbackInfo`. Durations are `TimeSpan`, timestamps are `DateTimeOffset`, polylines are encoded strings. | Directions models |
| Distance matrix | `DistanceRequestBuilder`, limits (origins x destinations <= 625, or 100 with TrafficAwareOptimal/Transit; <= 50 place-ID/address waypoints), `GetDistancesAsync`, `RouteMatrixElement` (sorted by origin then destination, `Status` null on success, `RouteMatrixElementCondition`), never cached. | `Services.DistanceMatrix` |
| Option rules by travel mode | One table of option x travel mode -> allowed, plus the builder error. Rules: departure xor arrival; arrival only for Transit; routing preference only for Drive/TwoWheeler; traffic model only for Drive + TrafficAwareOptimal; transit preferences only for Transit; heading/stopover/side of road only for Drive/TwoWheeler; via not allowed on origin/destination; unset mode counts as Drive. Not validated (documented only): past departure times, transit time windows. | Builder behavior (described, not the internal validator) |
| Known limitations | See the list below. | `PROGRESS.md` Known flaws |
| Coming from DelgadoMaps | Done in Step 2. Before/after example, the changes that affect most code, then per package: short two-column tables for packages and renamed types, and lists grouped by type for members (`old` → `new`, plus a short note). No version history beyond one line for 1.x/2.x users. | D3lg4doMaps 3.0.1 packages (reflection) + current API |

### Known limitations page content

Complete list, consolidated 2026-10-30 from `PROGRESS.md` Known flaws and Phase 3 optional items, the README's Known limitations, and the Step 5 and Step 6 findings. Write each as what the reader can't do, plus the workaround when there is one. Check every Google field name below against Google's reference while writing; they're listed from memory of the API surface, not verified.

- **Core**
  - Response models have `internal` setters, so consumers can't build populated results (`PlaceDetails`, `RouteResult`, `RouteMatrixElement`, ...) when mocking the services in their own tests. Workaround: mock at your own abstraction, or test against fixture JSON through `IMapsApiClient`.
  - `SlidingExpiration` can't be turned off. Workaround: set it equal to `AbsoluteExpiration`.
  - The configured `Language` isn't part of the cache key. Apps that share one distributed cache and `Prefix` but use different languages serve each other's responses. Workaround: one `Prefix` per app.
  - No built-in retries, including on 429 and 5xx. Workaround: your own policy, honoring `MapsRateLimitException.RetryAfter`.
  - A timeout has no dedicated exception type; it's a `MapsApiException` whose `InnerException` is an `OperationCanceledException`.
  - Custom endpoints: errors are mapped from the HTTP status only, so APIs that report errors inside a 200 body (several legacy web services) aren't detected.
- **Places and Routes:** `languageCode` / `regionCode` are not sent as request fields. Only an `Accept-Language` header is sent, built from `Language`-`Region`. `Region` has no documented effect, and `Language` depends on Google honoring the header.
- **Places**
  - Text and nearby search results carry only `PlaceId`, `DisplayName` and `Types`. Workaround: a Details call per place.
  - `GetDetailsAsync` always requests the same field set, billed as Enterprise + Atmosphere. Workaround: `GetDetailsRawAsync` with only the fields you need.
  - Autocomplete bias and restriction accept circles only (Google also accepts rectangles).
  - `PlaceSuggestion` exposes only where the first match ends (`EndOffset`), not where it starts, so matched text can't be highlighted reliably.
  - Request options Google supports that the builders don't expose: Text search `openNow`, `minRating`, `priceLevels`, `rankPreference`, `strictTypeFiltering`, `includePureServiceAreaBusinesses`; Nearby search `excludedTypes`, `includedPrimaryTypes`, `excludedPrimaryTypes`, `rankPreference`; Autocomplete `includedRegionCodes`, `origin`, `inputOffset`, `includeQueryPredictions`. Workaround for any of them: `IMapsApiClient` with your own request (see Custom endpoints).
- **Routes (v1 scope)**
  - No `extraComputations`: no traffic-on-polyline speed intervals and no toll info.
  - No `AvoidTunnels` modifier.
  - No travel advisories at leg and step level (route level only).
  - No `geocodingResults` for address waypoints.
  - Transit details are minimal: no headway and no agency phone number.
  - `GetDistancesAsync` buffers the whole matrix before returning; there is no streaming (`IAsyncEnumerable`) method. Workaround: `SendAndEnumerateAsync` on `IMapsApiClient`.
  - No helper to decode encoded polylines.
  - `null` handling is inconsistent across the builders (added 2026-10-30, to be fixed in a future version). `RouteRequestBuilder.From(null)` / `To(null)` don't throw; `Build()` then reports a missing origin or destination. `WithRouteModifiers(null)` and `WithTransitPreferences(null)` clear the value instead of throwing. `AddIntermediate`, `AddOrigin` and `AddDestination` do throw on `null`. Workaround: don't pass `null`; call the method only when you have a value.
- Re-check `PROGRESS.md` Known flaws right before release for anything added later.

## Page template

Every feature page uses the same skeleton. Drop any section that doesn't apply.

```md
---
sidebar_position: N
title: <Feature>
---

# <Feature>

One or two sentences: what it does and when to use it.

## Example
Minimal working snippet (5-20 lines), `ct` passed, `using` lines included.

## Options
Table: builder method | required | default | rule/limit.

## Result
Table of the 4-8 properties people actually use.
<details><summary>All properties</summary> full table </details>

## Rules and errors
Bullets: what the builder rejects (MapsInvalidRequestException) and Google-side gotchas.

## See also
Links within the same package or to Core only.
```

Target length: about two screens before the collapsed sections.

## Writing rules

- No emojis. Plain headings, short sentences, second person ("you").
- Callouts only through admonitions (`:::note`, `:::tip`, `:::warning`), at most two per page. A title goes in brackets, `:::warning[Title]`; the space form (`:::warning Title`) renders as raw text with the v4 flags on.
- Keep code lines within 80 characters: that is what fits a code block at 1440 px without a sideways scroll.
- Code is C# only. Consumer style: no `ConfigureAwait`, always pass `ct` where the method takes one, include `using` lines.
- Type and member names exactly as in `src/Public`. Never copy names from the old docs, and recheck any taken from the README.
- Don't restate Google's reference. Link to it for field meanings, billing and quotas:
  - Places API (New): https://developers.google.com/maps/documentation/places/web-service/op-overview
  - Routes API: https://developers.google.com/maps/documentation/routes
- Isolation: links from a Places page go only to Places or Core pages. Same for Routes.
- Use only built-in Docusaurus components: `Tabs`/`TabItem` (`@theme/Tabs`), `<details>`, admonitions and code blocks with titles (`title="Program.cs"`). No custom MDX components.

## Landing page

Rewrite `src/pages/index.tsx`, and delete `src/components/HomepageFeatures/`, `src/components/HomepageComplements/`, `src/pages/markdown-page.md` and the six feature SVGs in `static/img/`.

1. **Hero:** AarxMaps icon, name, a one-line tagline, and two buttons: "Get started" (-> `/docs/getting-started/quick-start`) and "GitHub".
2. ~~**Install + snippet:** `dotnet add package` and an 8-12 line example.~~ Dropped (user, 2026-10-30): no code on the landing page; see Critical decisions.
3. **Three cards** (Core, Places, Routes): one sentence each, linking to the overview page. No illustrations.

Keep `index.module.css` small: hero padding, cards grid, and a mobile breakpoint.

## Branding

**Decision:** use the Aarexus brand system (palette, wordmark). AarxMaps uses its own icon, which is the Aarexus logo plus a map element, designed by the user. No independent palette.

**Source files:** `BrandKit/` at the repo root. It is gitignored and copied manually between machines. CI builds from the repo, so every file the site or packages use gets copied or rendered into a tracked location.

| Asset | From | Goes to | Used by |
| --- | --- | --- | --- |
| AarxMaps icon PNG | `BrandKit/PNGs/AarxMaps-icon-256.png` | `docs/static/img/aarxmaps-icon.png` | navbar `logo.src`, hero. No `srcDark`: the icon has its own Ink background and reads on both themes. |
| Favicon `.ico` | `BrandKit/AarxMaps.ico` (16-256, copied as-is) | `docs/static/img/favicon.ico` | `favicon` |
| Social card 1200x630 PNG | Composed with Pillow: AarxMaps icon (512) + Aarexus wordmark PNG + palette background | `docs/static/img/social-card.png` | `themeConfig.image` |
| NuGet icon 128x128 PNG | Resized with Pillow from the 256 PNG | `icon.png` at the repo root, packed by all three csproj | `<PackageIcon>icon.png</PackageIcon>` + `<None Include="..\icon.png" Pack="true" PackagePath="\" />` |
| Aarexus palette | `BrandKit/` | `docs/src/css/custom.css` | Infima variables |
| Brand fonts | Inter + Sora from Google Fonts (latin woff2), OFL texts from `google/fonts` | Self-hosted in `docs/static/fonts/` | `--ifm-font-family-base` (Inter), `--ifm-heading-font-family` (Sora) |

**No SVGs are published (decided 2026-10-29).** The vector sources stay private in `BrandKit/`; everything public is PNG or ICO. Resizing and composing use the system Python's Pillow from a script in the session scratchpad, so nothing is added to the project and no extra system tool is needed.

**Using `brand-kit.md` (2026-10-29).** It describes Aarexus, not AarxMaps, but AarxMaps inherits the Aarexus system, so most of it applies:

- **Applies:** palette, fonts, the contrast notes, clear space and minimum sizes (they apply to the AarxMaps icon too), and the wordmark rules for the social card.
- **Doesn't apply:** the Aarexus favicon spec (the plain weave), because AarxMaps has its own `.ico`. Also the branch lockups (not exported) and the StocksWarden note.
- **Primary color:** Crimson in light mode (5.5:1 on Bone, higher on white). Vermilion in dark mode, because the kit forbids Crimson as text on dark (3.2:1 on Ink) and Vermilion reaches 5.0:1. Dark surfaces use Ink/Graphite, and secondary text uses Mist.

**Palette -> Infima:** set `--ifm-color-primary` and its six shades (`-dark`, `-darker`, `-darkest`, `-light`, `-lighter`, `-lightest`) in `:root` and again in `[data-theme='dark']`, using a lighter primary for dark mode. Docusaurus's color generator (https://docusaurus.io/docs/styling-layout#styling-your-site-with-infima) produces the shades from one hex value. Check text contrast on links and buttons in both themes (WCAG AA, 4.5:1).

Delete `D3lg4doMapsLogo.*` and `D3lg4doMapsMinimalistLogo.*` once the new files are in.

## Site configuration

### `docusaurus.config.ts`

| Key | Value |
| --- | --- |
| `title` | `'AarxMaps'` |
| `tagline` | One line, final wording while writing (e.g. "Strongly typed .NET SDK for Google Maps Platform") |
| `favicon` | `'img/favicon.ico'` |
| `url` | `'https://aarexus.github.io'` |
| `baseUrl` | `'/AarxMaps/'` |
| `organizationName` / `projectName` | `'Aarexus'` / `'AarxMaps'` |
| `onBrokenLinks` | `'throw'` (keep) |
| `markdown.hooks.onBrokenMarkdownLinks` | `'throw'` (add) |
| `docs.editUrl` | `'https://github.com/Aarexus/AarxMaps/tree/main/docs/'`. The old value is missing `docs/`. |
| `themeConfig.image` | `'img/social-card.png'`. The old value points to a missing file. |
| navbar | title `AarxMaps`, logo alt `AarxMaps`, `src`/`srcDark` as above; items: Docs (`docSidebar` `tutorialSidebar`), GitHub `https://github.com/Aarexus/AarxMaps` |
| footer `style` | `'dark'` (keep) |
| footer copyright | `` `Copyright © ${new Date().getFullYear()} Aarexus Software. Built with Docusaurus.` `` |
| versioning | none: no `docs.versions`, no `versions.json` |

**Footer columns:** same three as the blueprint, rebranded.

- **Packages:** Core -> `/docs/intro` (the general AarxMaps overview), Places -> `/docs/places/overview`, Routes -> `/docs/routes/overview`.
- **SDK Links:** GitHub -> `https://github.com/Aarexus/AarxMaps`, NuGet -> `https://www.nuget.org/packages?q=AarxMaps`.
- **Personal Info:** unchanged.
  - GitHub -> `https://github.com/JSebas-11`
  - LinkedIn -> `https://www.linkedin.com/in/jsebas11/`
  - Instagram -> `https://www.instagram.com/juans3_11.py/`

Core has no overview page of its own. Its footer link goes to the Introduction, which is the general AarxMaps overview (decided 2026-10-28).

### `package.json`, `sidebars.ts`

- `name`: `aarxmaps-docs`.
- Upgrade every `@docusaurus/*` package from 3.9.2 to **3.10.2**. That is npm's `latest` as of 2026-10-28; 4.0 is canary only. Read the 3.10 release notes for config changes before upgrading. Run `npm install` and commit the regenerated `package-lock.json`, because CI runs `npm ci`. If a newer stable 3.x or 4.x is out by then, ask before jumping a major version.
- No new npm dependencies.
- `sidebars.ts` stays autogenerated (`tutorialSidebar`, `dirName: '.'`). Ordering comes from `_category_.json` and `sidebar_position`.

### GitHub

Pages must be enabled on `Aarexus/AarxMaps` (Settings -> Pages -> Source: GitHub Actions) before `docs.yml` can deploy. `docs.yml` runs on push to `main` when `docs/**` changes. The user does this.

## Tooling

**Docusaurus:** classic preset only. Everything the plan needs (tabs, collapsible sections, admonitions, code titles, dark mode) is built in. No search plugin (decided), no Mermaid, no ideal-image.

**Claude Code plugins** (from `claude-plugins-official`, checked 2026-10-28):

| Plugin | Verdict | Why |
| --- | --- | --- |
| `playwright` (Microsoft Playwright MCP) | **Recommended** | It is the only way for Claude to see the rendered site: screenshots in light/dark mode and at phone width, broken layouts, the landing page. It runs `npx @playwright/mcp@latest` (unpinned) and downloads browser binaries on first use. It's an Apache-2.0 dev tool, not a project dependency; ask the user before installing. |
| `frontend-design` (Anthropic, skill only) | Skip | It aims for "distinctive, creative" UI, which pulls against a minimal, brand-driven docs site. The landing page is three blocks; it doesn't need it. |
| `context7` (Upstash, hosted MCP) | Skip | It looks up current Docusaurus docs through a third-party server. Fetching docusaurus.io directly does the same job. |
| `mintlify` | Skip | It's a different docs platform. |

Already installed and useful: `csharp-lsp` (checking type/member names while writing).

## Verification

Run these before marking any writing step done:

1. **Snippets compile.** From the repo root: `python3 -I docs/scripts/compile-snippets.py --out <dir outside the repo>` (default: the system temp folder). It writes a console and an ASP.NET Core scratch project (`net9.0`, warnings as errors, references to the three packages, plus `Microsoft.Extensions.Caching.StackExchangeRedis` in the Web one), extracts every `csharp` block, builds both and exits 1 on any warning or error. It never runs the code: there are no real keys and no live Google calls.
   - A fragment that needs variables the page assumes is wrapped by a per-folder default (`places`: `IPlacesService places, string placeId, CancellationToken ct`; `routes`: `IRoutesService routes, CancellationToken ct`) or by an explicit entry in `PAGE_RULES`. A block with no rule stops the script with the page and block index, so add a rule when a new page needs one.
   - `coming-from-delgadomaps.md` is skipped: its "before" block targets the old packages (Step 2 compiled it against the 3.0.1 NuGet cache).
2. **Site builds.** `cd docs && npm ci && npm run build`, with no broken links or broken markdown links (both throw).
3. **Visual check** (Playwright, installed): `npm run serve`, then check every page in light and dark mode, and the landing page plus one long page at 375 px wide.
   - Also check that no table scrolls sideways at 1440 px (`table.scrollWidth > table.clientWidth`).
   - The Playwright MCP writes snapshots and screenshots to `.playwright-mcp/` at the repo root. Delete it after each check, or add it to `.gitignore` (the user's call).
4. **Names audit.** Grep the old names in `docs/docs` and expect no hits: `DelgadoMaps`, `D3lg4do`, `AddDelgadoMaps`, `ComputeRoute`, `PolylineEncoding`, `MapsLoggingOptions`, `IMapsJsonSerializer`, `IRequestFactory`, `SearchByNearbyAsync`. The only exception is the "Coming from DelgadoMaps" page.
5. **Isolation audit.** Grep `docs/docs/places` for `Routes` and `docs/docs/routes` for `Places`, and expect no hits.

## README trim

Do this after the site is live, so the links resolve.

- **Keep:** intro paragraph, packages table, requirements, install, the combined quick start, a "Documentation" link list to each site section, and license.
- **Move to the site:** everything else.
- **Links** stay absolute, because the README is packed as the NuGet readme.

This supersedes the 2026-10-25 "README scope" decision in `PROGRESS.md`.

## Work steps

Claude does every step and the user reads the result. The user handles all git work (branch `Phase4-Docs`, commits, PR `Phase4: Docs`, merge); Claude never runs git write commands.

0. [X] Move to the docs machine (see Step 0 above). Checked 2026-10-29; the only open item is the user committing `docs/`.
1. [X] Planning and open questions (2026-10-28). XML doc fixes done.
2. [X] Build the DelgadoMaps name map from the old pages and the current API, before deleting anything. Write it straight into `coming-from-delgadomaps.md`. Done 2026-10-30.
3. [X] Clean up `docs/` (done 2026-10-30):
   - Delete the old pages, category files, homepage components, `markdown-page.md` and the old images.
   - Upgrade to 3.10.2 and apply the config above.
   - Add the new folders with their `_category_.json` files and placeholder pages.
   - Expect `npm run build` to pass.
4. [X] Branding (done 2026-10-30): copy/render the assets, palette into `custom.css`, fonts. Check both themes. Add the NuGet icon to the three csproj files and expect `dotnet build` to give 0 warnings. `BrandKit/` is in (2026-10-29); the only open item is the missing icon SVG (see Open questions).
5. [X] Write Introduction, Getting started and Core pages, then verify. Done 2026-10-30.
6. [X] Write Places pages, then verify. Done 2026-10-30.
   - [X] Part 2: restyle the `<details>` boxes (user's request, 2026-10-30), in its own commit before Step 7. Done 2026-10-30, see "Details styling" below.
7. [X] Write Routes pages, then verify. Done 2026-10-30.
8. [X] (Done 2026-10-30.) Write Known limitations and the landing page; finish Coming from DelgadoMaps (add "See also" links to the new pages, recheck the map against `src/Public` for anything renamed since 2026-10-30). Run all five verification checks. Landing: Ink hero with the icon, Crimson/Vermilion only for the button (decided 2026-10-30, see Critical decisions).
9. [X] User reads everything; fix findings. Done 2026-10-31, no findings.
10. Moved to `RELEASE-PLAN.md` R4 (deploy the site, merge to `main`).
11. Moved to `RELEASE-PLAN.md` R5-R6 (README trim, package checks); publishing is R9.

## Details styling (Step 6 part 2)

**Problem (user, 2026-10-30):** collapsible sections look off-brand. Docusaurus's `Details` theme component hard-codes Infima's `alert alert--info` classes, so every `<details>` renders as a light-blue info callout (teal in dark mode), the only element on the site outside the Aarexus palette.

**Fix, CSS only, in `src/css/custom.css`.** No swizzling (same reason as the `DocCard` decision: a theme override to maintain).

Implemented 2026-10-30 as planned below, with two adjustments: the dark border is the dark table border (`#606770`), not Graphite, which was nearly invisible on Ink (1.2:1); and the block also resets Infima's alert overrides of `--ifm-link-color` and `--ifm-code-background`, so links and inline code inside look like the rest of the page.

- Selector `.markdown details.alert`. Element plus Infima class, both public; admonitions are `div`s, so they're untouched.
- Override the Infima alert variables on it: `--ifm-alert-background-color`, `--ifm-alert-border-color`, `--ifm-alert-foreground-color` (and `--ifm-alert-border-left-width` to drop the thick left accent), plus `--docusaurus-details-decoration-color`, which Docusaurus documents as meant to be overridden (arrow and divider color).
- Proposed look: a quiet bordered panel that matches the tables.
  - Light: white background, a 1px neutral border (a light shade of Bone/Mist), Ink text, Crimson arrow, neutral divider.
  - Dark: Ink background (same as the page, so Vermilion links inside keep 4.98:1; never Graphite, which drops them to 4.30:1), a 1px Graphite border, Bone text, Vermilion arrow.
  - Summary at weight 600. Keep the default radius and padding.
- Check: light and dark, with a section open (click the `summary` in Playwright), on Place details and Photos and reviews; link contrast inside the panel; tables inside still don't scroll at 1440 px.

## Open questions

- Brand details resolved 2026-10-29: fonts are Sora 600/700 (headings) and Inter 400/500 (body); no dark-mode icon variant is needed; no SVGs are published.

## Critical decisions

- **Blueprint = starting point (2026-10-28).** The inherited site gives the shape (Docusaurus setup, package sections, site shell, topic list). Content and page layout are new, minimal and easy to follow.
- **Docusaurus latest stable (2026-10-28).** 3.10.2, classic preset, no extra npm packages.
- **Task-oriented structure (2026-10-25).** One page per task, grouped Getting started / Core / Places / Routes. No "Services" subfolders. Sidebar autogenerated from `_category_.json` + `sidebar_position`, as in the blueprint.
- **Package isolation (2026-10-25).** Places and Routes pages never reference each other. Shared topics live under Core. Only the Introduction has a combined example.
- **Public API only, rewritten from source (2026-10-25).** No old page content is ported. Names are checked against `src/Public`.
- **Site is the source of truth (2026-10-25).** The README gets trimmed once the site is live.
- **No emojis (2026-10-25).** Admonitions only for callouts.
- **Minimal landing (2026-10-25, revised 2026-10-30).** Hero and three package cards, no illustrations. The install + snippet block was built in Step 8 and then removed at the user's request: code right on the landing page can overwhelm new readers, so it starts at "Get started" (Quick start).
- **Ink hero (2026-10-30).** The landing hero is Ink in both themes with Bone text, and the brand colors only appear on the button. Infima's `hero--primary` would fill it with Crimson (light) or Vermilion (dark): a loud full-width block, and Bone text on Vermilion fails AA (3.47:1).
- **Hosting (2026-10-25).** `https://aarexus.github.io/AarxMaps/` through the existing `docs.yml`.
- **Branding (2026-10-25, 2026-10-28).** Aarexus palette and wordmark; AarxMaps icon = Aarexus logo + map element. Originals in gitignored `BrandKit/`, tracked copies in `docs/static/img/` and `icon.png`.
- **Extras (2026-10-25).** Known limitations page and a DelgadoMaps name map are in. No search plugin, no separate raw-JSON page (raw methods are covered on their feature pages).
- **No versioned docs (2026-10-28).** One live version; no version banners or migration pages like the old site.
- **v1 limitations accepted (2026-10-28).** `internal` setters on response models and the always-on sliding expiration ship in 1.0.0 and are documented on the Known limitations page.
- **Footer (2026-10-28).** The blueprint's three columns stay, rebranded, with the personal links unchanged. The Core link points to the Introduction (general overview).
- **Authorship and git (2026-10-28).** Claude writes the site, and the user reads it and owns all git work.
- **Plugins (2026-10-28, installed 2026-10-29).** Only `playwright`, for rendered-page checks.
- **No public SVGs (2026-10-29).** The user keeps vector brand sources private. The site and packages use PNG/ICO only; Pillow does the resizing and composing.
- **Name map shape (2026-10-30).** The plan said "one table". It's lists grouped by type instead, plus short two-column tables. The first version used three-column tables, and six of ten scrolled sideways even at 1440 px. Ground truth for old names is the 3.0.1 packages, not the old pages.
- **Self-hosted fonts (2026-10-30).** Inter and Sora are served from the site itself, not from Google Fonts. Readers make no third-party requests (which avoids the GDPR issue with Google Fonts embeds), and there's no npm dependency (`@fontsource` was ruled out). Latin subset only; that covers Spanish accents.
- **Dark-mode surfaces (2026-10-30).** Vermilion text is 4.98:1 on Ink but only 4.30:1 on Graphite, which fails AA for 16 px text. So every dark surface that can carry primary-colored text (page, navbar, sidebar) is Ink, and Graphite is only used where no primary text sits on it. Other pairs checked: Crimson on white 6.22:1, Ink on Vermilion 4.98:1, Mist on Ink 7.02:1. Bone on Vermilion is 3.47:1, so never put light text on a Vermilion fill.
- **No generated-index pages (2026-10-30).** Docusaurus's `DocCard` puts an emoji before every card title on generated-index pages, which breaks the no-emoji rule. The user chose to link the categories to pages instead: Places and Routes open their overview, and Core and Getting started have no index page. Swizzling `DocCard` was rejected, because it would be a theme override to maintain. Don't use `DocCardList` in pages either, for the same reason.
- **`future.v4` as an object (2026-10-30).** In 3.10, `future.v4: true` turns on `fasterByDefault`, which requires the `@docusaurus/faster` package (a new dependency the plan rules out). The config lists all five v4 flags explicitly with only `fasterByDefault: false`, because a partial object defaults any missing flag to `false`.
- **Workflows committed last (2026-10-29).** `.github/workflows/*.yml` stay untracked until all docs work is done, so nothing can deploy or publish by accident, and so they land in one release commit together with the csproj metadata (NuGet icon, versions, etc.). Neither workflow would fire from the branch anyway (`docs.yml`: push to `main` touching `docs/**`; `publish.yml`: `v*.*.*` tags), so the real cost is that the two files exist only on disk (laptop, and the PC if still there) until then. They must be committed before or with the Step 10 merge, or the site never deploys.

- **Quick start and examples use ASP.NET Core minimal APIs (2026-10-30).** `WebApplication` plus an endpoint with a bound `CancellationToken` is the main consumer shape and shows `ct` flowing naturally. Each Getting started page also has a bare `ServiceCollection` version for console apps and tests. Keys are read from configuration (`GetSection("AarxMaps").Bind(o)`, user secrets in development), never hard-coded.
- **Snippets are compiled from the page text (2026-10-30).** A script in the scratchpad extracts every `csharp` block, hoists `using` lines, wraps fragments in methods and builds two scratch projects (console and Web SDK, each with its own implicit usings). Retyping snippets into a scratch app hides drift; extracting them doesn't.
- **Admonitions with titles use brackets (2026-10-30).** `:::warning[Title]`. See Lessons learned.
- **Feature-page snippets assume `places` and `ct` (2026-10-30).** The Places overview shows registration and a full class with an injected `IPlacesService`, then says the other pages assume an injected `places` and a `CancellationToken ct`. Feature snippets stay short (builder, call, a loop over the result) and still include their `using` lines. Routes pages should do the same with `routes`.
- **Template sections adapted per page (2026-10-30).** Options tables are three columns (`Method | Required | Rule`, defaults inside Rule), result tables are `Property | Type | Notes`. Long or secondary members (all `PlaceDetails` properties, nested types, `ReviewInfo`/`ReviewSummary`) go in `<details>`, as tables or short paragraphs. Photos and reviews share one page with H3 sub-sections instead of the flat template.
- **Known limitations counts as shared (2026-10-30).** A Places page may link to it, like a Core page; it is the only non-Core page outside the package that Places links to.
- **Billing statements only where the SDK documents them (2026-10-30).** Pages state what the XML docs state (Details and reviews billed as Enterprise + Atmosphere, one billed request per photo and per page, session tokens) and link to Google for everything else.
- **Core pages may name Places and Routes behavior (2026-10-30).** Isolation applies to the Places and Routes folders only. Core pages mention module behavior when it's a Core concern (what is never cached, streamed responses and the timeout).

## Lessons learned

- **The session scratchpad isn't durable (2026-10-30).** It was reset between Step 5 and Step 6, taking the compose script and scratch projects with it. Fixed the same day: the script now lives in the repo as `docs/scripts/compile-snippets.py` (see Verification). Keep reusable tooling in the repo, and only generated output in the scratchpad.
- **Docusaurus `<details>` ignore a scripted `open` (2026-10-30).** The theme wraps them in a React collapsible, so `details.open = true` changes nothing on screen, and anything inside stays hidden, so measurements of it read as "fits". Click the `summary` in Playwright and check `offsetHeight > 0` before measuring collapsed content.
- **Infima's `.alert` restyles its contents (2026-10-30).** It redefines `--ifm-link-color`, `--ifm-code-background` and the tabs colors for everything inside. Any component that reuses alert classes (like Docusaurus's `Details`) needs those reset too, not just the background and border.
- **CSS modules lose to Infima on `.container` (2026-10-30).** A single-class `max-width` on an element that also has `container` is overridden by Infima's breakpoint rules. Use two classes (`.section .narrow`) and measure the rendered width.
- **Don't `pkill -f` inside a compound command (2026-10-30).** `pkill -f "docusaurus serve ..."` also matched the shell running it and killed the whole command (exit 144), so later steps silently didn't run. Run `pkill` as its own call.
- **Synchronous fragments need a synchronous wrapper (2026-10-30).** An `async Task` wrapper around a block with no `await` fails with CS1998 under `-warnaserror`. Give such blocks a `void Run(...)` rule.
- **Check each Google claim against the reference, not the plan (2026-10-30).** The plan said past departure times "fail"; Google's `computeRoutes` reference only says they're valid for Transit. Grep the reference page (`curl -sL ... | sed 's/<[^>]*>//g'`) before stating Google-side behavior the XML docs don't state.
- **A wrapper must not add `using` lines (2026-10-30).** Adding `using X;` around a snippet that already has it fails with CS0105, and around one that lacks it hides the missing line. Put fully qualified types in the wrapper signature instead.

- **Admonition titles need brackets with the v4 flags (2026-10-30).** `future.v4.mdx1CompatDisabledByDefault` turns off the MDX1 admonition syntax, so `:::warning Some title` shows up as literal text, and the build doesn't complain. Use `:::warning[Some title]`. Only a rendered-page check (Playwright, or grepping `build/` for `theme-admonition`) catches it.
- **Code blocks fit about 80 characters at 1440 px (2026-10-30).** Longer lines scroll inside the block. Split builder chains, long lambdas and parameter lists; a short local (`var start = new LatLng(...)`) usually fixes it.
- **Read the internals behind every behavior you document (2026-10-30).** The README said the cache key includes "language". It doesn't: the configured language is a default header added after the key is built. Claims about keys, timeouts, error bodies and serialization were checked against `RequestFactory`, `RequestFingerprintCacheKeyStrategy`, `ExceptionHandler`, `MapsApiClient` and `MapsJsonOptions` before going into the pages.

- **Partial `future.v4` objects default to false (2026-10-30).** Leaving a flag out of the object turns it off; list all five.

- **The inherited docs describe a different API.** Signatures, DI names and namespaces changed. Treat old pages as a list of topics, never as content.
- **Snippets drift.** Compiling the README snippets in a scratch app caught issues a read-through missed. The same check applies to every docs snippet.
- **Untracked files don't travel.** `docs/`, `.github/` and `README.md` were never committed, and `BrandKit/` is gitignored. Check `git status` before switching machines.
- **Node isn't on the planning machine.** Docusaurus work needs Node 20+ installed first, or the only build check is CI.
- **Playwright: don't toggle themes by hand (2026-10-30).** Setting `data-theme` directly hides the navbar logo (the `ThemedImage` state doesn't follow), and the 3.10 color-mode button didn't switch the theme when clicked from a script. Use `browser_emulate_media` with `colorScheme: 'dark'` and reload; `respectPrefersColorScheme` picks it up. Check 375 px by resizing and navigating, not with `window.open` (pop-ups open at the default size).
- **Infima tables don't wrap code (2026-10-30).** Tables are `display: block` and grow to fit the inline code, so wide tables scroll inside themselves instead of wrapping. Forcing `display: table` plus `overflow-wrap: anywhere` fits them, but breaks identifiers mid-word. For Steps 5-7: keep tables to two or three short columns, drop the type prefix from member names, and move anything long to lists or prose. The page template's four-column Options table (`method | required | default | rule`) is at risk; keep its cells short.
- **Code in headings breaks the TOC (2026-10-30).** Backticked `###` headings wrap mid-word in the right-hand TOC column. Keep headings plain text.
- **The old packages beat the old pages (2026-10-30).** The old docs missed or misstated details (e.g. `RouteMatrixOrigin`'s public constructor, `TransitPreferences` taking a `List`). Reflecting over the cached 3.0.1 packages gave the exact surface.
- **Check the brand kit against its own file list (2026-10-29).** `brand-kit.md` lists SVG exports that weren't copied into `BrandKit/`. Diff what the kit claims against what's on disk before planning asset work.
