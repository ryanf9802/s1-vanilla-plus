# Changelog

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
