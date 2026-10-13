using Il2CppScheduleOne.UI.Compass;
using UnityEngine;
using UnityEngine.UI;

namespace Compass;

/// <summary>
/// Per-frame tweaks on top of the vanilla compass elements:
///  - markers fade with distance (on child CanvasGroups, so vanilla's own angular fade is untouched),
///  - the distance label only shows while you face the marker, at any range (vanilla shows it within ~165ft always),
///  - a small chevron at the marker's top-right shows when the target is well above or below you.
/// Driven from MelonMod.OnLateUpdate: CompassManager.UpdateElement/LateUpdate hooks never fire (native-to-native calls).
/// </summary>
public static class CompassEnhancer
{
    private const float FeetPerUnit = 3.28084f; // vanilla labels are in feet

    private class Entry
    {
        public RectTransform Rect;
        public Image Chevron;
        public RectTransform ChevronRect;
        public readonly System.Collections.Generic.List<CanvasGroup> Groups = new();
    }

    private static readonly System.Collections.Generic.Dictionary<int, Entry> Entries = new();
    private static Sprite _chevronSprite;
    private static bool _errorLogged;
    public static int Frames, Applied, Failed;

    public static void Tick()
    {
        Frames++;
        var cm = CompassManager.Instance;
        var cam = Camera.main;
        if (cm == null || cm.elements == null || cam == null) return;
        var camT = cam.transform;

        // Pass 1: among markers you are facing, pick the one that gets a label (closest wins).
        int winner = PickLabelWinner(camT, cm);
        // Pass 2: apply visuals.
        foreach (var el in cm.elements)
        {
            try { Apply(camT, el, winner); }
            catch (System.Exception e)
            {
                Failed++;
                if (_errorLogged) continue;
                _errorLogged = true;
                MelonLoader.MelonLogger.Error($"[Compass] CompassEnhancer failed (logged once): {e}");
            }
        }
    }

    private static int _lastWinner = -1;
    private const float WinnerStickiness = 1.15f; // keep the previous winner unless a rival is >15% closer

    private static bool IsFacing(Transform camT, Vector3 toTarget)
    {
        Vector3 fwd = camT.forward; fwd.y = 0f;
        Vector3 flat = toTarget; flat.y = 0f;
        return fwd.sqrMagnitude > 1e-4f && flat.sqrMagnitude > 1e-4f
               && Vector3.Angle(fwd, flat) <= Settings.FacingAngle;
    }

    private static int PickLabelWinner(Transform camT, CompassManager cm)
    {
        int best = -1, previous = -1;
        float bestDist = float.MaxValue, previousDist = float.MaxValue;
        foreach (var el in cm.elements)
        {
            var target = el?.TargetTransform;
            var rect = el?.Rect;
            if (target == null || rect == null || !el.Visible) continue;
            Vector3 toTarget = target.position - camT.position;
            if (!IsFacing(camT, toTarget)) continue;
            int id = rect.GetInstanceID();
            float d = toTarget.magnitude;
            if (d < bestDist) { bestDist = d; best = id; }
            if (id == _lastWinner) { previous = id; previousDist = d; }
        }
        if (previous != -1 && previousDist <= bestDist * WinnerStickiness) best = previous;
        _lastWinner = best;
        return best;
    }

    private static void Apply(Transform camT, CompassManager.Element el, int labelWinner)
    {
        var target = el?.TargetTransform;
        var rect = el?.Rect;
        if (target == null || rect == null || !el.Visible) return;
        Applied++;

        var entry = GetEntry(rect);
        Vector3 toTarget = target.position - camT.position;
        float dist = toTarget.magnitude;

        // Distance fade on our own layer.
        float fade = Mathf.Lerp(1f, Settings.MinAlpha, Mathf.InverseLerp(Settings.FadeStart, Settings.FadeEnd, dist));
        foreach (var g in entry.Groups) if (g != null) g.alpha = fade;

        // Label: only on the closest marker you are facing (horizontal angle, so looking up/down still counts).
        var label = el.DistanceLabel;
        if (label != null)
        {
            bool facing = rect.GetInstanceID() == labelWinner;
            if (facing)
            {
                // Vanilla blanks the text beyond its threshold; fill it in ourselves.
                if (string.IsNullOrEmpty(label.text))
                    label.text = $"{Mathf.RoundToInt(dist * FeetPerUnit)}ft";
            }
            label.alpha = Mathf.MoveTowards(label.alpha, facing ? 1f : 0f, Time.deltaTime * 8f);
        }

        // Elevation chevron.
        if (entry.Chevron != null)
        {
            float dy = toTarget.y;
            bool show = Mathf.Abs(dy) >= Settings.ChevronThresholdFt / FeetPerUnit;
            if (entry.Chevron.enabled != show) entry.Chevron.enabled = show;
            if (show) entry.ChevronRect.localRotation = Quaternion.Euler(0f, 0f, dy > 0f ? 0f : 180f);
        }
    }

    private static Entry GetEntry(RectTransform rect)
    {
        int id = rect.GetInstanceID();
        if (Entries.TryGetValue(id, out var existing) && existing.Rect != null) return existing;

        var entry = new Entry { Rect = rect };

        var go = new GameObject("CompassChevron");
        go.transform.SetParent(rect, false);
        var cr = go.AddComponent<RectTransform>();
        cr.anchorMin = cr.anchorMax = new Vector2(1f, 1f);
        cr.pivot = new Vector2(0.5f, 0.5f);
        cr.anchoredPosition = Vector2.zero;
        cr.sizeDelta = new Vector2(Settings.ChevronSize, Settings.ChevronSize);
        var img = go.AddComponent<Image>();
        img.sprite = GetChevronSprite();
        img.color = new Color(1f, 1f, 1f, 0.9f);
        img.raycastTarget = false;
        img.enabled = false;
        entry.Chevron = img;
        entry.ChevronRect = cr;

        // Our fade goes on child groups; the element's own group belongs to vanilla.
        var names = new System.Text.StringBuilder();
        for (int i = 0; i < rect.childCount; i++)
        {
            var child = rect.GetChild(i);
            var g = child.GetComponent<CanvasGroup>();
            if (g == null) g = child.gameObject.AddComponent<CanvasGroup>();
            g.interactable = false;
            g.blocksRaycasts = false;
            entry.Groups.Add(g);
            names.Append(child.name).Append(' ');
        }
        if (Entries.Count < 3)
            MelonLoader.MelonLogger.Msg($"[Compass] element '{rect.name}' children={rect.childCount}: {names}");

        Entries[id] = entry;
        return entry;
    }

    /// <summary>A white, anti-aliased upward chevron drawn at runtime, so we don't depend on font glyphs.</summary>
    private static Sprite GetChevronSprite()
    {
        if (_chevronSprite != null) return _chevronSprite;
        const int n = 32;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.hideFlags = HideFlags.HideAndDontSave;
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        Vector2 a = new(5f, 9f), b = new(16f, 22f), c = new(27f, 9f);
        const float half = 2.4f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float d = Mathf.Min(SegDist(p, a, b), SegDist(p, b, c));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(half + 0.5f - d)));
            }
        tex.Apply();
        _chevronSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        _chevronSprite.hideFlags = HideFlags.HideAndDontSave;
        return _chevronSprite;
    }

    private static float SegDist(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return (p - (a + ab * t)).magnitude;
    }
}
