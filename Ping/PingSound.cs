using UnityEngine;

namespace Ping;

/// <summary>A short synthesized blip, so no audio asset is needed. Pitch varies per player so pings are distinguishable by ear.</summary>
public static class PingSound
{
    private static AudioClip _clip;
    private static AudioSource _source;

    public static void Play(int playerIndex)
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
        }
        _source.pitch = 0.85f + 0.09f * (playerIndex % 8);
        _source.PlayOneShot(_clip, Settings.Volume);
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
