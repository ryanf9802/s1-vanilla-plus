using Il2CppScheduleOne.PlayerScripts;
using UnityEngine;

namespace Compass;

/// <summary>Keeps one tracked compass marker per other player in the game (co-op partners), colored per player.</summary>
internal static class PlayerMarkers
{
    private static readonly System.Collections.Generic.Dictionary<int, MarkerHandle> Tracked = new();
    private static readonly System.Collections.Generic.HashSet<int> Seen = new();
    private static float _nextRefresh;
    private static float _eyeHeight = 1.6f;

    public static void Tick()
    {
        bool compass = Settings.ShowPlayers, overlay = Settings.PlayersOnOverlay;
        var list = Player.PlayerList;
        var local = Player.Local;
        if (list == null || local == null || (!compass && !overlay)) { RemoveAll(); return; }

        bool refresh = Time.time >= _nextRefresh;
        if (refresh)
        {
            _nextRefresh = Time.time + 1f;
            var cam = Camera.main;
            // Calibrate where a player's root sits relative to their eyes, so markers land at chest/eye height.
            if (cam != null) _eyeHeight = Mathf.Clamp(cam.transform.position.y - local.transform.position.y, 0.3f, 2.5f);
        }

        Seen.Clear();
        int localId = local.GetInstanceID();
        foreach (var p in list)
        {
            if (p == null) continue;
            int id = p.GetInstanceID();
            if (id == localId) continue;
            Seen.Add(id);

            if (Tracked.TryGetValue(id, out var h) && h.Alive)
            {
                if (h.OnCompass != compass || h.OnOverlay != overlay) { h.Remove(); Tracked.Remove(id); continue; } // re-created next frame
                h.FollowOffset = new Vector3(0f, _eyeHeight, 0f);
                if (refresh)
                {
                    string name = p.PlayerName;
                    if (!string.IsNullOrEmpty(name)) { h.Label = name; h.Tint = PlayerColors.ColorFor(name); }
                }
            }
            else
            {
                string name = p.PlayerName ?? "Player";
                Tracked[id] = CompassApi.AddTrackedMarker(p.transform, PlayerColors.ColorFor(name), name, MarkerIcon.Person,
                    compass, overlay, new Vector3(0f, _eyeHeight, 0f), overlayLiftFt: 1.2f);
            }
        }

        // Players who left.
        var gone = new System.Collections.Generic.List<int>();
        foreach (var kv in Tracked) if (!Seen.Contains(kv.Key) || !kv.Value.Alive) gone.Add(kv.Key);
        foreach (int id in gone)
        {
            if (Tracked.TryGetValue(id, out var h)) h.Remove();
            Tracked.Remove(id);
        }
    }

    private static void RemoveAll()
    {
        if (Tracked.Count == 0) return;
        foreach (var h in Tracked.Values) h.Remove();
        Tracked.Clear();
    }
}
