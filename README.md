# s1-vanilla-plus

Vanilla+ quality-of-life mods for [Schedule I](https://store.steampowered.com/app/3164500/), built with C# and [MelonLoader](https://github.com/LavaGang/MelonLoader) for the default (IL2CPP) Steam branch.

## Mods

| Mod | Status | Description |
|---|---|---|
| [Compass+](Compass/) | 0.2.0 | Distance fade, look-to-reveal distance labels, elevation chevrons and through-wall quest icons for the vanilla compass. |
| [Ping](Ping/) | 0.2.0 (solo-tested; co-op test pending) | Co-op ping on a keybind, in each player's color, shown on the compass and as a through-wall icon, with a sound. Needs Compass+. |

## Installing

Download from [Releases](https://github.com/ryanf9802/s1-vanilla-plus/releases) (MelonLoader 0.7.3 required):

- **All mods:** `S1VanillaPlus-<version>.zip`. Extract it into the game folder; it puts every mod's DLL in `Mods/`.
- **One mod:** `<PackageName>-<version>.zip` (for example `CompassPlus-0.2.0.zip`); copy its DLL into `Mods/`. If a mod depends on another (Ping needs Compass+ and the SteamNetworkLib library), install those too. The all-in-one zip always has everything it needs.

## Building

Requires the .NET 8 SDK and a MelonLoader 0.7.3 install of the game. Set the game path in `Compass/build/paths.props`, then:

    dotnet build Compass/Compass.csproj -c IL2CPP

The template's post-build step copies the DLL into the game's `Mods` folder and relaunches the game; add `-p:RunPostBuildEvent=Never` to only compile.

To make release packages: build each mod, then `./package_all.py`. It writes one zip per mod plus the all-in-one bundle to `dist/`.

## License

MIT. See [Compass/LICENSE.md](Compass/LICENSE.md).
