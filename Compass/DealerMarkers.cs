using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.Product;
using UnityEngine;

namespace Compass;

/// <summary>Keeps one tracked compass marker per recruited dealer (the player's own dealers), with a fixed dealer color.</summary>
internal static class DealerMarkers
{
    private static readonly Color DealerColor = new(0.20f, 0.66f, 0.34f);   // well stocked
    private static readonly Color LowColor = new(0.72f, 0.27f, 0.25f);      // empty: a muted red
    private static float _nextStockCheck;
    private static readonly System.Collections.Generic.Dictionary<int, MarkerHandle> Tracked = new();
    private static readonly System.Collections.Generic.HashSet<int> Seen = new();

    public static void Tick()
    {
        bool compass = Settings.ShowDealers, overlay = Settings.DealersOnOverlay;
        var list = Dealer.AllPlayerDealers;
        if (list == null || (!compass && !overlay)) { RemoveAll(); return; }

        bool refresh = Time.time >= _nextStockCheck;
        if (refresh) _nextStockCheck = Time.time + 1f;
        Seen.Clear();
        foreach (var d in list)
        {
            if (d == null || !d.IsRecruited) continue;
            int id = d.GetInstanceID();
            Seen.Add(id);
            if (Tracked.TryGetValue(id, out var h) && h.Alive)
            {
                if (h.OnCompass != compass || h.OnOverlay != overlay) { h.Remove(); Tracked.Remove(id); }
                else if (refresh) h.Color = StockColor(d);
                continue;
            }
            string name = d.FullName ?? "Dealer";
            MelonLoader.MelonLogger.Msg($"[Compass] tracking dealer '{name}'");
            Sprite mugshot = null;
            try { mugshot = d.MugshotSprite; } catch { /* no portrait: the sack icon is used */ }
            Tracked[id] = CompassApi.AddTrackedMarker(d.transform, StockColor(d), name, MarkerIcon.Dealer,
                compass, overlay, new Vector3(0f, 1.2f, 0f), overlayLiftFt: 1.2f, portrait: mugshot);
        }

        var gone = new System.Collections.Generic.List<int>();
        foreach (var kv in Tracked) if (!Seen.Contains(kv.Key) || !kv.Value.Alive) gone.Add(kv.Key);
        foreach (int id in gone)
        {
            if (Tracked.TryGetValue(id, out var h)) h.Remove();
            Tracked.Remove(id);
        }
    }

    /// <summary>Green when the dealer carries at least LowStock units of product, shading to red as they run out.</summary>
    private static Color StockColor(Dealer d)
    {
        int units = 0;
        try
        {
            var slots = d.ItemSlots;
            if (slots != null)
                foreach (var slot in slots)
                    if (slot?.ItemInstance != null && slot.ItemInstance.TryCast<ProductItemInstance>() != null) units += slot.Quantity;
        }
        catch { return DealerColor; }
        float t = Mathf.Clamp01(units / Mathf.Max(1f, Settings.DealerLowStock));
        return Color.Lerp(LowColor, DealerColor, t);
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
