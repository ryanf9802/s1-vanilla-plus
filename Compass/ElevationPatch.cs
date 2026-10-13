using HarmonyLib;
using Il2CppScheduleOne.UI.Compass;
using UnityEngine;

namespace Compass;

/// <summary>
/// Appends the height difference between the camera and a compass element's target to the
/// element's vanilla distance label, e.g. "142m" becomes "142m +12m".
/// </summary>
[HarmonyPatch(typeof(CompassManager), nameof(CompassManager.UpdateElement))]
public static class ElevationPatch
{
    // Height differences smaller than this are treated as level and not shown.
    private const float LevelTolerance = 3f;
    private const string Marker = " \u200B"; // zero-width space marks text we appended

    public static float LastDelta;

    public static void Postfix(CompassManager __instance, CompassManager.Element element)
    {
        try
        {
            var label = element?.DistanceLabel;
            var target = element?.TargetTransform;
            var cam = __instance.cam;
            if (label == null || target == null || cam == null || !element.Visible) return;

            string text = label.text ?? "";
            int idx = text.IndexOf(Marker, System.StringComparison.Ordinal);
            if (idx >= 0) text = text.Substring(0, idx);
            if (text.Length == 0) return; // vanilla hides the label, leave it alone

            float dy = target.position.y - cam.position.y;
            LastDelta = dy;
            if (Mathf.Abs(dy) < LevelTolerance) { label.text = text; return; }
            label.text = $"{text}{Marker}{(dy > 0 ? "+" : "-")}{Mathf.RoundToInt(Mathf.Abs(dy))}m";
        }
        catch (System.Exception e)
        {
            MelonLoader.MelonLogger.Error($"[Compass] ElevationPatch failed: {e.Message}");
        }
    }
}
