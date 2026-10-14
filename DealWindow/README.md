# DealWindow

Customer deals you accept show up in the quest list, map and compass straight away, even when their delivery window does not start for hours. DealWindow keeps them hidden until the window opens, then shows them as vanilla does. No new UI.

Part of [s1-vanilla-plus](https://github.com/ryanf9802/s1-vanilla-plus). Status: in development, not released. Works alongside Compass+ (its through-wall icons follow the compass marker) but does not require it.

## Settings
In `UserData/MelonPreferences.cfg` under `[DealWindow]`, or in-game with the Mods App: enable, show-early lead time in minutes (0 = exactly when the window opens), and separate switches for the quest HUD, journal, map and compass.

## Notes
- Default (IL2CPP) Steam branch only. Deals without a delivery window are never hidden.
- Hiding is cosmetic: the deal is still accepted and its timers still run.

## License
MIT. See `LICENSE.md`.
