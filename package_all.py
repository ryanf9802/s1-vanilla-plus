#!/usr/bin/env -S uv run --script
# /// script
# requires-python = ">=3.12"
# dependencies = []
# ///
"""
Package every mod in this repo (any top-level folder with assets/package.py), then bundle them.

Prerequisite: build each mod's IL2CPP configuration first (dotnet build <Mod>/<Mod>.csproj -c IL2CPP).

Output in ./dist:
  <PackageName>-<version>.zip   one per mod, for installing a single mod (Thunderstore/Nexus/GitHub)
  S1VanillaPlus-<version>.zip   all mods together; extract into the game folder (it contains Mods/<dll>)

Dependencies between mods (e.g. Ping needs Compass) are recorded in each mod's assets/manifest.json; a single-mod zip
only works if its dependencies are also installed, which is what the all-in-one bundle guarantees.
"""
import argparse
import json
import subprocess
import sys
import zipfile
from pathlib import Path

root = Path(__file__).resolve().parent
dist = root / "dist"


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--version", help="bundle version (default: highest mod version)")
    args = ap.parse_args()

    # A mod folder containing an `.unreleased` file is still experimental and is left out of every package.
    mods = sorted(p.parent.parent for p in root.glob("*/assets/package.py") if not (p.parent.parent / ".unreleased").exists())
    if not mods:
        print("no mods found")
        return 1
    dist.mkdir(exist_ok=True)

    bundle_files: dict[str, bytes] = {}
    versions = []
    for mod in mods:
        print(f"== {mod.name}")
        subprocess.run(["uv", "run", "--quiet", "--script", "assets/package.py", "--skip-mono"], cwd=mod, check=True)
        manifest = json.loads((mod / "assets" / "manifest.json").read_text(encoding="utf-8"))
        name, version = manifest["name"], manifest["version_number"]
        versions.append(tuple(int(x) for x in version.split(".")))
        single = dist / f"{name}-{version}.zip"
        single.write_bytes((mod / "dist" / f"{mod.name}.zip").read_bytes())
        print(f"   {single.name}  deps: {', '.join(manifest.get('dependencies', [])) or 'none'}")
        with zipfile.ZipFile(single) as z:
            for info in z.infolist():
                if info.filename.endswith(".dll"):
                    bundle_files[f"Mods/{Path(info.filename).name}"] = z.read(info)
        # Files a mod needs outside its own DLL (e.g. a networking library for UserLibs): assets/bundle-extra.json maps
        # a path inside the game folder to a file relative to the mod folder. Single-mod zips list these as dependencies instead.
        extra = mod / "assets" / "bundle-extra.json"
        if extra.exists():
            for arc, rel in json.loads(extra.read_text(encoding="utf-8")).items():
                bundle_files[arc] = (mod / rel).read_bytes()

    version = args.version or ".".join(map(str, max(versions)))
    bundle = dist / f"S1VanillaPlus-{version}.zip"
    with zipfile.ZipFile(bundle, "w", zipfile.ZIP_DEFLATED) as z:
        for arc, data in bundle_files.items():
            z.writestr(arc, data)
        for f in ("README.md", "LICENSE.md"):
            src = root / f if (root / f).exists() else mods[0] / f
            if src.exists():
                z.write(src, f)
    print(f"== bundle {bundle.name}: {', '.join(sorted(bundle_files))}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
