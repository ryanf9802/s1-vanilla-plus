using Il2CppScheduleOne.PlayerScripts;
using MelonLoader;
using UnityEngine;

namespace Portraits;

/// <summary>Watches every player: subscribes to their avatar's change events and re-renders their portrait (debounced).</summary>
internal static class PortraitTracker
{
    private class Tracked
    {
        public Player Player;
        public string Name;
        public float RenderAt = -1f;     // when a debounced render is due (-1 = none)
        public string Reason;
        public string Signature;
        public bool Subscribed;
        public int Renders;
    }

    private const float Debounce = 0.6f;
    private const float SignatureEvery = 3f;

    private static readonly System.Collections.Generic.Dictionary<int, Tracked> Players = new();
    private static float _nextScan;
    private static float _nextSignature;

    public static void Update(MelonLogger.Instance log)
    {
        float now = Time.time;
        if (now >= _nextScan)
        {
            _nextScan = now + 1f;
            Scan(log);
        }

        bool doSignature = now >= _nextSignature;
        if (doSignature) _nextSignature = now + SignatureEvery;

        foreach (var t in Players.Values)
        {
            if (t.Player == null || t.Player.Avatar == null) continue;
            if (doSignature)
            {
                string sig = PortraitRenderer.Signature(t.Player);
                if (t.Signature != null && sig != t.Signature) Schedule(t, "signature changed");
                t.Signature = sig;
            }
            if (t.RenderAt >= 0f && now >= t.RenderAt)
            {
                t.RenderAt = -1f;
                string reason = t.Reason;
                try
                {
                    string path = PortraitRenderer.Render(t.Player, $"{Sanitize(t.Name)}_{++t.Renders:D2}", log);
                    log.Msg($"[portrait] {t.Name}: rendered ({reason}) -> {path}");
                }
                catch (System.Exception e) { log.Error($"[portrait] {t.Name}: render failed: {e}"); }
            }
        }
    }

    private static void Scan(MelonLogger.Instance log)
    {
        var list = Player.PlayerList;
        if (list == null) return;
        foreach (var p in list)
        {
            if (p == null || p.Avatar == null || p.Avatar.Appearance == null) continue;
            int id = p.GetInstanceID();
            if (Players.TryGetValue(id, out var t) && t.Subscribed) { t.Name = p.PlayerName ?? t.Name; continue; }

            t = new Tracked { Player = p, Name = string.IsNullOrEmpty(p.PlayerName) ? "player" : p.PlayerName };
            Players[id] = t;
            Subscribe(t, log);
            Schedule(t, "first sight");
            log.Msg($"[portrait] tracking '{t.Name}' (local={p == Player.Local})");
        }
    }

    private static void Subscribe(Tracked t, MelonLogger.Instance log)
    {
        var app = t.Player.Avatar.Appearance;
        try
        {
            app.add_OnOutfitChanged((Il2CppSystem.Action<Il2CppSystem.Collections.Generic.List<Il2CppScheduleOne.Core.Avatar.SerializedAvatarObject>>)
                new System.Action<Il2CppSystem.Collections.Generic.List<Il2CppScheduleOne.Core.Avatar.SerializedAvatarObject>>(_ =>
                {
                    log.Msg($"[portrait] event OnOutfitChanged for '{t.Name}'");
                    Schedule(t, "outfit event");
                }));
            app.add_OnNakedAppearanceChanged((Il2CppSystem.Action<Il2CppScheduleOne.Core.Avatar.NakedAppearance>)
                new System.Action<Il2CppScheduleOne.Core.Avatar.NakedAppearance>(_ =>
                {
                    log.Msg($"[portrait] event OnNakedAppearanceChanged for '{t.Name}'");
                    Schedule(t, "naked appearance event");
                }));
            t.Subscribed = true;
        }
        catch (System.Exception e) { log.Error($"[portrait] could not subscribe for '{t.Name}': {e.Message}"); t.Subscribed = true; }
    }

    private static void Schedule(Tracked t, string reason)
    {
        t.RenderAt = Time.time + Debounce;
        t.Reason = reason;
    }

    private static string Sanitize(string n)
    {
        foreach (char c in System.IO.Path.GetInvalidFileNameChars()) n = n.Replace(c, '_');
        return n;
    }
}
