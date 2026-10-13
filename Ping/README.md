# Ping

Co-op ping for Schedule I. Press the ping key (middle mouse by default) to mark the spot you are looking at. Everyone in your co-op session sees a marker in *your* color on their compass and as an icon above the spot, visible through walls, with a short sound. Pings last 5 seconds and fade out.

Part of [s1-vanilla-plus](https://github.com/ryanf9802/s1-vanilla-plus). It builds on **Compass+**, which draws the markers, so both must be installed.

## Installation
The easiest way is the all-in-one `S1VanillaPlus` zip from the [releases page](https://github.com/ryanf9802/s1-vanilla-plus/releases): extract it into the game folder. It contains Compass+, Ping, and the networking library Ping needs.

To install by hand you need: MelonLoader 0.7.3, `Compass-IL2CPP.dll` and `Ping-IL2CPP.dll` in `Mods`, and [SteamNetworkLib](https://thunderstore.io/c/schedule-i/p/ifBars/SteamNetworkLib_Il2Cpp/) (`SteamNetworkLib-IL2Cpp.dll` in `UserLibs`).

Everyone who should see each other's pings needs the mods installed. Players without them are not affected and simply do not see pings.

## Settings
In `UserData/MelonPreferences.cfg` under `[Ping]`, or in-game with the Mods App: ping key, duration, max distance, cooldown, volume, and whether pings show on the compass and/or in the world.

## Notes
- Default (IL2CPP) Steam branch only. Tested on game version 0.4.7f12.
- Pings use the Steam lobby the game already creates for co-op. If Steam networking is unavailable you can still ping yourself.
- Colors are assigned from the players' Steam IDs, so every player sees the same color for the same person.

## License
MIT. See `LICENSE.md`.
