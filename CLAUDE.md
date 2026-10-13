# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

A collection of "vanilla+" quality-of-life mods for Schedule I (Steam), made by two friends playing co-op. Mods are C# MelonLoader DLLs. UI must look native to the game (vanilla fonts, colors, sizes, subtle animation). Planned in this order:

1. **Compass**: extends the vanilla compass with accurate per-marker distance and elevation difference, plus a small marker API (position, color, label, expiry) other mods can feed.
2. **Ping**: co-op ping on a keybind (camera raycast), per-player color, audible cue, 5 second lifetime, rendered on the compass. Networked over SteamNetworkLib (ifBars, Il2Cpp build, pin the version). Depends on Compass.

Layout: one folder per mod (`Compass/`, `Ping/`); there is no `Shared/` folder because Ping reuses Compass through its public API. Scaffold each mod with the `k073l.S1MelonMod` dotnet template (`dotnet new install k073l.S1MelonMod`). Game DLLs must never be committed; reference them via a local, untracked path property.

The overall plan lives outside the repo at `~/.claude/plans/just-do-inspection-and-goofy-bubble.md`.

## Environment (verified on the dev machine)

- WSL2 for editing; the game and the .NET SDK (8.0.425, installed via winget) are on Windows. Run Windows tooling from WSL with `powershell.exe` or `cmd.exe` (run from a non-UNC cwd such as `/tmp`, or `cmd.exe` complains). Listing `/mnt/c` directly can hang.
- **Dev vs main install:** the user swaps which Steam game folder is called `Schedule I` (the one Steam launches, their **main** install, which I must never touch) and the modded dev copy (currently `Schedule I - Mod Dev`). Folder names are therefore NOT reliable. The dev copy is the folder under `C:\Program Files (x86)\Steam\steamapps\common` that contains a `.s1vp-dev` marker file; `tools/s1` finds it by that marker (`tools/s1 mark "<folder>"` to set it, `tools/s1 dir` to print it) and refuses to run unless exactly one folder has it. Never hardcode a game path, never copy to or kill anything outside that folder. The dev copy has MelonLoader 0.7.3 (`Mods`, `UserData`, `UserLibs`; `MelonLoader\Il2CppAssemblies` generated).
- Game is the default **IL2CPP** branch (`GameAssembly.dll`, no `Managed` folder). Target the IL2CPP build; game types appear under `Il2CppScheduleOne.*` and interop assemblies are generated into `MelonLoader\Il2CppAssemblies` on first launch.
- Mods load by placing the DLL in the game's `Mods` folder. Co-op needs the same mod versions, MelonLoader, and branch on both players' machines.
- Save backup lives at `C:\Users\ryanf\Documents\ScheduleI_save_backup_20261013_012246`. Live saves are in `%USERPROFILE%\AppData\LocalLow\TVGS\Schedule I`.

## Notes

- Day-to-day commands go through `tools/s1` (`tools/s1 mainlog [regex]` reads the newest log of the user's MAIN install, strictly read-only; the user allows reading its logs but never writing there): `build [Compass|Ping]` (compile only), `deploy` (closes the dev game, copies DLLs into the dev folder's `Mods`/`UserLibs`), `launch` (console hidden), `kill`, `log [regex]`. `kill` only stops a `Schedule I` process whose path is inside the dev folder. `build/paths.props` takes the dev folder from `-p:S1DevDir` or the `S1_DEV_DIR` env var (the build errors out if neither is set) and sets `RunPostBuildEvent=Never`, because the template's post-build step runs `taskkill /IM "Schedule I.exe"` (which would also kill the main install), copies the DLL and relaunches the game. Direct build: `dotnet build <Mod>/<Mod>.csproj -c IL2CPP -p:S1DevDir=<dev folder>` via Windows dotnet from a non-UNC cwd. Optional-assembly warnings are noise. There are no tests or linters.
- Mod projects come from the template (`dotnet new S1MelonMod -n <Name> -S <game dir> -S1 <game dir>`); the game path comes from `<Mod>/build/paths.props` (see above). The vanilla `CompassManager` already supports per-target elements, so build on it; see `docs/recon.md` for findings and the decompile workflow.
- Game updates can break Harmony patches and mod network sync, so record the game build id (currently 25796331) and library versions in the README when pinning.
- **Console freeze gotcha:** the MelonLoader console window is a Windows console in select mode when clicked, and that blocks the game's log writes, so the game looks crashed or hung (often on the loading screen). The title bar then reads "Select ...". Launching with `--melonloader.hideconsole` avoids it; the template's post-build launch does not pass that flag. Logs are read from `MelonLoader\Logs` instead (see memory note on log reading).
- Settings are written to `UserData\MelonPreferences.cfg` only on a clean game exit; force-quitting skips that, and saved values override new code defaults, so delete or edit stale entries when changing a default.

## Ping architecture

- `Ping/` references `Compass` as a ProjectReference (built with `RunPostBuildEvent=Never`, so building Ping never kills the game) and `Ping/libs/SteamNetworkLib.dll` (ifBars/SteamNetworkLib 1.6.0 IL2CPP, MIT, committed; at runtime it lives in `UserLibs/SteamNetworkLib-IL2Cpp.dll`). `[assembly: MelonAdditionalDependencies("Compass-IL2CPP")]` orders loading.
- Compass exposes `Compass.CompassApi.AddMarker(pos, color, lifetime, name, onCompass, onOverlay)`: it creates an anchor GameObject, adds a native compass element cloned from a vanilla quest icon (`CompassEnhancer.IconTemplate`), tints it, and submits a through-wall overlay icon each frame; it removes everything at expiry. API markers skip the 500 ft compass cutoff and fade out over their last 0.6 s.
- Networking: `SteamNetworkClient` is created in `OnInitializeMelon`, `TryInitialize()` retried every 2 s, `ProcessIncomingMessages()` every frame. SteamNetworkLib listens to Steam's *unfiltered* lobby-entered callback, so it adopts the game's own co-op lobby if the client existed before the lobby was entered; otherwise Ping joins `Il2CppScheduleOne.Networking.Lobby.Instance.LobbyID` explicitly. A ping is a `TypedP2PMessage<PingPayload>` (world position only; sender identity and name come from Steam/lobby, never the payload) broadcast to the lobby. Colors: rank of the Steam ID among sorted lobby members, so all machines agree.
- Ping raycast (`Ping/PingRaycast.cs`): the game disables the box colliders of distant buildings (active but `enabled=false` until you are within ~65 ft), so a plain `Physics.Raycast` passes through far buildings. `PingRaycast.Cast` also ray-tests those disabled map boxes (cached index of non-trigger BoxColliders under root `Map`, refreshed every 60 s) and takes the nearer hit.
- Status: compiles; **not yet run in-game or with two players**. Known unknowns: whether the library adopts the game lobby by itself, whether legacy `Input` is usable (code falls back to the Input System), whether the synthesized sound respects the game's volume, and whether `AddElement` accepts the cloned icon as its content prefab.

## Settings and distribution

- Settings (`Compass/Settings.cs`) follow the Mods App (k0Mods, github.com/k073l/s1-modsapp) conventions so it can show them in-game: every entry has a display name, description and, for numbers, a `ValueRange`; category identifiers start with the MelonInfo mod name (`Compass`, `Compass_Overlay`). Values are read live, so edits apply immediately. New settings must follow this pattern.
- Package (IL2CPP only; there is no Mono build here): build each mod first, then run `./package_all.py` at the repo root. It runs every `<Mod>/assets/package.py --skip-mono`, writes `dist/<PackageName>-<ver>.zip` per mod, and an all-in-one `dist/S1VanillaPlus-<ver>.zip` (extract into the game folder; contains `Mods/<dll>`). A new mod just needs the template's `assets/` folder and it is picked up automatically. Inter-mod dependencies go in the mod's `assets/manifest.json` (Ping's manifest still lacks the Compass+ entry until the Thunderstore team name exists); files a mod needs outside its DLL go in `assets/bundle-extra.json` (Ping ships SteamNetworkLib to `UserLibs`) and are added to the all-in-one bundle only. The bundle always satisfies dependencies. Releases are on GitHub (repo `ryanf9802/s1-vanilla-plus`, public); attach both kinds of zip. Thunderstore modpack manifest for the bundle can't be written until the Thunderstore team name exists. Version lives in `Compass.csproj`, `MainMod.cs` (BuildInfo), `assets/manifest.json` and `assets/fomod/info.xml`; `assets/bumper.py` can bump them together. Thunderstore package name is `CompassPlus`; its dependency is `LavaGang-MelonLoader-0.7.3`.
- Other compass mods on Thunderstore overlap with this one (BetterCompass, AlwaysShowDistancesOnCompass, CompassWaypointsonscreen); noted in the README.

## Roadmap / deferred

- **Settings UI:** Compass settings are now Mods App-ready (see above); still to verify in-game with the Mods App installed, and to do the same for Ping.
- **Marker overlay (proposed, not started):** world-space marker shown above a target within ~50ft, through walls, used by both Compass (nearby markers) and Ping. Plan: lives inside the Compass mod as a public marker API with a screen-space overlay canvas (project with `Camera.WorldToScreenPoint`); Ping depends on Compass rather than a third mod.
