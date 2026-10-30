# AarxMaps - Release Plan

Last updated: 2026-10-31

Plan for taking AarxMaps from "docs done" to 1.0.0 on nuget.org. It replaces `DOC-PLAN.md` Work steps 10 and 11 (deploy the site, README trim, package checks) and adds the steps they were missing. Each step says who does it. The user owns git, GitHub and nuget.org. Changes to csproj and workflow files are code, so the user makes them (or asks Claude to), and Claude reviews them.

Read order in a new session: `CLAUDE.md` -> `PROGRESS.md` -> this file. `DOC-PLAN.md` holds the docs site history (Steps 0-9) and the README trim rules that R5 uses.

## Current status

- **Docs done.** `DOC-PLAN.md` Steps 0-9 are complete (2026-10-31): every page written and verified, and the user read the site with no findings.
- **Target changed (2026-10-31):** 1.0.0 targets `net10.0` only (see Critical decisions). The code, docs and `CLAUDE.md` still say `net8.0;net9.0` until R1 is done.
- **Next:** R1.
- **Blocked:** nothing.

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

- [ ] Drop the `Csproj metadata` stash. Its content is already in the working tree.
- [ ] Drop the `Dotnet 10 target` stash. It adds `net10.0` next to `net8.0;net9.0`, and its text changes (README, Installation, `CLAUDE.md`, `PROGRESS.md`) describe three targets, so it doesn't fit a `net10.0`-only release. Claude rewrites those texts below.
- [ ] Retarget the three packable projects and `AarxMaps.Tests` to `<TargetFramework>net10.0</TargetFramework>` (singular, replacing `TargetFrameworks`).
- [ ] Align the `Microsoft.Extensions.*` references in Core and Places on one patch, 10.0.14 (recheck the latest when you do it):

  | Package | Used by | Now | Target |
  | --- | --- | --- | --- |
  | `Microsoft.Extensions.Caching.Abstractions` | Core | 10.0.7 | 10.0.14 |
  | `Microsoft.Extensions.Caching.Memory` | Core | 10.0.7 | 10.0.14 |
  | `Microsoft.Extensions.DependencyInjection` | Core, Places | 10.0.3 | 10.0.14 |
  | `Microsoft.Extensions.Http` | Core | 10.0.3 | 10.0.14 |

- [ ] `dotnet build AarxMaps.sln` with 0 warnings, then `dotnet test AarxMaps.Tests`. `AnalysisLevel` is `latest`, so the .NET 10 SDK can bring analyzer rules the 8/9 builds didn't run. Fix them or suppress them with a reason, as `CLAUDE.md` says.

**Claude (docs, after the build is green):**

- [ ] Installation: requirements become ".NET 10 or later", and the "Supported .NET versions" table is replaced by one line. The Introduction drops ".NET 8 and later".
- [ ] Coming from DelgadoMaps: say that AarxMaps needs .NET 10, so apps on .NET 8 or 9 upgrade first.
- [ ] README: the two version lines.
- [ ] `docs/scripts/compile-snippets.py`: default `--framework` `net10.0`. Rerun it (every snippet must compile), then `npm run build`.
- [ ] `CLAUDE.md`: the TFM rules ("Builds with zero warnings on both `net8.0` and `net9.0`", the `-f net9.0` command, the C# 13 / lowest-TFM note) and the CI line. `PROGRESS.md`: the build state and the .NET 10 compatibility notes.
- [ ] Grep docs, README and tracking files for `net8`, `net9`, `.NET 8` and `.NET 9`, and expect hits only in history (`DOC-PLAN.md`, dated `PROGRESS.md` entries).

### R2. Package metadata (user edits, Claude reviews)

- [ ] **One place for shared metadata.** Move what the three csproj repeat into a root `Directory.Build.props`: `TargetFramework`, `Version`, authors, license, repository, icon and readme items, analyzer settings. One `Version` removes the "keep the three in sync before tagging" risk.
- [ ] **`PackageProjectUrl`** -> the docs site (`https://aarexus.github.io/AarxMaps/`). The repository stays in `RepositoryUrl`.
- [ ] **Symbols:** `IncludeSymbols` + `SymbolPackageFormat` `snupkg`, `EmbedUntrackedSources`, and `ContinuousIntegrationBuild` set when building on CI (`'$(GITHUB_ACTIONS)' == 'true'`). This gives debuggable packages with deterministic paths. No new dependency.
- [ ] **Release notes:** `PackageReleaseNotes`, a short line plus a link to the GitHub release.
- [ ] **Vulnerability check:** `dotnet list package --vulnerable --include-transitive` on the three packable projects, and expect no hits.

### R3. Workflows (user edits, Claude reviews)

- [ ] `publish.yml`:
  - `setup-dotnet` installs only `10.0.x`.
  - Fail when the tag (`v1.0.0`) doesn't match the package version.
  - Set `permissions: contents: read`.
  - Push the `.snupkg` files too. `dotnet nuget push *.nupkg` picks them up when they sit next to the packages.
- [ ] Optional: run the publish job in a GitHub environment (`nuget`) with the user as required reviewer, so a pushed tag waits for approval. Put the API key in that environment instead of a repo secret.
- [ ] `docs.yml` needs no changes.

### R4. Deploy the docs (user, then Claude)

Was Work step 10.

- [ ] **User:** commit `.github/workflows/`, `icon.png` and the csproj/props changes in one release commit, as `DOC-PLAN.md` Critical decisions ("Workflows committed last") says. Enable Pages (Settings -> Pages -> Source: GitHub Actions). Merge to `main` by PR (`Phase4: Docs`).
- [ ] **Claude:** check `https://aarexus.github.io/AarxMaps/`:
  - Every page loads under `/AarxMaps/`, and fonts, icon and favicon load (no 404s in the network log).
  - The social card URL resolves.
  - Both themes render, and the 404 page works.
  - Spot-check external links.

### R5. Trim the README (Claude, user reads)

Was the first half of Work step 11; follow `DOC-PLAN.md` "README trim". The README is packed into every package, so do it before R6. Use absolute links only, now that the site is live. Compile any snippet that stays.

### R6. Pack and inspect (Claude)

Was the second half of Work step 11.

- [ ] Run `dotnet pack -c Release`, then check each `.nupkg` and `.snupkg`:
  - `lib/net10.0` only, with the DLL and XML docs. README and icon are present.
  - License expression, project URL, repository commit and release notes are in the nuspec.
  - One dependency group (`net10.0`) with the 10.0.x versions from R1. The version is `1.0.0`.
- [ ] Check that the README renders the way nuget.org renders it: no relative links or images, no HTML that nuget.org strips.

### R7. Smoke test from the packages (Claude)

Every check so far used `ProjectReference`. Here, put the R6 packages in a local folder feed and create a fresh console app and a fresh ASP.NET Core app on `net10.0` that reference `AarxMaps.*` 1.0.0 from that feed. Compile the Quick start snippets in them with `-warnaserror`. This catches packaging mistakes (missing dependency, wrong TFM) that the solution build can't see. No live Google calls.

Also try to install the packages into a `net8.0` app. The restore should fail with NU1202 (package not compatible). Quote the message in Coming from DelgadoMaps if it helps readers recognize it.

### R8. nuget.org setup (user)

- [ ] Recheck that the IDs are free.
- [ ] Optional: request ID prefix reservation for `AarxMaps.*`. Packages then show a verified mark and nobody else can publish under the prefix.
- [ ] Create an API key: push-only, glob `AarxMaps.*`, with an expiry. Store it as `NUGET_API_KEY` (repo secret, or the R3 environment).
- [ ] Recheck `PROGRESS.md` Known flaws against the Known limitations page.

### R9. Release (user)

- [ ] On `main`, tag `v1.0.0` and push the tag. `publish.yml` builds, tests, packs and pushes.
- [ ] Create a GitHub Release for the tag with short notes: what AarxMaps is, that it needs .NET 10, and links to the docs and to "Coming from DelgadoMaps".

### R10. After release (user, then Claude)

- [ ] **Claude:** check the three nuget.org pages (README, icon, dependencies, symbols, license, `net10.0` shown as the target). Install 1.0.0 from nuget.org in a fresh app and build the Quick start.
- [ ] **User:** deprecate the `D3lg4doMaps.*` packages on nuget.org (reason "Other", alternate package `AarxMaps.*`). Users of the old IDs then see where to go.
- [ ] **Claude:** close out the docs:
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
- **Smoke test from packages, not projects (2026-10-31).** Every earlier check used `ProjectReference`. R7 installs the packed `.nupkg` files from a local folder into fresh apps, because only that catches packaging mistakes.

## Lessons learned

- **Check a stash before treating it as pending work (2026-10-31).** `Csproj metadata` looked like unapplied changes, but it was identical to the working tree. Compare it with `git diff stash@{0}` and `git show stash@{0}:<path> | cmp - <path>`.
- **Check support dates before picking target frameworks (2026-10-31).** The plan nearly fixed per-TFM dependency versions for two runtimes that were ten days from end of support. `releases-index.json` (dotnetcli blob storage) gives every channel's `eol-date` in one request.
