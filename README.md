# s1-vanilla-plus

Vanilla+ quality-of-life mods for [Schedule I](https://store.steampowered.com/app/3164500/), built with C# and [MelonLoader](https://github.com/LavaGang/MelonLoader) for the default (IL2CPP) Steam branch.

## Mods

| Mod | Status | Description |
|---|---|---|
| [Compass+](Compass/) | 0.3.0 | Distance fade, look-to-reveal distance labels, elevation chevrons and through-wall quest icons for the vanilla compass. |
| [Ping](Ping/) | 0.3.0 (co-op testing in progress; co-op test pending) | Co-op ping on a keybind, in each player's color, shown on the compass and as a through-wall icon, with a sound. Needs Compass+. |

## Installing

Download from [Releases](https://github.com/ryanf9802/s1-vanilla-plus/releases) (MelonLoader 0.7.3 required):

- **All mods:** `S1VanillaPlus-<version>.zip`. Extract it into the game folder; it puts every mod's DLL in `Mods/`.
- **One mod:** `<PackageName>-<version>.zip` (for example `CompassPlus-0.3.0.zip`); copy its DLL into `Mods/`. If a mod depends on another (Ping needs Compass+ and the SteamNetworkLib library), install those too. The all-in-one zip always has everything it needs.

## Optional: in-game settings

- [Mods App](https://thunderstore.io/c/schedule-i/p/k0Mods/ModsApp/)
- [S1API](https://github.com/ifBars/S1API/releases/) (required by the Mods App)

## Building

Requires the .NET 8 SDK and a MelonLoader 0.7.3 install of the game, kept as a separate copy for development. Point the build at that copy with an `S1_DEV_DIR` environment variable (or `-p:S1DevDir=<folder>`), then:

    dotnet build Compass/Compass.csproj -c IL2CPP

The build never runs the template's post-build step (it killed and relaunched the game); copy the DLL into the game's `Mods` folder yourself. `tools/s1` (WSL/bash) wraps build, deploy and launch for a game folder marked with a `.s1vp-dev` file.

To make release packages: build each mod, then `./package_all.py`. It writes one zip per mod plus the all-in-one bundle to `dist/`.

## License

MIT. See [Compass/LICENSE.md](Compass/LICENSE.md).
