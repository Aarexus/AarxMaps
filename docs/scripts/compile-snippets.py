#!/usr/bin/env python3
"""Compile every C# snippet on the docs site against the AarxMaps projects.

Usage, from the repo root:
    python3 -I docs/scripts/compile-snippets.py [--out DIR] [--framework TFM]

Creates two scratch projects in DIR (outside the repo by default): a console app and an
ASP.NET Core app, each with its SDK's implicit usings, warnings as errors and a
ProjectReference to Core, Places and Routes. Every ```csharp block under docs/docs is
written into one of them, then both are built. Exits with 1 if a build fails.

Blocks that declare public types are copied verbatim. Every other block is a fragment:
its `using` lines are hoisted and its body is wrapped in a static method whose parameters
are the variables the page assumes (`places`, `routes`, `ct`, ...). Wrapper signatures use
fully qualified types, so the wrapper never adds a `using` that could hide a missing one.

coming-from-delgadomaps.md is skipped: its "before" block targets the old 3.0.1 packages.
"""

import argparse
import re
import subprocess
import sys
import tempfile
from dataclasses import dataclass
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
DOCS_DIR = REPO_ROOT / "docs" / "docs"
SKIPPED_PAGES = {"coming-from-delgadomaps.md"}
REDIS_PACKAGE_VERSION = "10.0.7"

CONSOLE = "Console"
WEB = "Web"


@dataclass(frozen=True)
class Wrapper:
    """How a fragment is wrapped: target project and method signature (None = top-level Program.cs)."""
    project: str
    signature: str | None


PROGRAM = Wrapper(CONSOLE, None)
WEB_ARGS = Wrapper(WEB, "public static void Run(string[] args)")
WEB_BUILDER = Wrapper(WEB, "public static void Run(WebApplicationBuilder builder)")
PLACES_FRAGMENT = Wrapper(
    CONSOLE,
    "public static async Task RunAsync(AarxMaps.Places.Services.IPlacesService places, "
    "string placeId, CancellationToken ct)",
)
ROUTES_FRAGMENT = Wrapper(
    CONSOLE,
    "public static async Task RunAsync(AarxMaps.Routes.Services.IRoutesService routes, "
    "CancellationToken ct)",
)
ROUTES_WAYPOINTS = Wrapper(
    CONSOLE,
    "public static void Run(AarxMaps.Routes.Common.Models.Waypoint origin, "
    "AarxMaps.Routes.Common.Models.Waypoint destination)",
)
ROUTES_REQUEST = Wrapper(
    CONSOLE,
    "public static async Task RunAsync(AarxMaps.Routes.Services.IRoutesService routes, "
    "AarxMaps.Routes.Services.Directions.RouteRequest request, CancellationToken ct)",
)

# Explicit rules per page and block index. Fragments without a rule fall back to the
# folder default below; anything else must be listed here or the script stops.
PAGE_RULES: dict[str, dict[int, Wrapper]] = {
    "intro.md": {0: PROGRAM},
    "getting-started/quick-start.md": {
        0: WEB_ARGS,
        1: WEB_ARGS,
        2: Wrapper(CONSOLE, "public static async Task RunAsync()"),
    },
    "core/configuration.md": {
        0: WEB_ARGS,
        1: WEB_BUILDER,
        2: Wrapper(WEB, "public static void Run(IServiceCollection services, string apiKey)"),
    },
    "core/caching.md": {0: WEB_BUILDER, 1: WEB_BUILDER},
    "core/cancellation.md": {
        1: Wrapper(
            CONSOLE,
            "public static async Task RunAsync(AarxMaps.Places.Services.IPlacesService places, "
            "AarxMaps.Places.Services.Searching.TextRequest request, CancellationToken ct)",
        ),
    },
    "core/coordinates.md": {0: Wrapper(CONSOLE, "public static void Run()")},
    "places/overview.md": {0: WEB_BUILDER},
    "routes/overview.md": {0: WEB_BUILDER},
    "routes/waypoints.md": {0: Wrapper(CONSOLE, "public static void Run()")},
    "routes/directions.md": {
        1: Wrapper(
            CONSOLE,
            "public static async Task RunAsync(AarxMaps.Routes.Services.IRoutesService routes, "
            "AarxMaps.Routes.Common.Models.Waypoint depot, "
            "IReadOnlyList<AarxMaps.Routes.Common.Models.Waypoint> stops, CancellationToken ct)",
        ),
        2: Wrapper(CONSOLE, "public static void Run()"),
        3: ROUTES_WAYPOINTS,
        4: ROUTES_REQUEST,
    },
    "routes/route-results.md": {
        0: ROUTES_REQUEST,
        1: Wrapper(CONSOLE, "public static void Run(AarxMaps.Routes.Services.Directions.RouteLeg leg)"),
    },
    "routes/travel-mode-rules.md": {0: ROUTES_WAYPOINTS},
}
FOLDER_DEFAULTS: dict[str, Wrapper] = {
    "places": PLACES_FRAGMENT,
    "routes": ROUTES_FRAGMENT,
}

CSHARP_BLOCK = re.compile(r"```csharp[^\n]*\n(.*?)```", re.S)
USING_LINE = re.compile(r"^using [\w.]+;$")
PUBLIC_TYPE = re.compile(r"^public ", re.M)


# -------------------- PROJECTS --------------------
def project_file(sdk: str, framework: str, extra_items: str = "") -> str:
    output_type = "    <OutputType>Exe</OutputType>\n" if sdk == "Microsoft.NET.Sdk" else ""
    references = "\n".join(
        f'    <ProjectReference Include="{REPO_ROOT / name / name}.csproj" />'
        for name in ("AarxMaps.Core", "AarxMaps.Places", "AarxMaps.Routes")
    )
    return f"""<Project Sdk="{sdk}">
  <PropertyGroup>
{output_type}    <TargetFramework>{framework}</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
{references}
  </ItemGroup>
{extra_items}</Project>
"""


def write_projects(out_dir: Path, framework: str) -> None:
    redis = (
        "  <ItemGroup>\n"
        f'    <PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" '
        f'Version="{REDIS_PACKAGE_VERSION}" />\n'
        "  </ItemGroup>\n"
    )
    for project, sdk, extra in ((CONSOLE, "Microsoft.NET.Sdk", ""), (WEB, "Microsoft.NET.Sdk.Web", redis)):
        project_dir = out_dir / project
        project_dir.mkdir(parents=True, exist_ok=True)
        for old in project_dir.glob("*.cs"):
            old.unlink()
        (project_dir / f"{project}.csproj").write_text(project_file(sdk, framework, extra))
    # The console Program.cs comes from the Introduction; the web app needs an empty one.
    (out_dir / WEB / "Program.cs").write_text("return;\n")


# -------------------- SNIPPETS --------------------
def wrap_fragment(code: str, class_name: str, signature: str) -> str:
    lines = code.splitlines()
    usings = [line for line in lines if USING_LINE.match(line)]
    body = "\n".join("        " + line for line in lines if line not in usings)
    return (
        "\n".join(usings)
        + f"\n\npublic static class {class_name} {{\n    {signature} {{\n{body}\n    }}\n}}\n"
    )


def resolve_wrapper(page: str, index: int, code: str) -> Wrapper | None:
    """Returns the wrapper for a block, or None when it declares public types (copied verbatim)."""
    rule = PAGE_RULES.get(page, {}).get(index)
    if rule is not None:
        return rule
    if PUBLIC_TYPE.search(code):
        return None
    folder_default = FOLDER_DEFAULTS.get(page.split("/")[0])
    if folder_default is not None:
        return folder_default
    raise SystemExit(f"No wrapping rule for {page} block {index}. Add one to PAGE_RULES.")


def write_snippets(out_dir: Path) -> int:
    pages = sorted(
        p.relative_to(DOCS_DIR).as_posix()
        for p in DOCS_DIR.rglob("*.md")
        if p.name not in SKIPPED_PAGES
    )
    count = 0
    for page in pages:
        for index, code in enumerate(CSHARP_BLOCK.findall((DOCS_DIR / page).read_text())):
            class_name = re.sub(r"\W", "_", page.removesuffix(".md")) + f"_{index}"
            wrapper = resolve_wrapper(page, index, code)

            if wrapper is None:
                (out_dir / CONSOLE / f"{class_name}.cs").write_text(code)
            elif wrapper.signature is None:
                (out_dir / wrapper.project / "Program.cs").write_text(code)
            else:
                text = wrap_fragment(code, class_name, wrapper.signature)
                (out_dir / wrapper.project / f"{class_name}.cs").write_text(text)
            count += 1
    return count


# -------------------- BUILD --------------------
def build(project_dir: Path) -> bool:
    result = subprocess.run(
        ["dotnet", "build", str(project_dir), "-warnaserror", "-nologo", "-v", "q"],
        capture_output=True, text=True, check=False,
    )
    diagnostics = sorted({
        line.strip() for line in result.stdout.splitlines()
        if " error " in line or " warning " in line
    })
    status = "ok" if result.returncode == 0 else "FAILED"
    print(f"{project_dir.name}: {status}")
    for line in diagnostics:
        print(f"  {line}")
    return result.returncode == 0


def main() -> int:
    parser = argparse.ArgumentParser(description="Compile the C# snippets of the docs site.")
    parser.add_argument("--out", type=Path, default=Path(tempfile.gettempdir()) / "aarxmaps-snippets",
                        help="Scratch directory for the generated projects (default: system temp).")
    parser.add_argument("--framework", default="net9.0", help="Target framework (default: net9.0).")
    args = parser.parse_args()

    out_dir: Path = args.out.resolve()
    if out_dir.is_relative_to(REPO_ROOT):
        raise SystemExit("--out must be outside the repository.")

    write_projects(out_dir, args.framework)
    count = write_snippets(out_dir)
    print(f"{count} snippets written to {out_dir}")

    builds_ok = [build(out_dir / project) for project in (CONSOLE, WEB)]
    return 0 if all(builds_ok) else 1


if __name__ == "__main__":
    sys.exit(main())
