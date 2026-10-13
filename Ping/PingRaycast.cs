using UnityEngine;

namespace Ping;

/// <summary>
/// The ping ray. The game switches the colliders of distant buildings off (they are active but disabled until you get within
/// roughly 65 ft), so a plain physics raycast flies through far buildings and lands on whatever is behind them. This adds an
/// exact ray-vs-box test against those disabled map boxes and takes whichever surface is nearer.
/// </summary>
internal static class PingRaycast
{
    private struct Candidate { public BoxCollider Box; public Vector3 Center; public float Radius; }

    private static readonly System.Collections.Generic.List<Candidate> Candidates = new();
    private static float _builtAt = -1000f;
    private const float RefreshSeconds = 60f; // the map is static; this only catches scene loads and newly built things

    public static bool Cast(Vector3 origin, Vector3 dir, float maxUnits, int mask, out Vector3 point, out Vector3 normal)
    {
        float best = maxUnits;
        point = default;
        normal = Vector3.up;
        bool found = false;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, maxUnits, mask, QueryTriggerInteraction.Ignore))
        {
            best = hit.distance;
            point = hit.point;
            normal = hit.normal;
            found = true;
        }

        if (Time.realtimeSinceStartup - _builtAt > RefreshSeconds) Rebuild(mask);

        foreach (var c in Candidates)
        {
            // Cheap sphere prefilter in plain C#, so the precise test only runs for boxes near the ray.
            Vector3 oc = c.Center - origin;
            float along = Vector3.Dot(oc, dir);
            if (along < -c.Radius || along > best + c.Radius) continue;
            if (oc.sqrMagnitude - along * along > c.Radius * c.Radius) continue;

            var box = c.Box;
            if (box == null || box.enabled || !box.gameObject.activeInHierarchy) continue; // enabled boxes are covered by physics
            if (!RayBox(box, origin, dir, best, out float t, out Vector3 n) || t >= best) continue;
            best = t;
            point = origin + dir * t;
            normal = n;
            found = true;
        }
        return found;
    }

    private static void Rebuild(int mask)
    {
        _builtAt = Time.realtimeSinceStartup;
        Candidates.Clear();
        foreach (var b in UnityEngine.Object.FindObjectsOfType<BoxCollider>(true))
        {
            if (b == null || b.isTrigger || !b.gameObject.activeInHierarchy) continue;
            if ((mask & (1 << b.gameObject.layer)) == 0) continue;
            var t = b.transform;
            if (t.root.name != "Map") continue; // buildings and street furniture; leaves props, NPCs and managers alone
            Vector3 s = t.lossyScale;
            float radius = 0.5f * new Vector3(b.size.x * s.x, b.size.y * s.y, b.size.z * s.z).magnitude;
            Candidates.Add(new Candidate { Box = b, Center = t.TransformPoint(b.center), Radius = radius });
        }
        MelonLoader.MelonLogger.Msg($"[Ping] indexed {Candidates.Count} map boxes for long-range pings");
    }

    /// <summary>Slab test in the box's local space. Skips boxes the ray starts inside.</summary>
    private static bool RayBox(BoxCollider b, Vector3 origin, Vector3 dir, float maxT, out float tHit, out Vector3 worldNormal)
    {
        tHit = 0f;
        worldNormal = Vector3.up;
        var t = b.transform;
        Vector3 o = t.InverseTransformPoint(origin);
        Vector3 d = t.InverseTransformVector(dir);
        Vector3 lo = b.center - b.size * 0.5f, hi = b.center + b.size * 0.5f;
        float tmin = 0f, tmax = maxT;
        int axis = -1;
        float sign = 0f;
        for (int a = 0; a < 3; a++)
        {
            float oa = o[a], da = d[a];
            if (Mathf.Abs(da) < 1e-6f)
            {
                if (oa < lo[a] || oa > hi[a]) return false;
                continue;
            }
            float t1 = (lo[a] - oa) / da, t2 = (hi[a] - oa) / da;
            float entrySign = da > 0f ? -1f : 1f; // entering through the low face means the normal points toward -axis
            if (t1 > t2) { (t1, t2) = (t2, t1); }
            if (t1 > tmin) { tmin = t1; axis = a; sign = entrySign; }
            tmax = Mathf.Min(tmax, t2);
            if (tmin > tmax) return false;
        }
        if (axis < 0 || tmin <= 0.01f) return false; // started inside the box
        var local = Vector3.zero;
        local[axis] = sign;
        worldNormal = t.TransformDirection(local).normalized;
        tHit = tmin;
        return true;
    }
}
