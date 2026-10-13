# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

A collection of "vanilla+" quality-of-life mods for Schedule I (Steam), made by two friends playing co-op. Mods are C# MelonLoader DLLs. UI must look native to the game (vanilla fonts, colors, sizes, subtle animation). Planned in this order:

1. **Compass**: extends the vanilla compass with accurate per-marker distance and elevation difference, plus a small marker API (position, color, label, expiry) other mods can feed.
2. **Ping**: co-op ping on a keybind (camera raycast), per-player color, audible cue, 5 second lifetime, rendered on the compass. Networked over SteamNetworkLib (ifBars, Il2Cpp build, pin the version). Depends on Compass.

Intended layout: one folder per mod (`Compass/`, `Ping/`) plus `Shared/` for common code. Scaffold each mod with the `k073l.S1MelonMod` dotnet template (`dotnet new install k073l.S1MelonMod`). Game DLLs must never be committed; reference them via a local, untracked path property.

The overall plan lives outside the repo at `~/.claude/plans/just-do-inspection-and-goofy-bubble.md`.

## Environment (verified on the dev machine)

- WSL2 for editing; the game and the .NET SDK (8.0.425, installed via winget) are on Windows. Run Windows tooling from WSL with `powershell.exe` or `cmd.exe` (run from a non-UNC cwd such as `/tmp`, or `cmd.exe` complains). Listing `/mnt/c` directly can hang.
- Folder roles under `C:\Program Files (x86)\Steam\steamapps\common`: `Schedule I` is the **modded** install (MelonLoader 0.7.3; `Mods`, `UserData`, `UserLibs`; `MelonLoader\Il2CppAssemblies` already generated). `Schedule I - Main` is the untouched **vanilla** copy; don't modify it. Steam launches the folder named `Schedule I`.
- Game is the default **IL2CPP** branch (`GameAssembly.dll`, no `Managed` folder). Target the IL2CPP build; game types appear under `Il2CppScheduleOne.*` and interop assemblies are generated into `MelonLoader\Il2CppAssemblies` on first launch.
- Mods load by placing the DLL in the game's `Mods` folder. Co-op needs the same mod versions, MelonLoader, and branch on both players' machines.
- Save backup lives at `C:\Users\ryanf\Documents\ScheduleI_save_backup_20261013_012246`. Live saves are in `%USERPROFILE%\AppData\LocalLow\TVGS\Schedule I`.

## Notes

- Build (from `/tmp`, via Windows dotnet; the project path must be a Windows/UNC path): `powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' build '<win path>\Compass\Compass.csproj' -c IL2CPP"`. The template's post-build step force-kills any running game (`taskkill /F`), copies the DLL into the game's `Mods` folder, **and relaunches the game**. To compile only without disturbing a running game, add `-p:RunPostBuildEvent=Never` and filter output with `| Select-String 'error|Build succeeded'` (the optional-assembly warnings are noisy). There are no tests or linters.
- Mod projects come from the template (`dotnet new S1MelonMod -n <Name> -S <game dir> -S1 <game dir>`); game paths live in `<Mod>/build/paths.props`. The vanilla `CompassManager` already supports per-target elements, so build on it; see `docs/recon.md` for findings and the decompile workflow.
- Game updates can break Harmony patches and mod network sync, so record the game build id (currently 25796331) and library versions in the README when pinning.
- **Console freeze gotcha:** the MelonLoader console window is a Windows console in select mode when clicked, and that blocks the game's log writes, so the game looks crashed or hung (often on the loading screen). The title bar then reads "Select ...". Launching with `--melonloader.hideconsole` avoids it; the template's post-build launch does not pass that flag. Logs are read from `MelonLoader\Logs` instead (see memory note on log reading).
- Settings are written to `UserData\MelonPreferences.cfg` only on a clean game exit; force-quitting skips that, and saved values override new code defaults, so delete or edit stale entries when changing a default.

## Settings and distribution

- Settings (`Compass/Settings.cs`) follow the Mods App (k0Mods, github.com/k073l/s1-modsapp) conventions so it can show them in-game: every entry has a display name, description and, for numbers, a `ValueRange`; category identifiers start with the MelonInfo mod name (`Compass`, `Compass_Overlay`). Values are read live, so edits apply immediately. New settings must follow this pattern.
- Package (IL2CPP only; there is no Mono build here): build each mod first, then run `./package_all.py` at the repo root. It runs every `<Mod>/assets/package.py --skip-mono`, writes `dist/<PackageName>-<ver>.zip` per mod, and an all-in-one `dist/S1VanillaPlus-<ver>.zip` (extract into the game folder; contains `Mods/<dll>`). A new mod just needs the template's `assets/` folder and it is picked up automatically. Inter-mod dependencies (Ping needs Compass+) go in the mod's `assets/manifest.json`; the bundle always satisfies them. Releases are on GitHub (repo `ryanf9802/s1-vanilla-plus`, public); attach both kinds of zip. Thunderstore modpack manifest for the bundle can't be written until the Thunderstore team name exists. Version lives in `Compass.csproj`, `MainMod.cs` (BuildInfo), `assets/manifest.json` and `assets/fomod/info.xml`; `assets/bumper.py` can bump them together. Thunderstore package name is `CompassPlus`; its dependency is `LavaGang-MelonLoader-0.7.3`.
- Other compass mods on Thunderstore overlap with this one (BetterCompass, AlwaysShowDistancesOnCompass, CompassWaypointsonscreen); noted in the README.

## Roadmap / deferred

- **Settings UI:** Compass settings are now Mods App-ready (see above); still to verify in-game with the Mods App installed, and to do the same for Ping.
- **Marker overlay (proposed, not started):** world-space marker shown above a target within ~50ft, through walls, used by both Compass (nearby markers) and Ping. Plan: lives inside the Compass mod as a public marker API with a screen-space overlay canvas (project with `Camera.WorldToScreenPoint`); Ping depends on Compass rather than a third mod.
