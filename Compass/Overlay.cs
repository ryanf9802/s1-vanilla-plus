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

    private struct Item { public Vector3 Pos; public Transform Source; }

    // One live clone of a vanilla icon object (circle + glyph children) per drawn item.
    private class Slot { public GameObject Go; public RectTransform Rt; public CanvasGroup Group; public int SourceId; }

    private static readonly System.Collections.Generic.List<Item> Items = new();
    private static readonly System.Collections.Generic.List<Slot> Pool = new();
    private static RectTransform _root;
    private static Canvas _canvas;

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
    /// Distance fade and global opacity are applied by the overlay.</summary>
    public static void Submit(Vector3 worldPos, Transform iconSource)
    {
        if (iconSource != null) Items.Add(new Item { Pos = worldPos, Source = iconSource });
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

            var slot = GetSlot(used++, item.Source);
            slot.Group.alpha = alpha * Settings.OverlayOpacity;
            slot.Rt.sizeDelta = new Vector2(Settings.OverlaySize, Settings.OverlaySize);
            slot.Rt.localPosition = new Vector3(local.x, local.y, 0f);
        }
        Hide(used);
        LastDrawn = used;
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
        var go = new GameObject("CompassOverlay");
        go.transform.SetParent(hudCanvas.transform, false);
        _root = go.AddComponent<RectTransform>();
        _root.anchorMin = Vector2.zero;
        _root.anchorMax = Vector2.one;
        _root.offsetMin = _root.offsetMax = Vector2.zero;
        MelonLoader.MelonLogger.Msg($"[Compass] overlay root created under canvas '{hudCanvas.name}' mode={hudCanvas.renderMode}");
    }
}
