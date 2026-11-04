# AarxMaps - Release Plan

Last updated: 2026-11-04

**Complete (2026-11-04): AarxMaps 1.0.0 is published.** This file is now the step-by-step record of the release; its decisions and lessons are merged into `PROGRESS.md`, and the process for future releases is in `CLAUDE.md` (CI / release).

Plan for taking AarxMaps from "docs done" to 1.0.0 on nuget.org. It replaces `DOC-PLAN.md` Work steps 10 and 11 (deploy the site, README trim, package checks) and adds the steps they were missing. Each step says who does it. The user owns git, GitHub and nuget.org. Changes to csproj and workflow files are code, so the user makes them (or asks Claude to), and Claude reviews them.

Read order in a new session: `CLAUDE.md` -> `PROGRESS.md` -> this file. `DOC-PLAN.md` holds the docs site history (Steps 0-9) and the README trim rules that R5 uses.

## Current status

- **Docs done.** `DOC-PLAN.md` Steps 0-9 are complete (2026-10-31): every page written and verified, and the user read the site with no findings.
- **Target changed (2026-10-31):** 1.0.0 targets `net10.0` only (see Critical decisions).
- **R1 done (2026-10-31):** code, docs and tracking files are on `net10.0`. The R2 vulnerability check was run at the same time.
- **R2 done (2026-10-31):** Claude made the edits at the user's request; the user reviewed them.
- **R3 done (2026-10-31):** Claude made the edits at the user's request; the user reviewed them.
- **R4 done (2026-11-01):** `Release-steps` (docs + R1-R3) merged to `main`, Pages enabled, site live and checked. One fix (`trailingSlash: true`) is on the working tree and goes to `main` with R5.
- **R5 done (2026-11-01):** README trimmed from 682 to 107 lines; the user read it and approved it.
- **R6 and R7 done (2026-11-01):** packages inspected and installed from a local feed into fresh apps; all checks pass. Both R6 findings fixed at the user's request, and R7 rerun on the new packages.
- **Release order agreed (2026-11-01):** R6-R8 don't change the repo, so they run on the working tree first. Then one final commit and PR to `main` with everything (README, `trailingSlash`, tracking, any fixes), then R9 tags that commit. R10's tracking updates are a separate commit after the release.
- **R8 done (2026-11-03):** trusted publishing policy and `nuget` environment set up and checked, prefix `AarxMaps` reserved for `Aarexus` (granted 2026-11-03).
- **Blocked:** nothing.
- **R9 and R10 done (2026-11-04):** released and checked. Nothing left.

## Steps

### Facts checked

Checked 2026-10-31:

- **.NET support:** Microsoft's release index (`releases-index.json`) gives .NET 8 (LTS) and .NET 9 (STS) the same end of support, 2026-11-10. .NET 10 (LTS) is supported until 2028-11-14.
- **Stash:** the `Csproj metadata` stash is identical to the working tree: `.github/workflows/*.yml`, `icon.png` and the three csproj icon lines, compared file by file. It's a duplicate.
- **Pack output (before R1):** `dotnet pack AarxMaps.sln -c Release` produces exactly three packages (the test project isn't packed). Each holds `lib/net8.0` and `lib/net9.0` with the DLL and XML docs, `README.md` and `icon.png`. The nuspec has the repository URL plus commit, because SourceLink is built into the .NET 8+ SDK. There's no symbols package (`.snupkg`).
- **Package IDs:** `AarxMaps.Core`, `.Places` and `.Routes` don't exist on nuget.org yet (flat container returns 404). The three `D3lg4doMaps.*` packages do exist.
- **Dependencies:** every package depends on `Microsoft.Extensions.*` 10.0.x, mixed 10.0.3 and 10.0.7. The latest stable patch of all four is 10.0.14, and nuget.org's vulnerability data flags no 10.0.x version of any of them.
- **Tests:** `publish.yml` runs `dotnet test` for every TFM, not only `net9.0` as `CLAUDE.md` says. After R1 there is only one TFM.

### R1. Housekeeping and retarget to .NET 10

**User (code and git):**

- [x] Drop the `Csproj metadata` stash. Its content is already in the working tree.
- [x] Drop the `Dotnet 10 target` stash. It adds `net10.0` next to `net8.0;net9.0`, and its text changes (README, Installation, `CLAUDE.md`, `PROGRESS.md`) describe three targets, so it doesn't fit a `net10.0`-only release. Claude rewrites those texts below.
- [x] Retarget the three packable projects and `AarxMaps.Tests` to `<TargetFramework>net10.0</TargetFramework>` (singular, replacing `TargetFrameworks`).
- [x] Align the `Microsoft.Extensions.*` references in Core and Places on one patch, 10.0.14 (recheck the latest when you do it):

  | Package | Used by | Now | Target |
  | --- | --- | --- | --- |
  | `Microsoft.Extensions.Caching.Abstractions` | Core | 10.0.7 | 10.0.14 |
  | `Microsoft.Extensions.Caching.Memory` | Core | 10.0.7 | 10.0.14 |
  | `Microsoft.Extensions.DependencyInjection` | Core, Places | 10.0.3 | 10.0.14 |
  | `Microsoft.Extensions.Http` | Core | 10.0.3 | 10.0.14 |

- [x] `dotnet build AarxMaps.sln` with 0 warnings, then `dotnet test AarxMaps.Tests`. `AnalysisLevel` is `latest`, so the .NET 10 SDK can bring analyzer rules the 8/9 builds didn't run. Fix them or suppress them with a reason, as `CLAUDE.md` says. Result (2026-10-31): clean Debug and Release builds with 0 warnings, 1,284 tests pass; no new analyzer rules fired.

**Claude (docs, after the build is green):**

- [x] Installation: requirements become ".NET 10 or later", and the "Supported .NET versions" table is replaced by one line. The Introduction drops ".NET 8 and later".
- [x] Coming from DelgadoMaps: say that AarxMaps needs .NET 10, so apps on .NET 8 or 9 upgrade first.
- [x] README: the two version lines.
- [x] `docs/scripts/compile-snippets.py`: default `--framework` `net10.0`. Rerun it (every snippet must compile), then `npm run build`.
- [x] `CLAUDE.md`: the TFM rules ("Builds with zero warnings on both `net8.0` and `net9.0`", the `-f net9.0` command, the C# 13 / lowest-TFM note) and the CI line. `PROGRESS.md`: the build state and the .NET 10 compatibility notes.
- [x] Grep docs, README and tracking files for `net8`, `net9`, `.NET 8` and `.NET 9`, and expect hits only in history (`DOC-PLAN.md`, dated `PROGRESS.md` entries).

### R2. Package metadata (Claude edited at the user's request, user reviews)

- [x] **One place for shared metadata.** Move what the three csproj repeat into a root `Directory.Build.props`: `TargetFramework`, `Version`, authors, license, repository, icon and readme items, analyzer settings. One `Version` removes the "keep the three in sync before tagging" risk.
- [x] **`PackageProjectUrl`** -> the docs site (`https://aarexus.github.io/AarxMaps/`). The repository stays in `RepositoryUrl`.
- [x] **Symbols:** `IncludeSymbols` + `SymbolPackageFormat` `snupkg`, `EmbedUntrackedSources`, and `ContinuousIntegrationBuild` set when building on CI (`'$(GITHUB_ACTIONS)' == 'true'`). This gives debuggable packages with deterministic paths. No new dependency.
- [x] **Release notes:** `PackageReleaseNotes`, a short line plus a link to the GitHub release.
- **Result (2026-10-31):**
  - Clean Debug and Release builds with 0 warnings; 1,284 tests pass. Evaluated properties match the old csproj values for all four projects.
  - Each package now also produces a `.snupkg` (PDB only).
  - The nuspec differs from the R1 output only in `projectUrl` (docs site), the new `releaseNotes` and the commit hash.
  - A simulated CI build (`GITHUB_ACTIONS=true`) gives a PDB with no local paths, SourceLink mapping `/_/*` to `raw.githubusercontent.com/Aarexus/AarxMaps/<commit>`, and the generated sources embedded.
  - No package was added, so the vulnerability check stands.
  - `EnableNETAnalyzers` was dropped: it is `true` by default on .NET 5+ (verified with `-getProperty`).
- [x] **Vulnerability check:** `dotnet list package --vulnerable --include-transitive` on the three packable projects, and expect no hits. Done 2026-10-31 after the 10.0.14 bump: no hits in any project, Tests included. Rerun if R2 adds a package.

### R3. Workflows (Claude edited at the user's request, user reviews)

- [x] `publish.yml`:
  - `setup-dotnet` installs only `10.0.x`.
  - Fail when the tag (`v1.0.0`) doesn't match the package version.
  - Set `permissions: contents: read`.
  - Push the `.snupkg` files too. `dotnet nuget push *.nupkg` picks them up when they sit next to the packages.
- [x] Optional: run the publish job in a GitHub environment (`nuget`) with the user as required reviewer, so a pushed tag waits for approval. Put the API key in that environment instead of a repo secret. The workflow side is done (`environment: nuget`); the reviewer and the secret are set up in R8.
- [x] ~~`docs.yml` needs no changes.~~ Wrong: it pinned Node 20 (end of life 2026-04-30) and actions three majors old. Bumped like `publish.yml`, and Node 24 (LTS until 2028-04-30).
- **Result (2026-10-31):**
  - Both workflows use the latest action majors: `checkout@v7`, `setup-dotnet@v6`, `setup-node@v7`, `configure-pages@v6`, `upload-pages-artifact@v5`, `deploy-pages@v5`. Their major releases were checked for breaking changes; none affect these workflows (mostly the Node 24 action runtime). `upload-pages-artifact` v4+ drops dotfiles, so `.nojekyll` is left out; Pages deployed through Actions doesn't run Jekyll, so it isn't needed.
  - `publish.yml` also passes the API key through an `env` variable instead of inlining `${{ secrets.* }}` in the command.
  - Verified locally: the tag check passes for `v1.0.0` and fails for `v1.0.1`; restore, build, test and pack with the workflow's flags give 0 warnings, 1,284 passing tests, and three `.nupkg` + three `.snupkg` in the output folder. Both files parse as YAML.
  - Not verifiable locally: the docs build on Node 24 (local machine has Node 20). The first `docs.yml` run in R4 checks it.

### R4. Deploy the docs (user, then Claude)

Was Work step 10.

- [x] **User:** commit `.github/workflows/`, `icon.png` and the csproj/props changes in one release commit, as `DOC-PLAN.md` Critical decisions ("Workflows committed last") says. Enable Pages (Settings -> Pages -> Source: GitHub Actions). Merge to `main` by PR (`Phase4: Docs`).
- [x] **Claude:** check `https://aarexus.github.io/AarxMaps/`:
  - Every page loads under `/AarxMaps/`, and fonts, icon and favicon load (no 404s in the network log).
  - The social card URL resolves.
  - Both themes render, and the 404 page works.
  - Spot-check external links.
- **Result (2026-11-01):**
  - All 25 sitemap pages load (after a redirect, see below). Every asset referenced by the pages and the CSS returns 200: both fonts (Inter, Sora), icon, favicon, social card, JS and CSS bundles.
  - In a browser (Playwright): home page in light and directions page in dark render correctly, fonts loaded, no console errors, no failed requests.
  - Unknown URLs return 404 with the site's "Page Not Found" page.
  - All 43 external links checked: 42 return 200; LinkedIn returns 999, its standard answer to automated requests (not broken).
  - The Node 24 docs build passed on CI (first `docs.yml` run).
  - **Finding, fixed:** every docs URL answered 301. Docusaurus emits `<path>/index.html`, GitHub Pages redirects `/<path>` to `/<path>/`, and with `trailingSlash` unset the sitemap, canonical and `og:url` all used the no-slash form. `docs/docusaurus.config.ts` now sets `trailingSlash: true`; the rebuilt site uses `/` everywhere and the output paths are unchanged. Deploys with the R5 PR. **Live 2026-11-04:** pages answer 200 directly, canonical and all sitemap URLs end in `/`.

### R5. Trim the README (Claude, user reads)

Was the first half of Work step 11; follow `DOC-PLAN.md` "README trim". Links to the site use the trailing-slash form (`.../docs/intro/`), matching `trailingSlash: true`. The README is packed into every package, so do it before R6. Use absolute links only, now that the site is live. Compile any snippet that stays.

- [x] **Result (2026-11-01):**
  - Kept: intro paragraph, packages table, .NET line, requirements, install, quick start, license and the Google disclaimer. Added a docs-site link under the intro and a "Documentation" list with every site page, grouped by sidebar section.
  - Dropped (all on the site): contents, configuration, registration rules, caching, every Places and Routes section, errors, cancellation, custom endpoints, usage notes, known limitations, namespace reference, and the console example. The misleading "services are safe to inject into singletons" line (`DOC-PLAN.md` finding) went with usage notes.
  - The quick start keeps its combined shape (Places and Routes in one ASP.NET Core `Program.cs`) but now matches the site's Quick start: key from the `AarxMaps` configuration section via user secrets (was `GoogleMaps:ApiKey`), and the route picked by `RouteLabel.DefaultRoute` (was the first route).
  - Checks: all 27 links return 200 directly (no redirects), no relative links, no HTML. The quick start compiles in a fresh `net10.0` web app with `-warnaserror`.
- [x] Read and approved by the user (2026-11-01).
- [ ] **User:** open the PR to `main` with the README and the `trailingSlash` fix. Title suggestion: `Phase4: README trim`.

### R6. Pack and inspect (Claude)

Was the second half of Work step 11.

- [x] Run `dotnet pack -c Release`, then check each `.nupkg` and `.snupkg`:
  - `lib/net10.0` only, with the DLL and XML docs. README and icon are present.
  - License expression, project URL, repository commit and release notes are in the nuspec.
  - One dependency group (`net10.0`) with the 10.0.x versions from R1. The version is `1.0.0`.
- [x] Check that the README renders the way nuget.org renders it: no relative links or images, no HTML that nuget.org strips.
- **Result (2026-11-01):** packed the CI way (clean `dotnet build -c Release`, then `dotnet pack --no-build`), 0 warnings.
  - Each `.nupkg`: nuspec, `README.md` (identical to the repo file), `icon.png` (identical), `lib/net10.0/<id>.dll` and `.xml`. Each `.snupkg`: the PDB only.
  - Nuspec: version `1.0.0`, MIT expression, docs-site project URL, release notes, repository URL + commit. One `net10.0` dependency group: Core -> four `Microsoft.Extensions.*` 10.0.14; Places -> `AarxMaps.Core` 1.0.0 + `Microsoft.Extensions.DependencyInjection` 10.0.14; Routes -> `AarxMaps.Core` 1.0.0.
  - DLLs: `AssemblyVersion` 1.0.0.0, informational version `1.0.0+<commit>`.
  - README: no relative links, no images, no HTML (checked in R5).
- **Findings (both fixed 2026-11-01 at the user's request):**
  1. **Minor:** a standalone `dotnet pack` on a clean tree fails with NU5026 (DLL not found). With `GeneratePackageOnBuild=true`, NuGet's `Pack` target no longer depends on `Build` (`NuGet.Build.Tasks.Pack.targets`), because the build is expected to pack. CI is unaffected (build, then `pack --no-build`). Fix: drop `GeneratePackageOnBuild` from `Directory.Build.props`. Builds then stop writing packages, `dotnet pack` works on its own, and CI still works unchanged. **Fixed:** removed. Verified: builds write no packages, a standalone `dotnet pack -c Release` on a clean tree produces all six files, and the CI sequence (restore, build, test, `pack --no-build`) still gives 0 warnings, 1,284 tests and six files.
  2. **Nit:** Places declares `Microsoft.Extensions.DependencyInjection` directly and Routes doesn't; both get it through Core. Drop the Places reference for consistency, or leave it. **Fixed:** removed from `AarxMaps.Places.csproj`. Places builds with 0 warnings (DI resolves transitively at 10.0.14), and its nuspec now depends only on `AarxMaps.Core` 1.0.0, like Routes.
  - **R7 rerun on the fixed packages (2026-11-01):** same results: web and console apps compile with `-warnaserror`, the runtime console exits 0 with all four checks, `net8.0` still fails with NU1202, nothing in the global cache.

### R7. Smoke test from the packages (Claude)

Every check so far used `ProjectReference`. Here, put the R6 packages in a local folder feed and create a fresh console app and a fresh ASP.NET Core app on `net10.0` that reference `AarxMaps.*` 1.0.0 from that feed. Compile the Quick start snippets in them with `-warnaserror`. This catches packaging mistakes (missing dependency, wrong TFM) that the solution build can't see. No live Google calls.

Also try to install the packages into a `net8.0` app. The restore should fail with NU1202 (package not compatible). Quote the message in Coming from DelgadoMaps if it helps readers recognize it.

- [x] **Result (2026-11-01):** local folder feed with package source mapping (`AarxMaps.*` -> local, everything else -> nuget.org) and an isolated `NUGET_PACKAGES` cache, so the local 1.0.0 never reaches the global cache (checked: it didn't).
  - `dotnet add package AarxMaps.Places/Routes --version 1.0.0` works; `AarxMaps.Core` comes in transitively.
  - Fresh `net10.0` web app with the README quick start: builds with `-warnaserror`, 0 warnings.
  - Fresh `net10.0` console app with the Introduction's combined example (Places -> place ID -> Routes): builds with `-warnaserror`, 0 warnings.
  - Runtime console (no network): resolves `IPlacesService` and `IRoutesService`, builds a text search and a route request, rejects `LatLng(91, 0)` with `MapsInvalidRequestException`, and rejects a missing API key with `OptionsValidationException` ("ApiKey must be provided."). Exit code 0.
  - `net8.0` app: `error: NU1202: Package AarxMaps.Places 1.0.0 is not compatible with net8.0 (.NETCoreApp,Version=v8.0). Package AarxMaps.Places 1.0.0 supports: net10.0 (.NETCoreApp,Version=v10.0)`. Coming from DelgadoMaps already names NU1202, so no docs change.

### R8. nuget.org setup (user)

- [x] Recheck that the IDs are free. Checked 2026-11-02 (Claude): `AarxMaps.Core`, `.Places`, `.Routes` return 404 on the flat container and 0 search hits. `D3lg4doMaps.*` is owned by the `JSebas-11` account. A profile named `Aarexus` exists on nuget.org with no packages; the user confirms whether it's theirs before choosing the package owner.
- **Package owner: the `Aarexus` nuget.org organization (user, 2026-11-02).** The user owns it and is a member through the `JSebas-11` account.
- [x] Request ID prefix reservation for `AarxMaps.*` (user wants it). Email sent by the user 2026-11-02. **Granted 2026-11-03:** nuget.org reserved the prefix `AarxMaps` (no dot, so it covers every ID starting with `AarxMaps`) for the `Aarexus` account. The verified mark shows once the packages are published; check it in R10. Email `account@nuget.org` from the owner side with the owner display name (`Aarexus`) and the prefix. Packages then show a verified mark and nobody else can publish under the prefix; existing packages owned by `Aarexus` get the mark too, so it doesn't block the release.
- [x] **Trusted publishing policy instead of an API key** (nuget.org recommends it; no long-lived secret). Logged in as `JSebas-11`: username menu -> Trusted Publishing -> add policy:
  - Package owner `Aarexus`, repository owner `Aarexus`, repository `AarxMaps`, workflow file `publish.yml` (file name only), environment `nuget`.
  - Scope: push new packages and new versions, glob `AarxMaps.*`, if the form offers scopes.
  - If the policy shows as temporarily active (7 days), publish within that window or restart it; the first successful publish makes it permanent.
- [x] `publish.yml` switched to trusted publishing (Claude, 2026-11-02): `id-token: write` next to `contents: read`, a `NuGet/login@v1` step (`user: JSebas-11`, the account that creates the policy) right before the push, and the push uses its `NUGET_API_KEY` output (valid 1 hour). No `NUGET_API_KEY` secret anymore. Parses as YAML; the OIDC exchange can only be tested by the release run. Known issue NuGet/login#6 (401 with reusable workflows) doesn't apply: `publish.yml` isn't reusable.
- [x] GitHub environment `nuget` (Settings -> Environments): required reviewer = the user, "Prevent self-review" off, deployment tag rule `v*.*.*`. No secrets needed. Delete any leftover repo-level `NUGET_API_KEY` secret from the D3lg4doMaps setup.
  - Set up by the user 2026-11-02 (policy too). Checked by Claude through the public API: reviewer `JSebas-11`, self-review allowed: OK. **Tag rule is `v*.*.*.` (trailing dot), which `v1.0.0` doesn't match, so the release would be blocked: fix to `v*.*.*`.** There's also a `main` branch rule; not needed (the workflow only runs on tags) and it opens the environment to any workflow on `main`: remove it.
  - Recheck 2026-11-02: `main` rule removed. The tag rule is still stored as `v*.*.*.` (raw API value, 7 characters); the trailing dot is easy to miss in the UI. User to delete it and retype `v*.*.*`.
  - Fixed 2026-11-03: the only rule is the tag rule `v*.*.*` (raw API value), which matches `v1.0.0`. Reviewer `JSebas-11`, self-review allowed. Environment ready.
- [x] Recheck `PROGRESS.md` Known flaws against the Known limitations page. Checked 2026-11-02 (Claude): all four flaws are on the page with matching scope and workarounds (language/region, internal setters, Routes `null` handling, sliding expiration). The page also lists items missing from `PROGRESS.md` (retries, timeout exception, cache key without language, request options); they come from `DOC-PLAN.md` and get merged in R10. No doc change.

### R9. Release (user)

- [x] On `main`, tag `v1.0.0` and push the tag. `publish.yml` builds, tests, packs and pushes.
- [x] Create a GitHub Release for the tag with short notes: what AarxMaps is, that it needs .NET 10, and links to the docs and to "Coming from DelgadoMaps".
- **Result (2026-11-04):** GitHub Release `AarxMaps 1.0.0` created with a new tag `v1.0.0` on `807f454` (`main`, PR #5); release label None, so it's Latest. Run 41864727655 queued for a while (GitHub Actions incident, "degraded availability"), then waited for review; the user approved the `nuget` environment. All ten steps green, including the tag check and the trusted publishing login. Packages were downloadable within minutes; search and registration indexing took a few hours.

### R10. After release (user, then Claude)

- [x] **Claude:** check the three nuget.org pages (README, icon, dependencies, symbols, license, `net10.0` shown as the target). Install 1.0.0 from nuget.org in a fresh app and build the Quick start.
  - **Result (2026-11-04):** all three listed at 1.0.0, owner `Aarexus`, **verified** (reserved prefix), author Aarexus Software, MIT, project URL = docs site, one `net10.0` dependency group (Core -> four `Microsoft.Extensions.*` >= 10.0.14; Places and Routes -> `AarxMaps.Core` >= 1.0.0).
  - Icons are the repo's 128x128 `icon.png`, byte for byte. The README renders on nuget.org with all six sections, the packages table, three code blocks, 25 docs-site links and no relative links.
  - Symbols: each `.snupkg` downloads from nuget.org; its PDB has no local paths and SourceLink maps `/_/*` to `raw.githubusercontent.com/Aarexus/AarxMaps/807f454.../*`, the release commit.
  - Fresh apps with nuget.org as the only source and an isolated cache: `dotnet add package` without a version resolves 1.0.0; the README quick start (web) and the Introduction example (console) build with `-warnaserror`, 0 warnings; the runtime console exits 0 (services resolve, requests build, invalid latitude and missing API key rejected). Nothing reached the global cache.
- [x] **User:** deprecate the `D3lg4doMaps.*` packages on nuget.org, **after** AarxMaps is live: the alternate package must already exist on nuget.org. Logged in as `JSebas-11` (their owner), Manage package -> Deprecation, all versions, reason "Legacy", alternate `D3lg4doMaps.Core` -> `AarxMaps.Core`, `.Places` -> `AarxMaps.Places`, `.Routes` -> `AarxMaps.Routes` (no specific version), custom message pointing to the Coming from DelgadoMaps page and the .NET 10 requirement. Users of the old IDs then get a warning with the replacement in Visual Studio, `dotnet list package --deprecated` and nuget.org.
  - **Done 2026-11-04, checked by Claude in the registration data:** all 14 versions (Core 5, Places 5, Routes 4) deprecated as Legacy, with the matching AarxMaps alternate and the custom message.
  - **Deprecate, don't unlist (2026-11-03).** Unlisting only hides a package from search: existing projects still restore it, nobody gets a pointer to AarxMaps, and it would hide the deprecation notice from searchers. Unlist later only if wanted, and only after deprecating.
- [x] **Claude:** close out the docs (2026-11-04):
  - Merge the decisions and lessons of `DOC-PLAN.md` (as its header says) and of this file into `PROGRESS.md`.
  - Mark Phase 4 done.
  - Update `CLAUDE.md`'s status snapshot: 1.0.0 is published, so public API changes now need a planned breaking release and migration notes.

## Critical decisions

- **Separate release plan (2026-10-31).** The release steps live in this file, not in `DOC-PLAN.md`, at the user's request. `DOC-PLAN.md` keeps the docs site history only.
- **Release replaces Work steps 10 and 11 (2026-10-31).** Deploy the site (R4), README trim (R5) and package checks (R6) moved here. R1-R3, R7, R8 and the deprecation in R10 are new.
- **Target `net10.0` only (user, 2026-10-31).** .NET 8 and 9 both reach end of support on 2026-11-10, ten days after this decision, and .NET 10 is the current LTS (until 2028-11-14). Shipping 1.0.0 on two runtimes that are about to go out of support makes no sense.
  - **Cost:** apps on .NET 8 or 9 can't install AarxMaps (NU1202) until they upgrade, including existing DelgadoMaps users.
  - **Gain:** one TFM, one dependency line (`Microsoft.Extensions.*` 10.0.x), one test run, and no lowest-TFM language limits.
  - **Supersedes** the same-day decision to use per-TFM `Microsoft.Extensions` versions (8.0.x for `net8.0`, 9.0.x for `net9.0`), and the `Dotnet 10 target` stash, which added `net10.0` next to the old targets.
- **`Directory.Build.props` scope (2026-10-31).** Shared settings (TFM, nullable, implicit usings) apply to every project. Packaging, XML docs and analyzer settings are conditioned on `'$(MSBuildProjectName)' != 'AarxMaps.Tests'`. `IsPackable` can't be the condition, because the props file is evaluated before the csproj sets it.
- **Release notes and version are hardcoded per release (2026-10-31).** `PackageReleaseNotes` names v1.0.0 explicitly. Each release updates `Version` and `PackageReleaseNotes` together in `Directory.Build.props`.
- **Publish job runs in a `nuget` environment (2026-10-31).** Lets the user add a required reviewer; the trusted publishing policy is also bound to it. Harmless if left unconfigured.
- **Trusted publishing, no API key (user, 2026-11-02).** nuget.org exchanges the workflow's GitHub OIDC token for a one-hour key, bound to the `Aarexus/AarxMaps` repo, `publish.yml` and the `nuget` environment. Nothing to store, rotate or leak. Supersedes the R8 API key plan.
- **Packages owned by the `Aarexus` organization (user, 2026-11-02),** not the personal `JSebas-11` account that owns `D3lg4doMaps.*`. Matches the package author.
- **`trailingSlash: true` on the docs site (2026-11-01).** Removes the GitHub Pages 301 on every page. `true` instead of `false` because it keeps the `index.html` layout, so both `/path` (redirect) and `/path/` work; with `false`, a link with a trailing slash would 404 on Pages.
- **No `GeneratePackageOnBuild` (user, 2026-11-01).** Packing is `dotnet pack`'s job; builds stay fast and don't write packages.
- **Modules reference only Core (user, 2026-11-01).** Places and Routes get `Microsoft.Extensions.*` through Core, so the dependency versions are set in one csproj.
- **Smoke test from packages, not projects (2026-10-31).** Every earlier check used `ProjectReference`. R7 installs the packed `.nupkg` files from a local folder into fresh apps, because only that catches packaging mistakes.

## Lessons learned

- **Check environment rules through the API, not by eye (2026-11-02).** `GET /repos/<owner>/<repo>/environments/<name>/deployment-branch-policies` is public for public repos and showed a tag rule with a stray trailing dot that would have blocked the release.

- **`GeneratePackageOnBuild` changes what `dotnet pack` does (2026-11-01).** With it on, `Pack` skips `Build`, so `dotnet pack` on a clean tree fails with NU5026. Earlier checks passed only because `bin/` already held a build.
- **Isolate the NuGet cache when smoke testing a local feed (2026-11-01).** A locally packed 1.0.0 in `~/.nuget/packages` would be reused later instead of the nuget.org 1.0.0 (same ID and version), hiding problems in the published package. Set `NUGET_PACKAGES` to a scratch folder.

- **Set `trailingSlash` explicitly for Docusaurus on GitHub Pages (2026-11-01).** Left unset, the site builds and works, but every canonical/sitemap URL costs a 301. `npm run build` and `npm run serve` don't show it; only requests against the live host do.

- **Check action versions and runtime support when touching workflows (2026-10-31).** The plan said `docs.yml` needed no changes, but it ran on Node 20 (end of life) with actions three majors behind. The GitHub releases API (`/repos/<owner>/<action>/releases/latest`) and Node's `schedule.json` answer both in seconds.

- **An empty `Directory.Build.props` breaks every build (2026-10-31).** MSBuild imports it automatically and fails with MSB4024 "Root element is missing". Even a placeholder needs `<Project></Project>`.

- **Verify a retarget with a clean build (2026-10-31).** After the TFM change, `bin/` still held the previous day's `net8.0`/`net9.0` outputs, which could pass for a new build. Delete `bin/`/`obj/`, rebuild, then check timestamps and package contents.

- **Check a stash before treating it as pending work (2026-10-31).** `Csproj metadata` looked like unapplied changes, but it was identical to the working tree. Compare it with `git diff stash@{0}` and `git show stash@{0}:<path> | cmp - <path>`.
- **Check support dates before picking target frameworks (2026-10-31).** The plan nearly fixed per-TFM dependency versions for two runtimes that were ten days from end of support. `releases-index.json` (dotnetcli blob storage) gives every channel's `eol-date` in one request.
