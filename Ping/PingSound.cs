using UnityEngine;

namespace Ping;

/// <summary>A short synthesized blip, so no audio asset is needed. Pitch varies per player so pings are distinguishable by ear.</summary>
public static class PingSound
{
    private static AudioClip _clip;
    private static AudioSource _source;
    private static AudioLowPassFilter _lowPass;

    /// <summary>Play the blip for a ping at <paramref name="worldPos"/>; with directional audio it pans toward the ping's side
    /// of where you are looking and is muffled when the ping is behind you.</summary>
    public static void Play(int playerIndex, Vector3 worldPos)
    {
        if (!Settings.Sound || Settings.Volume <= 0f) return;
        if (_clip == null) _clip = Build();
        if (_source == null)
        {
            var go = new GameObject("PingSound");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _source = go.AddComponent<AudioSource>();
            _source.spatialBlend = 0f; // plain 2D so every player hears it clearly
            _source.playOnAwake = false;
            _lowPass = go.AddComponent<AudioLowPassFilter>();
        }
        ApplyDirection(worldPos);
        _source.pitch = 0.85f + 0.09f * (playerIndex % 8);
        _source.PlayOneShot(_clip, Settings.Volume);
    }

    private static void ApplyDirection(Vector3 worldPos)
    {
        float pan = 0f, cutoff = 22000f;
        var cam = Camera.main;
        if (Settings.Directional && cam != null)
        {
            Vector3 fwd = cam.transform.forward; fwd.y = 0f;
            Vector3 to = worldPos - cam.transform.position; to.y = 0f;
            if (fwd.sqrMagnitude > 1e-4f && to.sqrMagnitude > 1f) // closer than 1 unit: no meaningful direction
            {
                float angle = Vector3.SignedAngle(fwd, to, Vector3.up) * Mathf.Deg2Rad; // + is to your right
                pan = Mathf.Sin(angle) * 0.9f;
                float behind = Mathf.Clamp01(-Mathf.Cos(angle) * 1.2f);
                cutoff = Mathf.Lerp(22000f, 1800f, behind);
            }
        }
        _source.panStereo = pan;
        if (_lowPass != null) _lowPass.cutoffFrequency = cutoff;
    }

    private static AudioClip Build()
    {
        const int rate = 44100;
        int n = (int)(rate * 0.22f);
        var data = new float[n];
        double phase = 0;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            double freq = 880.0 + 440.0 * Math.Min(1.0, t * 4.0);            // quick upward glide
            phase += 2.0 * Math.PI * freq / rate;
            float env = Mathf.Min(1f, i / 220f) * Mathf.Exp(-t * 6f);          // click-free attack, soft decay
            data[i] = (float)Math.Sin(phase) * env * 0.6f;
        }
        var clip = AudioClip.Create("PingBlip", n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
