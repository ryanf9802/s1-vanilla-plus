# Changelog

## 0.2.4
- Version bump to match Compass+ 0.2.4 (world-change fix).

## 0.2.3
- Fix ping colors not matching the sender's compass marker: pings now carry the sender's in-game name (both players need this version), instead of being matched by Steam name.

## 0.2.2
- Version bump to match Compass+ 0.2.2.

## 0.2.1
- Ping colors now match the player's compass marker (ranked by in-game name via Compass+).

## 0.2.0
- Pings now land on distant buildings. The game turns building colliders off at range, so the ping also tests those disabled colliders.
- Directional ping sound: pans toward the ping's side of your view and is muffled when the ping is behind you (`Directional` setting).

## 0.1.0
- Ping where you look with a configurable key (middle mouse by default).
- Partners see a marker in their own color on the compass and as a through-wall icon, with a sound.
- Pings last 5 seconds and fade out.
- Settings use MelonPreferences with ranges (Mods App compatible).
