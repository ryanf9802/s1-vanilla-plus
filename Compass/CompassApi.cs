using Il2CppScheduleOne.UI.Compass;
using UnityEngine;
using UnityEngine.UI;

namespace Compass;

/// <summary>The icon a marker is drawn with.</summary>
public enum MarkerIcon
{
    /// <summary>A map pin with a dot: something at a place (pings).</summary>
    Pin,
    /// <summary>A disc with a person silhouette: a player.</summary>
    Person,
    /// <summary>A disc with a small sack: a dealer.</summary>
    Dealer,
}

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
    internal Sprite Portrait;        // optional picture shown inside a round frame instead of a glyph
    internal Transform CustomTemplate; // per-marker icon built for a Portrait
    internal MarkerIcon Icon;
    internal Transform Follow;      // when set, the marker tracks this transform (plus FollowOffset) and never expires on its own
    internal Vector3 FollowOffset;
    internal bool FadesWithDistance; // normal distance fade/hiding like built-in markers instead of always-visible
    internal float LiftFt;           // overlay icon height above the anchor, for FadesWithDistance markers
    /// <summary>Display name; can be updated while the marker lives.</summary>
    public string Label { get => Name; set => Name = value; }
    /// <summary>Tint color; can be updated while the marker lives.</summary>
    public Color Tint { get => Color; set => Color = value; }

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

    /// <summary>
    /// Add a marker that follows <paramref name="target"/> (at <paramref name="offset"/> from it) until you call
    /// <see cref="MarkerHandle.Remove"/> or the target is destroyed. Unlike <see cref="AddMarker"/> it fades with distance like
    /// built-in markers and is never hidden by range on the compass.
    /// </summary>
    public static MarkerHandle AddTrackedMarker(Transform target, Color color, string name, MarkerIcon icon = MarkerIcon.Person,
        bool onCompass = true, bool onOverlay = true, Vector3 offset = default, float overlayLiftFt = 1f, Sprite portrait = null)
    {
        var go = new GameObject("CompassTracked:" + name);
        go.transform.position = target.position + offset;
        var h = new MarkerHandle
        {
            Go = go, Color = color, Name = name, OnCompass = onCompass, OnOverlay = onOverlay,
            Expire = float.PositiveInfinity, FadeOut = 1f, Icon = icon,
            Follow = target, FollowOffset = offset, FadesWithDistance = true, LiftFt = overlayLiftFt, Portrait = portrait,
        };
        Markers.Add(h);
        ByTarget[go.transform.GetInstanceID()] = h;
        return h;
    }

    /// <summary>True for markers that fade with distance like built-in ones (tracked markers) rather than staying fully visible.</summary>
    internal static bool FadesWithDistance(Transform target)
        => ByTarget.TryGetValue(target.GetInstanceID(), out var h) && h.FadesWithDistance;

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
            if (h.Go == null || Time.time >= h.Expire || (h.Follow == null && h.FadesWithDistance)) { Destroy(cm, h); Markers.RemoveAt(i); continue; }
            if (h.Follow != null) h.Go.transform.position = h.Follow.position + h.FollowOffset;

            Transform template;
            if (h.Portrait != null)
            {
                if (h.CustomTemplate == null) h.CustomTemplate = BuildTemplate(h.Icon, h.Portrait);
                template = h.CustomTemplate;
            }
            else template = GetTemplate(h.Icon);
            if (template == null) continue;

            if (h.OnCompass && h.Element == null)
            {
                h.Element = cm.AddElement(h.Go.transform, template.GetComponent<RectTransform>(), true);
            }
            if (h.Element != null) TintIcon(h); // every frame: the element's rect may not exist yet on the frame it is added
            if (h.OnOverlay)
                Overlay.Submit(h.Go.transform.position, template, h.Name, h.Color, LifetimeAlpha(h.Go.transform),
                    alwaysVisible: !h.FadesWithDistance, key: h.Go.transform.GetInstanceID(), liftFt: h.FadesWithDistance ? h.LiftFt : (float?)null);
        }
    }

    private static GameObject _templateHolder;

    /// <summary>Drop everything tied to the previous world: markers, cached templates and generated sprites.</summary>
    internal static void Reset()
    {
        foreach (var h in Markers)
        {
            h.Alive = false;
            try { if (h.Go != null) UnityEngine.Object.Destroy(h.Go); } catch { }
        }
        Markers.Clear();
        ByTarget.Clear();
        try { if (_templateHolder != null) UnityEngine.Object.Destroy(_templateHolder); } catch { }
        _templateHolder = null;
        _templates.Clear();
        _pinSprite = _dotSprite = _discSprite = _personSprite = _bagSprite = null;
    }

    private static readonly System.Collections.Generic.Dictionary<MarkerIcon, Transform> _templates = new();
    private static Sprite _pinSprite, _dotSprite, _discSprite, _personSprite, _bagSprite;

    private static string TintName(MarkerIcon icon) => icon switch { MarkerIcon.Person => "PlayerDisc", MarkerIcon.Dealer => "DealerDisc", _ => "PingPin" };

    private static Transform GetTemplate(MarkerIcon icon)
    {
        if (!_templates.TryGetValue(icon, out var t) || t == null)
        {
            t = BuildTemplate(icon, null);
            _templates[icon] = t;
        }
        return t;
    }

    /// <summary>
    /// The icon API markers use, built from scratch (no dependence on any vanilla icon existing in the world): an empty icon
    /// object of the compass's element size holding our own colored shape and a white glyph. The shape is tinted per marker.
    /// </summary>
    private static Transform BuildTemplate(MarkerIcon icon, Sprite portrait)
    {
        Vector2 size = new(40f, 40f);
        try
        {
            var cm = CompassManager.Instance;
            if (cm != null && cm.ElementContentSize.x > 1f) size = cm.ElementContentSize;
        }
        catch { /* keep the default size */ }

        if (_templateHolder == null)
        {
            _templateHolder = new GameObject("CompassMarkerTemplates");
            _templateHolder.SetActive(false); // keeps the prefab itself off screen; clones copy its own active state
        }
        var go = new GameObject(icon + "Icon");
        var root = go.AddComponent<RectTransform>();
        root.SetParent(_templateHolder.transform, false);
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = size;
        go.SetActive(true);
        MelonLoader.MelonLogger.Msg($"[Compass] built {icon} marker template ({size.x:0}x{size.y:0})");

        bool person = icon != MarkerIcon.Pin; // person and dealer icons are a disc with a glyph; the ping is a pin with a dot
        var shape = new GameObject(TintName(icon));
        var sr = shape.AddComponent<RectTransform>();
        sr.SetParent(go.transform, false);
        float grow = person ? 0.05f : 0.15f; // slightly larger than the vanilla disc, same center
        sr.anchorMin = new Vector2(-grow, -grow);
        sr.anchorMax = new Vector2(1f + grow, 1f + grow);
        sr.offsetMin = sr.offsetMax = Vector2.zero;
        var shapeImg = shape.AddComponent<Image>();
        shapeImg.sprite = person ? GetDiscSprite() : GetPinSprite();
        shapeImg.preserveAspect = true;
        shapeImg.raycastTarget = false;

        if (portrait != null)
        {
            // Profile-picture style: the picture clipped to a circle inside the colored ring.
            var maskGo = new GameObject("PortraitMask");
            var mr = maskGo.AddComponent<RectTransform>();
            mr.SetParent(sr, false);
            mr.anchorMin = new Vector2(0.09f, 0.09f); mr.anchorMax = new Vector2(0.91f, 0.91f);
            mr.offsetMin = mr.offsetMax = Vector2.zero;
            var maskImg = maskGo.AddComponent<Image>();
            maskImg.sprite = GetDiscSprite();
            maskImg.raycastTarget = false;
            var mask = maskGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            var picGo = new GameObject("Portrait");
            var pr = picGo.AddComponent<RectTransform>();
            pr.SetParent(mr, false);
            pr.anchorMin = Vector2.zero; pr.anchorMax = Vector2.one;
            pr.offsetMin = pr.offsetMax = Vector2.zero;
            var pic = picGo.AddComponent<Image>();
            pic.sprite = portrait;
            pic.raycastTarget = false;
            return go.transform;
        }

        var glyph = new GameObject(person ? icon + "Glyph" : "PingDot");
        var gr = glyph.AddComponent<RectTransform>();
        gr.SetParent(sr, false);
        if (person)
        {
            gr.anchorMin = Vector2.zero; gr.anchorMax = Vector2.one;
            gr.offsetMin = gr.offsetMax = Vector2.zero;
        }
        else
        {
            gr.anchorMin = gr.anchorMax = new Vector2(0.5f, PinCenterY);
            gr.pivot = new Vector2(0.5f, 0.5f);
            gr.anchoredPosition = Vector2.zero;
            gr.sizeDelta = new Vector2(PinSize * PinDotRadius * 2f, PinSize * PinDotRadius * 2f);
        }
        var glyphImg = glyph.AddComponent<Image>();
        glyphImg.sprite = icon == MarkerIcon.Person ? GetPersonSprite() : icon == MarkerIcon.Dealer ? GetBagSprite() : GetDotSprite();
        glyphImg.raycastTarget = false;
        gr.localScale = Vector3.one;
        return go.transform;
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

    private static Sprite GetDiscSprite()
    {
        if (_discSprite != null) return _discSprite;
        const int n = 64;
        var tex = NewTexture(n);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n * 0.5f, n * 0.5f));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(n * 0.5f - d)));
            }
        tex.Apply();
        return _discSprite = MakeSprite(tex);
    }

    /// <summary>A white head-and-shoulders silhouette that sits inside the disc.</summary>
    private static Sprite GetPersonSprite()
    {
        if (_personSprite != null) return _personSprite;
        const int n = 64, ss = 3;
        var tex = NewTexture(n);
        var head = new Vector2(0.5f, 0.63f);
        const float headR = 0.15f;
        var body = new Vector2(0.5f, 0.24f);
        const float bodyRx = 0.27f, bodyRy = 0.25f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                int hits = 0;
                for (int sy = 0; sy < ss; sy++)
                    for (int sx = 0; sx < ss; sx++)
                    {
                        var q = new Vector2((x + (sx + 0.5f) / ss) / n, (y + (sy + 0.5f) / ss) / n);
                        bool inHead = (q - head).magnitude <= headR;
                        float ex = (q.x - body.x) / bodyRx, ey = (q.y - body.y) / bodyRy;
                        bool inBody = q.y >= body.y && ex * ex + ey * ey <= 1f; // upper half of an ellipse: the shoulders
                        if (inHead || inBody) hits++;
                    }
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, hits / (float)(ss * ss)));
            }
        tex.Apply();
        return _personSprite = MakeSprite(tex);
    }

    /// <summary>A white sack (body, neck and flared top) that sits inside the disc.</summary>
    private static Sprite GetBagSprite()
    {
        if (_bagSprite != null) return _bagSprite;
        const int n = 64, ss = 3;
        var tex = NewTexture(n);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                int hits = 0;
                for (int sy = 0; sy < ss; sy++)
                    for (int sx = 0; sx < ss; sx++)
                    {
                        float qx = (x + (sx + 0.5f) / ss) / n, qy = (y + (sy + 0.5f) / ss) / n;
                        // body: a rounded block; neck: narrow; top: a small flare
                        float cx = Mathf.Abs(qx - 0.5f);
                        bool body = cx <= 0.21f && qy >= 0.24f && qy <= 0.58f
                                    && !(cx > 0.17f && (qy < 0.28f || qy > 0.54f)); // rounded corners
                        bool neck = cx <= 0.07f && qy > 0.58f && qy <= 0.68f;
                        bool top = cx <= 0.16f && qy > 0.68f && qy <= 0.76f;
                        if (body || neck || top) hits++;
                    }
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, hits / (float)(ss * ss)));
            }
        tex.Apply();
        return _bagSprite = MakeSprite(tex);
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
                if (img.gameObject.name == TintName(h.Icon)) { h.Pin = img; break; }
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
