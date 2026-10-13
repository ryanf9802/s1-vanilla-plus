# Compass+

A "vanilla+" upgrade to Schedule I's compass. Everything uses the game's own fonts, colors and icons, so it looks like it shipped with the game.

## Features
- **Distance fade:** compass markers dim with distance, are fully opaque up close, and disappear past 500 ft.
- **Look to reveal distance:** a marker's distance label shows at *any* range while you face it (vanilla only labels markers within ~165 ft, regardless of where you look). Only the closest marker in your view gets a label, so clusters stay readable.
- **Elevation chevron:** a small up or down chevron after the distance tells you the target is 10 ft or more above or below you.
- **Through-wall marker icons:** anything shown on the compass (quest objectives, landmarks, NPCs) within 50 ft gets its own icon floating above it, visible through walls. Icons ease in as you approach and fade out as you reach the marker. Look at an icon and its name appears underneath.
- **Co-op partners on the compass:** every other player in your game gets a marker in their own color (a disc with a person silhouette), plus, if you turn on `PlayersOnOverlay`, an icon above them through walls when they are close. Colors are the same for everyone and match that player's pings if you also run Ping. Nothing is synced: it reads the player list the game already has.
- **Your dealers on the compass:** every dealer you have recruited gets a marker showing their portrait (the same picture the phone map uses) in a ring that is green when they are well stocked and shades to red as their product runs out, so you can find where they are and who needs a restock. Optionally also an icon above them through walls (off by default).
- **Everything is configurable**: see below.

## Installation
1. Install [MelonLoader](https://github.com/LavaGang/MelonLoader/releases) 0.7.3 on Schedule I (default Steam branch).
2. Put `Compass-IL2CPP.dll` in the game's `Mods` folder.
3. Launch the game.

Only the default (IL2CPP) Steam branch is supported. The alternate/Mono branch is not.

## Settings
Settings live in `UserData/MelonPreferences.cfg` under `[Compass]`, `[Compass_Players]` (partners and dealers) and `[Compass_Overlay]`, and apply the next time you launch the game. For in-game sliders and toggles, install the [Mods App](https://thunderstore.io/c/schedule-i/p/k0Mods/ModsApp/) (it needs the S1API fork); changes made there apply immediately.

| Setting | Default | Meaning |
|---|---|---|
| `MaxDistanceFt` | 500 | Compass markers farther than this are hidden |
| `FadeStartFt` | 50 | Fully opaque inside this distance |
| `MinAlpha` | 0.25 | Opacity at max distance |
| `FacingAngle` | 6 | Degrees from straight ahead that count as "facing" a marker |
| `ChevronThresholdFt` / `ChevronSize` | 10 / 8 | When the elevation chevron shows, and its size |
| `ShowPlayers` / `PlayersOnOverlay` | on / off | Co-op partners on the compass strip / through walls |
| `ShowDealers` / `DealersOnOverlay` | on / off | Your recruited dealers on the compass strip / through walls |
| `DealerLowStock` | 10 | Units of product at which a dealer's frame is fully green (fewer shades toward red) |
| `OverlayMaxFt` | 50 | Overlay icons for quest markers, landmarks and NPCs appear within this distance |
| `DealerOverlayMaxFt` / `PlayerOverlayMaxFt` | 100 / 150 | Same, for dealers and co-op partners |
| `OverlayFadeInFt` | 16.4 | Icons fade in over this distance (5 m) |
| `OverlayFadeStartFt` / `OverlayFadeEndFt` | 6 / 2 | Icons fade out between these distances |
| `OverlayHeightFt` | 5 | Height above the target |
| `OverlaySize` / `OverlayOpacity` | 22 / 0.6 | Icon size and maximum opacity |
| `OverlayQuestsOnly` | off | Only numbered quest objectives (nothing else) |
| `OverlayShowNames` | on | Show a name under the icon you look at |

## Compatibility
- Other mods that change compass labels or markers (BetterCompass, AlwaysShowDistancesOnCompass, CompassWaypointsonscreen, CompassOverhaul) overlap with this one. Use only one at a time.
- The mod is client-side only: it changes your own HUD and nothing is synced in co-op, so you can use it without your friends having it.
- Game updates can break mods. Tested on game version 0.4.7f12 with MelonLoader 0.7.3.

## License
MIT. See `LICENSE.md`.
