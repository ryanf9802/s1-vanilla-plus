# schedule_one

Vanilla+ quality-of-life mods for [Schedule I](https://store.steampowered.com/app/3164500/), built with C# and [MelonLoader](https://github.com/LavaGang/MelonLoader) for the default (IL2CPP) Steam branch.

## Mods

| Mod | Status | Description |
|---|---|---|
| [Compass+](Compass/) | 0.1.0 | Distance fade, look-to-reveal distance labels, elevation chevrons and through-wall quest icons for the vanilla compass. |
| Ping | planned | Co-op ping on a keybind, shown on the compass and as an overlay icon. |

Download the latest build from [Releases](https://github.com/ryanf9802/schedule_one/releases).

## Building

Requires the .NET 8 SDK and a MelonLoader 0.7.3 install of the game. Set the game path in `Compass/build/paths.props`, then:

    dotnet build Compass/Compass.csproj -c IL2CPP

The template's post-build step copies the DLL into the game's `Mods` folder and relaunches the game; add `-p:RunPostBuildEvent=Never` to only compile.

To make a release package: `cd Compass && uv run --script assets/package.py --skip-mono`.

## License

MIT. See [Compass/LICENSE.md](Compass/LICENSE.md).
