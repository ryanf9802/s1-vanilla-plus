using Il2CppScheduleOne.UI.Compass;
using UnityEngine;
using UnityEngine.UI;

namespace Compass;

/// <summary>Handle to a marker added through <see cref="CompassApi.AddMarker"/>.</summary>
public sealed class MarkerHandle
{
    internal GameObject Go;
    internal Color Color;
    internal string Name;
    internal float Expire, FadeOut;
    internal bool OnCompass, OnOverlay;
    internal CompassManager.Element Element;

    /// <summary>True until the marker expires or is removed.</summary>
    public bool Alive { get; internal set; } = true;

    /// <summary>Remove the marker now.</summary>
    public void Remove() => Expire = 0f;
}

/// <summary>
/// Public API for other mods (Ping) to put timed, colored markers on the compass strip and as through-wall overlay
/// icons. The markers use the same fades, labels and ranges as built-in ones.
/// </summary>
public static class CompassApi
{
    private static readonly System.Collections.Generic.List<MarkerHandle> Markers = new();
    private static readonly System.Collections.Generic.Dictionary<int, MarkerHandle> ByTarget = new();

    /// <summary>
    /// Add a marker at <paramref name="worldPos"/> that lives for <paramref name="lifetimeSeconds"/> (it fades out over the last
    /// <paramref name="fadeOutSeconds"/>). Call from the main thread.
    /// </summary>
    public static MarkerHandle AddMarker(Vector3 worldPos, Color color, float lifetimeSeconds, string name,
        bool onCompass = true, bool onOverlay = true, float fadeOutSeconds = 0.6f)
    {
        var go = new GameObject("CompassMarker:" + name);
        go.transform.position = worldPos;
        var h = new MarkerHandle
        {
            Go = go, Color = color, Name = name, OnCompass = onCompass, OnOverlay = onOverlay,
            Expire = Time.time + lifetimeSeconds, FadeOut = Mathf.Max(0.01f, fadeOutSeconds),
        };
        Markers.Add(h);
        ByTarget[go.transform.GetInstanceID()] = h;
        return h;
    }

    /// <summary>True if <paramref name="target"/> is the anchor of a marker created through this API.</summary>
    internal static bool IsMarkerTarget(Transform target) => ByTarget.ContainsKey(target.GetInstanceID());

    /// <summary>1 for normal markers; for API markers, fades 1 to 0 over the final FadeOut seconds.</summary>
    internal static float LifetimeAlpha(Transform target)
        => ByTarget.TryGetValue(target.GetInstanceID(), out var h) ? Mathf.Clamp01((h.Expire - Time.time) / h.FadeOut) : 1f;

    internal static void Tick(CompassManager cm)
    {
        for (int i = Markers.Count - 1; i >= 0; i--)
        {
            var h = Markers[i];
            if (h.Go == null || Time.time >= h.Expire) { Destroy(cm, h); Markers.RemoveAt(i); continue; }

            var template = CompassEnhancer.IconTemplate;
            if (template == null) continue; // no vanilla icon to copy yet; try again next frame

            if (h.OnCompass && h.Element == null)
            {
                h.Element = cm.AddElement(h.Go.transform, template.GetComponent<RectTransform>(), true);
                TintIcon(h);
            }
            if (h.OnOverlay)
                Overlay.Submit(h.Go.transform.position, template, h.Name, h.Color, LifetimeAlpha(h.Go.transform));
        }
    }

    private static void TintIcon(MarkerHandle h)
    {
        var rect = h.Element?.Rect;
        if (rect == null) return;
        for (int c = 0; c < rect.childCount; c++)
        {
            var child = rect.GetChild(c);
            if (child.name == "Text" || child.name == "CompassChevron") continue;
            var img = child.GetComponent<Image>();
            if (img != null) img.color = h.Color; // the colored disc; glyph children keep their own colors
        }
    }

    private static void Destroy(CompassManager cm, MarkerHandle h)
    {
        h.Alive = false;
        if (h.Go != null)
        {
            ByTarget.Remove(h.Go.transform.GetInstanceID());
            if (h.Element != null && cm != null) cm.RemoveElement(h.Go.transform, true);
            UnityEngine.Object.Destroy(h.Go);
        }
    }
}
