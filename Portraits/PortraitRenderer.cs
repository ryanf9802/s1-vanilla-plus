using Il2CppScheduleOne.PlayerScripts;
using MelonLoader;
using UnityEngine;

namespace Portraits;

/// <summary>Renders a player's live avatar (head and shoulders) into a transparent texture.</summary>
internal static class PortraitRenderer
{
    private const int Size = 256;
    private const int IsolationLayer = 31; // the avatar is moved here for the instant of the shot so only it is drawn

    /// <summary>A cheap fingerprint of what the avatar is made of (parts, materials, textures); changes when they dress or look different.</summary>
    public static string Signature(Player p)
    {
        int h = 17;
        var rends = p.Avatar.GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends)
        {
            if (r == null) continue;
            h = h * 31 + r.gameObject.name.GetHashCode();
            var m = r.sharedMaterial;
            if (m == null) continue;
            h = h * 31 + m.GetInstanceID();
            var tex = m.mainTexture;
            if (tex != null) h = h * 31 + tex.GetInstanceID();
        }
        return $"{rends.Count}:{h}";
    }

    public static string Render(Player p, string label, MelonLogger.Instance log)
    {
        var avatar = p.Avatar;
        Transform head = FindHead(avatar.transform);
        log.Msg($"[portrait] '{label}': head bone={(head != null ? head.name + " scale=" + head.lossyScale : "not found")}");
        if (head == null) head = avatar.CenterPointTransform != null ? avatar.CenterPointTransform : avatar.transform;

        // Move every drawable part of the avatar to the isolation layer and make sure it is enabled (the local player's own
        // model is hidden from them, and distant avatars are culled), remembering how to put each back.
        var renderers = avatar.GetComponentsInChildren<Renderer>(true);
        var saved = new System.Collections.Generic.List<(Renderer r, bool enabled, int layer)>();
        foreach (var r in renderers)
        {
            if (r == null) continue;
            if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
            if (r.gameObject.name.Contains("mpostor")) continue;
            saved.Add((r, r.enabled, r.gameObject.layer));
            r.enabled = true;
            r.gameObject.layer = IsolationLayer;
        }

        string dir = System.IO.Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory, "Portraits");
        System.IO.Directory.CreateDirectory(dir);
        string first = null;
        try
        {
            foreach (var (suffix, dirSign) in new[] { ("front", 1f), ("back", -1f) })
            {
                string path = System.IO.Path.Combine(dir, $"{label}_{suffix}.png");
                Shoot(head, avatar.transform.forward * dirSign, path);
                first ??= path;
            }
        }
        finally
        {
            foreach (var (r, enabled, layer) in saved)
            {
                if (r == null) continue;
                r.gameObject.layer = layer;
                r.enabled = enabled;
            }
        }
        log.Msg($"[portrait] '{label}': {saved.Count} renderers shot");
        return first;
    }

    private static void Shoot(Transform head, Vector3 facing, string path)
    {
        var camGo = new GameObject("PortraitCamera");
        var rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        try
        {
            var cam = camGo.AddComponent<Camera>();
            cam.enabled = false; // rendered by hand, once
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.cullingMask = 1 << IsolationLayer;
            cam.fieldOfView = 28f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 10f;
            cam.allowHDR = false;
            cam.allowMSAA = false;
            cam.targetTexture = rt;

            Vector3 target = head.position + Vector3.up * 0.06f;
            camGo.transform.position = target + new Vector3(facing.x, 0f, facing.z).normalized * 0.8f;
            camGo.transform.LookAt(target);
            cam.Render();

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;

            var png = ImageConversion.EncodeToPNG(tex);
            var bytes = new byte[png.Length];
            for (int i = 0; i < bytes.Length; i++) bytes[i] = png[i];
            System.IO.File.WriteAllBytes(path, bytes);
        }
        finally
        {
            UnityEngine.Object.Destroy(camGo);
            UnityEngine.Object.Destroy(rt);
            UnityEngine.Object.Destroy(tex);
        }
    }

    private static Transform FindHead(Transform root)
    {
        Transform fallback = null;
        var queue = new System.Collections.Generic.Queue<Transform>();
        queue.Enqueue(root);
        while (queue.Count > 0)
        {
            var t = queue.Dequeue();
            string n = t.name ?? "";
            if (n == "mixamorig:Head") return t;
            if (fallback == null && n.Contains("Head") && !n.Contains("Top") && !n.Contains("End")) fallback = t;
            for (int i = 0; i < t.childCount; i++) queue.Enqueue(t.GetChild(i));
        }
        return fallback;
    }
}
