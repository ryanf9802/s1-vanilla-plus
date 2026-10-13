using HarmonyLib;
using Il2CppScheduleOne.UI.Compass;
using UnityEngine;

namespace Compass;

/// <summary>
/// Runs after the vanilla per-element update. Fades markers by distance, shows the distance label for any
/// marker you are facing (vanilla only labels markers within DISTANCE_LABEL_THRESHOLD), and appends the
/// height difference to the label.
/// </summary>
[HarmonyPatch(typeof(CompassManager), nameof(CompassManager.UpdateElement))]
public static class ElevationPatch
{
    private const float FeetPerUnit = 3.28084f; // vanilla labels are in feet
    private const string Marker = " \u200B";    // zero-width space marks text we appended
    private static bool _errorLogged;

    public static void Postfix(CompassManager.Element element)
    {
        try
        {
            var cm = CompassManager.Instance;
            var target = element?.TargetTransform;
            if (cm == null || target == null || cm.cam == null || !element.Visible) return;

            Vector3 camPos = cm.cam.position;
            Vector3 toTarget = target.position - camPos;
            float dist = toTarget.magnitude;

            // Distance fade, multiplied onto the alpha vanilla would use for this angle.
            var group = element.Group;
            if (group != null)
            {
                cm.GetCompassData(target.position, out _, out float vanillaAlpha);
                float t = Mathf.InverseLerp(Settings.FadeStart, Settings.FadeEnd, dist);
                group.alpha = vanillaAlpha * Mathf.Lerp(1f, Settings.MinAlpha, t);
            }

            var label = element.DistanceLabel;
            if (label == null) return;

            string text = label.text ?? "";
            int idx = text.IndexOf(Marker, System.StringComparison.Ordinal);
            if (idx >= 0) text = text.Substring(0, idx);

            // Facing check on the horizontal plane, so looking up/down at a marker still counts.
            Vector3 fwd = cm.cam.forward; fwd.y = 0f;
            Vector3 flat = toTarget; flat.y = 0f;
            bool facing = fwd.sqrMagnitude > 1e-4f && flat.sqrMagnitude > 1e-4f
                          && Vector3.Angle(fwd, flat) <= Settings.FacingAngle;

            if (text.Length == 0 && facing)
                text = $"{Mathf.RoundToInt(dist * FeetPerUnit)}ft";
            if (text.Length == 0) { label.text = text; return; }

            float dy = toTarget.y;
            if (Settings.ShowElevation && Mathf.Abs(dy) >= Settings.LevelTolerance)
                text = $"{text}{Marker}{(dy > 0 ? "+" : "-")}{Mathf.RoundToInt(Mathf.Abs(dy) * FeetPerUnit)}ft";
            label.text = text;
        }
        catch (System.Exception e)
        {
            if (_errorLogged) return;
            _errorLogged = true;
            MelonLoader.MelonLogger.Error($"[Compass] ElevationPatch failed (logged once): {e}");
        }
    }
}
