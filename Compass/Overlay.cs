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

    private struct Item { public Vector3 Pos; public Sprite Sprite; public Color Color; }

    private static readonly System.Collections.Generic.List<Item> Items = new();
    private static readonly System.Collections.Generic.List<Image> Pool = new();
    private static RectTransform _root;
    private static Canvas _canvas;

    public static int LastDrawn;

    /// <summary>Distance-based opacity shared by every overlay icon: 0 beyond MaxFt, 1 from FadeStartFt out, ramping to 0 at FadeEndFt.</summary>
    public static float AlphaForDistanceFt(float distFt)
    {
        if (distFt > Settings.OverlayMaxFt) return 0f;
        return Mathf.Clamp01(Mathf.InverseLerp(Settings.OverlayFadeEndFt, Settings.OverlayFadeStartFt, distFt));
    }

    public static void BeginFrame() => Items.Clear();

    /// <summary>Queue an icon above <paramref name="worldPos"/> for this frame. Distance fade is applied by the overlay.</summary>
    public static void Submit(Vector3 worldPos, Sprite sprite, Color color)
    {
        if (sprite != null) Items.Add(new Item { Pos = worldPos, Sprite = sprite, Color = color });
    }

    public static void Render(Canvas hudCanvas, Camera cam)
    {
        if (hudCanvas == null || cam == null) { Hide(0); return; }
        EnsureRoot(hudCanvas);
        Vector3 feet = cam.transform.position - new Vector3(0f, EyeHeight, 0f);
        Vector3 lift = new(0f, Settings.OverlayHeightFt / FeetPerUnit, 0f);
        Camera uiCam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;

        int used = 0;
        foreach (var item in Items)
        {
            float alpha = AlphaForDistanceFt(Vector3.Distance(feet, item.Pos) * FeetPerUnit);
            if (alpha <= 0.01f) continue;
            Vector3 sp = cam.WorldToScreenPoint(item.Pos + lift);
            if (sp.z <= 0f || sp.x < 0f || sp.y < 0f || sp.x > Screen.width || sp.y > Screen.height) continue;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, new Vector2(sp.x, sp.y), uiCam, out Vector2 local)) continue;

            var img = GetIcon(used++);
            img.sprite = item.Sprite;
            img.color = new Color(item.Color.r, item.Color.g, item.Color.b, item.Color.a * alpha);
            var rt = img.rectTransform;
            rt.sizeDelta = new Vector2(Settings.OverlaySize, Settings.OverlaySize);
            rt.localPosition = new Vector3(local.x, local.y, 0f);
        }
        Hide(used);
        LastDrawn = used;
    }

    private static void Hide(int from)
    {
        for (int i = from; i < Pool.Count; i++)
            if (Pool[i] != null && Pool[i].gameObject.activeSelf) Pool[i].gameObject.SetActive(false);
    }

    private static Image GetIcon(int i)
    {
        while (Pool.Count <= i)
        {
            var go = new GameObject("OverlayIcon");
            go.transform.SetParent(_root, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;
            Pool.Add(img);
        }
        var icon = Pool[i];
        if (!icon.gameObject.activeSelf) icon.gameObject.SetActive(true);
        return icon;
    }

    private static void EnsureRoot(Canvas hudCanvas)
    {
        if (_root != null && _canvas == hudCanvas) return;
        _canvas = hudCanvas;
        Pool.Clear();
        var go = new GameObject("CompassOverlay");
        go.transform.SetParent(hudCanvas.transform, false);
        _root = go.AddComponent<RectTransform>();
        _root.anchorMin = Vector2.zero;
        _root.anchorMax = Vector2.one;
        _root.offsetMin = _root.offsetMax = Vector2.zero;
        MelonLoader.MelonLogger.Msg($"[Compass] overlay root created under canvas '{hudCanvas.name}' mode={hudCanvas.renderMode}");
    }
}
