using UnityEngine;
using UnityEngine.UI;

namespace Compass;

/// <summary>
/// Immediate-mode HUD overlay: icons drawn over world positions, visible through walls (it is screen-space UI,
/// so nothing in the 3D scene can occlude it). Every frame, callers <see cref="Submit"/> what to draw;
/// <see cref="Render"/> places the icons. Compass submits quest objectives; the Ping mod will submit pings the same way.
/// The root is parented to the vanilla compass canvas, so it hides whenever the HUD hides.
/// </summary>
public static class Overlay
{
    private const float FeetPerUnit = 3.28084f;
    private const float EyeHeight = 1.7f; // measure distance from the player's feet, not the camera

    private struct Item { public Vector3 Pos; public Transform Source; public string Name; public Color? Tint; public float AlphaMul; public bool Always; public int Key; }

    // One live clone of a vanilla icon object (circle + glyph children) per drawn item.
    private class Slot { public GameObject Go; public RectTransform Rt; public CanvasGroup Group; public Image Disc; public int SourceId; }

    private static readonly System.Collections.Generic.List<Item> Items = new();
    private static readonly System.Collections.Generic.List<Slot> Pool = new();
    private static RectTransform _root;
    private static Canvas _canvas;
    private static Il2CppTMPro.TextMeshProUGUI _nameLabel;
    private static RectTransform _nameRect;
    private static float _nameBaseFontSize;
    private static int _nameKey;                                   // source id of the icon the name belongs to
    private static readonly System.Collections.Generic.Dictionary<int, Vector3> FrameIconPos = new();

    public static int LastDrawn;

    /// <summary>Distance-based opacity shared by every overlay icon: 0 beyond MaxFt, ramping up over the first FadeInFt after entering range,
    /// full until FadeStartFt, then ramping down to 0 at FadeEndFt.</summary>
    public static float AlphaForDistanceFt(float distFt)
    {
        float max = Settings.OverlayMaxFt;
        if (distFt > max) return 0f;
        float fadeIn = Settings.OverlayFadeInFt > 0f ? Mathf.Clamp01(Mathf.InverseLerp(max, max - Settings.OverlayFadeInFt, distFt)) : 1f;
        float fadeOut = Mathf.Clamp01(Mathf.InverseLerp(Settings.OverlayFadeEndFt, Settings.OverlayFadeStartFt, distFt));
        return fadeIn * fadeOut;
    }

    public static void BeginFrame() => Items.Clear();

    /// <summary>Queue a copy of the vanilla icon object <paramref name="iconSource"/> above <paramref name="worldPos"/> for this frame.
    /// <paramref name="key" /> identifies the marker for the name label when several share one icon source (pings).
    /// Distance fade and global opacity are applied by the overlay, unless <paramref name="alwaysVisible"/> (full opacity at any range).</summary>
    public static void Submit(Vector3 worldPos, Transform iconSource, string name = null, Color? tint = null, float alphaMul = 1f, bool alwaysVisible = false, int key = 0)
    {
        if (iconSource != null) Items.Add(new Item { Pos = worldPos, Source = iconSource, Name = name, Tint = tint, AlphaMul = alphaMul, Always = alwaysVisible, Key = key != 0 ? key : iconSource.GetInstanceID() });
    }

    public static void Render(Canvas hudCanvas, Camera cam, Il2CppTMPro.TMP_Text styleSource)
    {
        if (hudCanvas == null || cam == null) { Hide(0); return; }
        EnsureRoot(hudCanvas);
        EnsureNameLabel(styleSource);
        if (PhoneOpen())
        {
            // The overlay canvas draws above the phone UI, so step aside while the phone is up.
            FrameIconPos.Clear();
            UpdateNameLabel(null, 0);
            Hide(0);
            LastDrawn = 0;
            return;
        }
        Vector3 feet = cam.transform.position - new Vector3(0f, EyeHeight, 0f);
        Vector3 lift = new(0f, Settings.OverlayHeightFt / FeetPerUnit, 0f);
        Camera uiCam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;

        FrameIconPos.Clear();
        int used = 0;
        int lookKey = 0;
        string lookName = null;
        float bestCenterDist = Settings.OverlayLookRadius * Screen.height;
        var screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        foreach (var item in Items)
        {
            float alpha = item.Always ? 1f : AlphaForDistanceFt(Vector3.Distance(feet, item.Pos) * FeetPerUnit);
            if (alpha <= 0.01f) continue;
            Vector3 sp = cam.WorldToScreenPoint(item.Always ? item.Pos : item.Pos + lift);
            if (sp.z <= 0f || sp.x < 0f || sp.y < 0f || sp.x > Screen.width || sp.y > Screen.height) continue;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, new Vector2(sp.x, sp.y), uiCam, out Vector2 local)) continue;

            var slot = GetSlot(used++, item.Source);
            slot.Group.alpha = (item.Always ? 1f : alpha * Settings.OverlayOpacity) * item.AlphaMul;
            if (item.Tint.HasValue && slot.Disc != null) slot.Disc.color = item.Tint.Value;
            slot.Rt.sizeDelta = new Vector2(Settings.OverlaySize, Settings.OverlaySize);
            // Pings sit on the spot itself: lift the icon by the pin's height so its tip lands on the point.
            float tipLift = item.Always ? Settings.OverlaySize * 0.57f : 0f;
            slot.Rt.localPosition = new Vector3(local.x, local.y + tipLift, 0f);

            FrameIconPos[item.Key] = slot.Rt.localPosition;

            // The icon nearest screen center (within the look radius) is the one you are looking at.
            float cd = Vector2.Distance(new Vector2(sp.x, sp.y + tipLift * _canvas.scaleFactor), screenCenter); // from where the icon is drawn
            if (item.Name != null && alpha > 0.05f && cd < bestCenterDist)
            {
                bestCenterDist = cd;
                lookName = item.Name;
                lookKey = item.Key;
            }
        }
        UpdateNameLabel(lookName, lookKey);
        Hide(used);
        LastDrawn = used;
    }

    private static bool PhoneOpen()
    {
        try
        {
            var phone = Il2CppScheduleOne.UI.Phone.Phone.Instance;
            return phone != null && phone.IsOpen;
        }
        catch { return false; }
    }

    private static void UpdateNameLabel(string name, int key)
    {
        if (_nameLabel == null) return;
        float fontSize = _nameBaseFontSize * Settings.OverlayNameScale;
        if (!Mathf.Approximately(_nameLabel.fontSize, fontSize)) _nameLabel.fontSize = fontSize;
        bool looking = name != null && Settings.OverlayShowNames;
        if (looking)
        {
            _nameKey = key;
            if (_nameLabel.text != name) _nameLabel.text = name;
        }
        // Keep following the icon the name belongs to while it fades out; if that icon is no longer drawn, just fade.
        bool tracked = FrameIconPos.TryGetValue(_nameKey, out Vector3 iconPos);
        if (tracked)
            _nameRect.localPosition = iconPos + new Vector3(0f, -(Settings.OverlaySize * 0.5f + _nameLabel.fontSize * 0.7f), 0f);
        float goal = looking ? Settings.OverlayNameOpacity : 0f;
        float speed = tracked ? 6f : 20f;
        _nameLabel.alpha = Mathf.MoveTowards(_nameLabel.alpha, goal, Time.deltaTime * speed);
    }

    private static void EnsureNameLabel(Il2CppTMPro.TMP_Text src)
    {
        if (_nameLabel != null || src == null || _root == null) return;
        var go = new GameObject("OverlayName");
        go.transform.SetParent(_root, false);
        _nameRect = go.AddComponent<RectTransform>();
        _nameRect.anchorMin = _nameRect.anchorMax = _nameRect.pivot = new Vector2(0.5f, 0.5f);
        _nameRect.sizeDelta = new Vector2(500f, 40f);
        _nameLabel = go.AddComponent<Il2CppTMPro.TextMeshProUGUI>();
        _nameLabel.font = src.font;
        _nameLabel.fontSharedMaterial = src.fontSharedMaterial;
        _nameBaseFontSize = src.fontSize;
        _nameLabel.color = src.color;
        _nameLabel.alignment = Il2CppTMPro.TextAlignmentOptions.Center;
        _nameLabel.raycastTarget = false;
        _nameLabel.alpha = 0f;
        _nameLabel.text = "";
    }

    private static void Hide(int from)
    {
        for (int i = from; i < Pool.Count; i++)
            if (Pool[i].Go != null && Pool[i].Go.activeSelf) Pool[i].Go.SetActive(false);
    }

    private static Slot GetSlot(int i, Transform source)
    {
        while (Pool.Count <= i) Pool.Add(new Slot());
        var slot = Pool[i];
        int id = source.GetInstanceID();
        if (slot.Go == null || slot.SourceId != id)
        {
            if (slot.Go != null) UnityEngine.Object.Destroy(slot.Go);
            slot.Go = UnityEngine.Object.Instantiate(source.gameObject, _root);
            slot.Go.name = "OverlayIcon";
            slot.Go.SetActive(true);
            slot.Rt = slot.Go.GetComponent<RectTransform>();
            slot.Rt.anchorMin = slot.Rt.anchorMax = slot.Rt.pivot = new Vector2(0.5f, 0.5f);
            slot.Rt.localScale = Vector3.one;
            slot.Disc = null; // the colored shape: first image on a child other than the distance label
            for (int c = 0; c < slot.Rt.childCount && slot.Disc == null; c++)
            {
                var child = slot.Rt.GetChild(c);
                if (child.name != "Text") slot.Disc = child.GetComponent<Image>();
            }
            slot.Group = slot.Go.GetComponent<CanvasGroup>();
            if (slot.Group == null) slot.Group = slot.Go.AddComponent<CanvasGroup>();
            slot.Group.interactable = false;
            slot.Group.blocksRaycasts = false;
            slot.SourceId = id;
        }
        else if (!slot.Go.activeSelf) slot.Go.SetActive(true);
        return slot;
    }

    private static void EnsureRoot(Canvas hudCanvas)
    {
        if (_root != null && _canvas == hudCanvas) return;
        _canvas = hudCanvas;
        Pool.Clear();
        _nameLabel = null;
        var go = new GameObject("CompassOverlay");
        go.transform.SetParent(hudCanvas.transform, false);
        _root = go.AddComponent<RectTransform>();
        _root.anchorMin = Vector2.zero;
        _root.anchorMax = Vector2.one;
        _root.offsetMin = _root.offsetMax = Vector2.zero;
        MelonLoader.MelonLogger.Msg($"[Compass] overlay root created under canvas '{hudCanvas.name}' mode={hudCanvas.renderMode}");
    }
}
