# DealWindow recon

Decompiled from the interop assemblies (signatures only, no bodies), game build 0.4.7f12. Behavior marked (unverified) must be confirmed in game.

## How contract timing works

- A customer deal is `Il2CppScheduleOne.Quests.Contract : Quest` (also in static `Contract.Contracts`). Fields: `Customer`, `Dealer`, `Payment`, `ProductList`, `DeliveryLocation`, `AcceptTime` (GameDateTime), `PickupScheduleIndex`, and **`DeliveryWindow`** (`QuestWindowConfig`).
- `QuestWindowConfig`: `bool IsEnabled`, `int WindowStartTime`, `int WindowEndTime`. Times are 24-hour HHMM ints (for example 1500) as used by `TimeManager.CurrentTime` (unverified; consistent with `TimeManager.IsGivenTimeWithinRange(int givenTime, int min, int max)` and `Get24HourTimeFromMinSum`).
- Vanilla also has `ContractInfo` (the offer: `DeliveryWindow`, `Expires`, `ExpiresAfter`, `IsCounterOffer`) which becomes the Contract on acceptance (`InitializeContract(..., QuestWindowConfig deliveryWindow, ..., GameDateTime acceptTime)`).
- `Contract` overrides `ShouldQuestShowUI`, `ShouldShowJournalEntry`, `OnUncappedMinPass`, and has `UpdateTiming`/`UpdatePoI`. Their bodies are unknown, and vanilla evidently shows the deal before the window opens anyway.
- Time: `TimeManager.Instance.CurrentTime` (HHMM int), `ElapsedDays`, static `IsGivenTimeWithinRange`.

## Where a deal is visible

- Quest HUD: `Quest.hudUI` (`QuestHUDUI`, created by `SetupHUDUI`, `hudUIExists`).
- Journal: `Quest.journalEntry` (RectTransform built by `SetupJournalEntry`).
- Map: each `QuestEntry.PoI` (`POI` MonoBehaviour; `QuestEntry.ShouldShowPoI`, `UpdatePoI`, `CreatePoI`).
- Compass: each `QuestEntry.compassElement` (`CompassManager.Element`, has settable `Visible`). Compass+'s through-wall overlay only draws elements with `Visible == true` (`CompassEnhancer.Apply`), so hiding the element hides the overlay icon too.

## Hook chosen

Per-frame (every 0.1 s) toggling from outside vanilla, in `DealHider`: for each `Contract` that is `Active` with an enabled window and the current time outside `[start - LeadMinutes, end)`, deactivate `hudUI.gameObject`, `journalEntry.gameObject` and each entry's `PoI.gameObject`, and set each entry's `compassElement.Visible = false`. When the window opens, only the things we hid are reactivated (tracked per contract).

Why not patch `ShouldQuestShowUI` / `ShouldShowJournalEntry`: if vanilla consults them once at creation, returning false could stop the UI from ever being created, and the deal would stay hidden after the window opens. Toggling visibility is reversible by construction. Two best-effort Harmony postfixes (`QuestEntry.ShouldShowPoI` and `QuestEntry.UpdateCompassElement`) stop vanilla re-showing the map and compass markers between ticks; the mod works (with a possible one-tick flicker) if they fail to patch.

"Pending" is anything outside the window, not strictly "before it starts": a missed window is either expired by vanilla or recurs later, and in both cases the deal is not actionable.

## Unverified / risks

- Whether `Contract.DeliveryWindow` is populated and times are HHMM (the mod logs nothing unless `DebugLogging` is on).
- Deactivating `hudUI.gameObject` may make vanilla log "coroutine couldn't be started" if it calls FadeIn/Bop on it while hidden.
- `POI.OnDisable`/`OnEnable` may destroy and rebuild the map marker UI; re-enabling should recreate it but is untested.
- Vanilla may re-activate `journalEntry` on journal refresh (then it flickers back until the next tick).
- Vanilla may not reset `compassElement.Visible` on its own when the window opens; the mod restores it to `true` for Active entries.
