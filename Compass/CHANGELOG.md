# Changelog

## Unreleased
- Recruited dealers show on the compass with their phone-map portrait in a green round frame (`ShowDealers`; a green sack icon if the game has no portrait), with an optional through-wall icon (`DealersOnOverlay`, off by default).

## 0.2.5
- Fix: pings and player markers did not show in worlds whose compass had no `QuestIcon(Clone)` icon (it can be `QuestIcon_Blue(Clone)` or absent). Their icons are now built from scratch and no longer copy a vanilla icon.
- Overlay icons for vanilla markers accept any icon name.

## 0.2.4
- Fix: after loading another world, other players and pings no longer showed. Cached icon templates from the previous world are now discarded when the game creates a new compass.
- Errors are now logged per source (at most every 10 s) instead of once for everything, and the log notes when player markers and templates are created.

## 0.2.3
- Version bump to match Ping 0.2.3.

## 0.2.2
- Through-wall icons for other players are now off by default (`PlayersOnOverlay`).

## 0.2.1
- Other players (co-op partners) show on the compass and as through-wall icons, each in their own color (`ShowPlayers`; through-wall icons via `PlayersOnOverlay`).
- New API: `CompassApi.AddTrackedMarker` follows a transform; markers can use the `Person` icon. `PlayerColors` gives a per-player color shared with Ping.

## 0.2.0
- Overlay icons now cover everything on the compass (quest objectives, landmarks, NPCs, other markers), not just quest-prefab markers.
- Overlay labels for quest objectives show the objective's own title.
- API markers (used by Ping) get their own pin icon tinted per marker, stay at full opacity at any range, and sit on their spot.
- Overlay icons now appear within 50 ft by default (was 75 ft).
- Through-wall overlay icons hide while the phone is open instead of drawing over it.

## 0.1.0
- Compass markers fade with distance and hide past a configurable maximum.
- Distance labels appear when facing a marker, only for the closest one in view.
- Elevation chevron after the distance label.
- Through-wall overlay icons for quest markers and landmarks, with fade in/out and a name shown while looked at.
- All settings exposed through MelonPreferences with ranges, ready for the Mods App.
