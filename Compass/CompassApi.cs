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
    internal Image Pin;

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

            var template = GetPingTemplate();
            if (template == null) continue; // no vanilla icon to build from yet; try again next frame

            if (h.OnCompass && h.Element == null)
            {
                h.Element = cm.AddElement(h.Go.transform, template.GetComponent<RectTransform>(), true);
            }
            if (h.Element != null) TintIcon(h); // every frame: the element's rect may not exist yet on the frame it is added
            if (h.OnOverlay)
                Overlay.Submit(h.Go.transform.position, template, h.Name, h.Color, LifetimeAlpha(h.Go.transform), alwaysVisible: true, key: h.Go.transform.GetInstanceID());
        }
    }

    private static Transform _pingTemplate;
    private static Sprite _pinSprite, _dotSprite;

    /// <summary>
    /// The icon API markers use: a copy of the vanilla marker object (keeps its distance label and layout) whose circle and
    /// glyph are replaced by a white map-pin with a dot. The pin is tinted per marker; the dot stays white.
    /// </summary>
    private static Transform GetPingTemplate()
    {
        if (_pingTemplate != null) return _pingTemplate;
        var src = CompassEnhancer.IconTemplate;
        if (src == null) return null;

        var holder = new GameObject("CompassPingTemplates");
        holder.SetActive(false); // keeps the prefab itself off screen; clones copy its own active state
        var go = UnityEngine.Object.Instantiate(src.gameObject, holder.transform, false);
        go.name = "PingIcon";
        go.SetActive(true);
        var rootImg = go.GetComponent<Image>();
        if (rootImg != null) rootImg.enabled = false;
        for (int i = go.transform.childCount - 1; i >= 0; i--)
        {
            var child = go.transform.GetChild(i);
            if (child.name != "Text") UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        var pin = new GameObject("PingPin");
        var pr = pin.AddComponent<RectTransform>();
        pr.SetParent(go.transform, false);
        pr.anchorMin = new Vector2(-0.15f, -0.15f); // slightly larger than the vanilla disc, same center
        pr.anchorMax = new Vector2(1.15f, 1.15f);
        pr.offsetMin = pr.offsetMax = Vector2.zero;
        var pinImg = pin.AddComponent<Image>();
        pinImg.sprite = GetPinSprite();
        pinImg.preserveAspect = true;
        pinImg.raycastTarget = false;

        var dot = new GameObject("PingDot");
        var dr = dot.AddComponent<RectTransform>();
        dr.SetParent(pr, false);
        dr.anchorMin = dr.anchorMax = new Vector2(0.5f, PinCenterY);
        dr.pivot = new Vector2(0.5f, 0.5f);
        dr.anchoredPosition = Vector2.zero;
        dr.sizeDelta = new Vector2(PinSize * PinDotRadius * 2f, PinSize * PinDotRadius * 2f);
        var dotImg = dot.AddComponent<Image>();
        dotImg.sprite = GetDotSprite();
        dotImg.raycastTarget = false;
        dr.localScale = Vector3.one;

        _pingTemplate = go.transform;
        return _pingTemplate;
    }

    // Pin geometry in a unit square (y up): a circle on top of a point, like a map pin.
    private const float PinCenterY = 0.64f, PinRadius = 0.30f, PinTipY = 0.06f, PinDotRadius = 0.12f;
    private const float PinSize = 22f; // reference size the dot is laid out for; the pin itself scales with its parent

    private static Sprite GetPinSprite()
    {
        if (_pinSprite != null) return _pinSprite;
        const int n = 64, ss = 3;
        var tex = NewTexture(n);
        var c = new Vector2(0.5f, PinCenterY);
        var tip = new Vector2(0.5f, PinTipY);
        float theta = Mathf.Acos(PinRadius / (c.y - tip.y));
        var p1 = c + PinRadius * new Vector2(Mathf.Sin(theta), -Mathf.Cos(theta));
        var p2 = c + PinRadius * new Vector2(-Mathf.Sin(theta), -Mathf.Cos(theta));
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                int hits = 0;
                for (int sy = 0; sy < ss; sy++)
                    for (int sx = 0; sx < ss; sx++)
                    {
                        var q = new Vector2((x + (sx + 0.5f) / ss) / n, (y + (sy + 0.5f) / ss) / n);
                        if ((q - c).magnitude <= PinRadius || InTriangle(q, tip, p1, p2)) hits++;
                    }
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, hits / (float)(ss * ss)));
            }
        tex.Apply();
        return _pinSprite = MakeSprite(tex);
    }

    private static Sprite GetDotSprite()
    {
        if (_dotSprite != null) return _dotSprite;
        const int n = 32;
        var tex = NewTexture(n);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n * 0.5f, n * 0.5f));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(n * 0.5f - d)));
            }
        tex.Apply();
        return _dotSprite = MakeSprite(tex);
    }

    private static Texture2D NewTexture(int n)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.hideFlags = HideFlags.HideAndDontSave;
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        return tex;
    }

    private static Sprite MakeSprite(Texture2D tex)
    {
        var sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        sp.hideFlags = HideFlags.HideAndDontSave;
        return sp;
    }

    private static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Cross(p, a, b), d2 = Cross(p, b, c), d3 = Cross(p, c, a);
        bool neg = d1 < 0f || d2 < 0f || d3 < 0f, pos = d1 > 0f || d2 > 0f || d3 > 0f;
        return !(neg && pos);
    }

    private static float Cross(Vector2 p, Vector2 a, Vector2 b) => (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);

    private static void TintIcon(MarkerHandle h)
    {
        if (h.Pin == null)
        {
            var rect = h.Element?.Rect;
            if (rect == null) return;
            foreach (var img in rect.GetComponentsInChildren<Image>(true))
                if (img.gameObject.name == "PingPin") { h.Pin = img; break; }
        }
        if (h.Pin != null) h.Pin.color = h.Color; // the dot stays white
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
