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

- Build (from `/tmp`, via Windows dotnet; the project path must be a Windows/UNC path): `powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' build '<win path>\Compass\Compass.csproj' -c IL2CPP"`. The template's post-build step copies the DLL into the game's `Mods` folder **and launches the game**. There are no tests or linters.
- Mod projects come from the template (`dotnet new S1MelonMod -n <Name> -S <game dir> -S1 <game dir>`); game paths live in `<Mod>/build/paths.props`. The vanilla `CompassManager` already supports per-target elements, so build on it; see `docs/recon.md` for findings and the decompile workflow.
- Game updates can break Harmony patches and mod network sync, so record the game build id (currently 25796331) and library versions in the README when pinning.
