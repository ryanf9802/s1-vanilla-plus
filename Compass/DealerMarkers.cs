using Il2CppScheduleOne.Economy;
using UnityEngine;

namespace Compass;

/// <summary>Keeps one tracked compass marker per recruited dealer (the player's own dealers), with a fixed dealer color.</summary>
internal static class DealerMarkers
{
    private static readonly Color DealerColor = new(0.20f, 0.66f, 0.34f);
    private static readonly System.Collections.Generic.Dictionary<int, MarkerHandle> Tracked = new();
    private static readonly System.Collections.Generic.HashSet<int> Seen = new();

    public static void Tick()
    {
        bool compass = Settings.ShowDealers, overlay = Settings.DealersOnOverlay;
        var list = Dealer.AllPlayerDealers;
        if (list == null || (!compass && !overlay)) { RemoveAll(); return; }

        Seen.Clear();
        foreach (var d in list)
        {
            if (d == null || !d.IsRecruited) continue;
            int id = d.GetInstanceID();
            Seen.Add(id);
            if (Tracked.TryGetValue(id, out var h) && h.Alive)
            {
                if (h.OnCompass != compass || h.OnOverlay != overlay) { h.Remove(); Tracked.Remove(id); }
                continue;
            }
            string name = d.FullName ?? "Dealer";
            MelonLoader.MelonLogger.Msg($"[Compass] tracking dealer '{name}'");
            Tracked[id] = CompassApi.AddTrackedMarker(d.transform, DealerColor, name, MarkerIcon.Dealer,
                compass, overlay, new Vector3(0f, 1.2f, 0f), overlayLiftFt: 1.2f);
        }

        var gone = new System.Collections.Generic.List<int>();
        foreach (var kv in Tracked) if (!Seen.Contains(kv.Key) || !kv.Value.Alive) gone.Add(kv.Key);
        foreach (int id in gone)
        {
            if (Tracked.TryGetValue(id, out var h)) h.Remove();
            Tracked.Remove(id);
        }
    }

    internal static void Reset()
    {
        Tracked.Clear();
        Seen.Clear();
    }

    private static void RemoveAll()
    {
        if (Tracked.Count == 0) return;
        foreach (var h in Tracked.Values) h.Remove();
        Tracked.Clear();
    }
}
