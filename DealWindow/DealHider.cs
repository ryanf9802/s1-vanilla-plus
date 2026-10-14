using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Quests;
using MelonLoader;
using UnityEngine;

namespace DealWindow;

/// <summary>
/// Hides the on-screen pieces of accepted customer deals whose delivery window has not opened yet, and shows them again when it does.
/// Everything is toggled from outside the vanilla quest code (HUD entry, journal entry, map marker, compass element), and only
/// things this class hid are restored, so quests the player hid or tracked on their own are never touched.
/// </summary>
public static class DealHider
{
    private class Hidden
    {
        public bool Hud, Journal, Map, Compass;
        public Contract Contract;
    }

    private static readonly System.Collections.Generic.Dictionary<int, Hidden> State = new();
    private static readonly System.Collections.Generic.List<int> Stale = new();
    private static float _nextCheck;

    public static void Reset() => State.Clear();

    /// <summary>True when <paramref name="quest"/> is an accepted deal that should not be shown yet.</summary>
    public static bool IsPending(Quest quest)
    {
        var contract = quest?.TryCast<Contract>();
        if (contract == null || contract.State != EQuestState.Active) return false;
        var window = contract.DeliveryWindow;
        if (window == null || !window.IsEnabled) return false;
        var tm = TimeManager.Instance;
        if (tm == null) return false;
        int start = ShiftBack(window.WindowStartTime, Settings.LeadMinutes);
        return !TimeManager.IsGivenTimeWithinRange(tm.CurrentTime, start, window.WindowEndTime);
    }

    /// <summary>24-hour time (HHMM) moved back by <paramref name="minutes"/>, wrapping at midnight.</summary>
    internal static int ShiftBack(int time24, int minutes)
    {
        if (minutes <= 0) return time24;
        int total = (time24 / 100) * 60 + time24 % 100 - minutes;
        total = ((total % 1440) + 1440) % 1440;
        return (total / 60) * 100 + total % 60;
    }

    public static void Tick(MelonLogger.Instance log)
    {
        if (Time.unscaledTime < _nextCheck) return;
        _nextCheck = Time.unscaledTime + 0.1f;

        var contracts = Contract.Contracts;
        if (contracts == null) return;

        if (!Settings.Enabled)
        {
            RestoreAll(log);
            return;
        }

        // Remember which contracts still exist so finished or destroyed ones drop out of the bookkeeping.
        foreach (var kv in State) if (kv.Value.Contract == null) Stale.Add(kv.Key);
        foreach (int id in Stale) State.Remove(id);
        Stale.Clear();

        for (int i = 0; i < contracts.Count; i++)
        {
            var c = contracts[i];
            if (c == null) continue;
            int id = c.GetInstanceID();
            bool pending = IsPending(c);
            if (!pending && !State.ContainsKey(id)) continue;

            if (!State.TryGetValue(id, out var h)) State[id] = h = new Hidden { Contract = c };
            bool changed = Apply(c, h, pending);
            if (changed && Settings.DebugLogging)
                log.Msg($"[DealWindow] {(pending ? "hiding" : "showing")} '{c.GetQuestTitle()}' (window {c.DeliveryWindow?.WindowStartTime}-{c.DeliveryWindow?.WindowEndTime}, now {TimeManager.Instance?.CurrentTime})");
            if (!pending && !h.Hud && !h.Journal && !h.Map && !h.Compass) State.Remove(id);
        }
    }

    private static void RestoreAll(MelonLogger.Instance log)
    {
        if (State.Count == 0) return;
        foreach (var h in State.Values)
            if (h.Contract != null) Apply(h.Contract, h, false);
        State.Clear();
    }

    /// <summary>Hide (pending) or restore (not pending) each piece according to the settings. Returns true if anything flipped.</summary>
    private static bool Apply(Contract c, Hidden h, bool pending)
    {
        bool changed = false;

        // Quest HUD entry (the on-screen list).
        bool wantHud = pending && Settings.HideInHud;
        if (wantHud || h.Hud)
        {
            var hud = c.hudUIExists ? c.hudUI : null;
            if (hud != null)
            {
                var go = hud.gameObject;
                if (wantHud && go.activeSelf) { go.SetActive(false); h.Hud = true; changed = true; }
                else if (!wantHud && h.Hud) { go.SetActive(true); h.Hud = false; changed = true; }
            }
            else if (!wantHud) h.Hud = false;
        }

        // Journal (quest list) entry.
        bool wantJournal = pending && Settings.HideInJournal;
        if (wantJournal || h.Journal)
        {
            var je = c.journalEntry;
            if (je != null)
            {
                var go = je.gameObject;
                if (wantJournal && go.activeSelf) { go.SetActive(false); h.Journal = true; changed = true; }
                else if (!wantJournal && h.Journal) { go.SetActive(true); h.Journal = false; changed = true; }
            }
            else if (!wantJournal) h.Journal = false;
        }

        // Map marker and compass marker, per objective entry.
        bool wantMap = pending && Settings.HideOnMap;
        bool wantCompass = pending && Settings.HideOnCompass;
        var entries = c.Entries;
        if ((wantMap || wantCompass || h.Map || h.Compass) && entries != null)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e == null) continue;

                var poi = e.PoI;
                if (poi != null)
                {
                    var go = poi.gameObject;
                    if (wantMap && go.activeSelf) { go.SetActive(false); h.Map = true; changed = true; }
                    else if (!wantMap && h.Map && !go.activeSelf) { go.SetActive(true); changed = true; }
                }

                var el = e.compassElement;
                if (el != null)
                {
                    if (wantCompass && el.Visible) { el.Visible = false; h.Compass = true; changed = true; }
                    else if (!wantCompass && h.Compass && !el.Visible && e.State == EQuestState.Active) { el.Visible = true; changed = true; }
                }
            }
            if (!wantMap) h.Map = false;
            if (!wantCompass) h.Compass = false;
        }

        return changed;
    }
}
