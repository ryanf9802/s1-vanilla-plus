using MelonLoader;
using MelonLoader.Preferences;
using UnityEngine;

namespace Ping;

/// <summary>Ping settings, stored in UserData/MelonPreferences.cfg under [Ping]. Written to be shown by the Mods App (display names, ranges).</summary>
public static class Settings
{
    private static MelonPreferences_Entry<KeyCode> _key;
    private static MelonPreferences_Entry<float> _lifetime, _volume, _maxDistanceFt, _cooldown;
    private static MelonPreferences_Entry<bool> _sound, _directional, _onCompass, _onOverlay;

    /// <summary>Key or mouse button that sends a ping. Mouse2 is the middle mouse button.</summary>
    public static KeyCode Key => _key.Value;
    public static float LifetimeSeconds => _lifetime.Value;
    public static float Volume => _volume.Value;
    public static float MaxDistanceFt => _maxDistanceFt.Value;
    public static float CooldownSeconds => _cooldown.Value;
    public static bool Sound => _sound.Value;
    public static bool Directional => _directional.Value;
    public static bool OnCompass => _onCompass.Value;
    public static bool OnOverlay => _onOverlay.Value;

    public static void Init()
    {
        var c = MelonPreferences.CreateCategory("Ping", "Ping");
        _key = c.CreateEntry("Key", KeyCode.Mouse2, "Ping key", "Key or mouse button that sends a ping to your co-op partners. Mouse2 is the middle mouse button.");
        _lifetime = Float(c, "LifetimeSeconds", 5f, "Ping duration (s)", "How long a ping stays visible.", 1f, 30f);
        _maxDistanceFt = Float(c, "MaxDistanceFt", 1500f, "Max ping distance (ft)", "Farthest point you can ping.", 50f, 5000f);
        _cooldown = Float(c, "CooldownSeconds", 0.4f, "Cooldown (s)", "Minimum time between your pings.", 0.1f, 5f);
        _sound = c.CreateEntry("Sound", true, "Ping sound", "Play a sound when a ping is placed (by you or a partner).");
        _directional = c.CreateEntry("Directional", true, "Directional sound", "Pan the ping sound toward the side the ping is on, and muffle it when it is behind you.");
        _volume = Float(c, "Volume", 0.5f, "Ping volume", "Volume of the ping sound.", 0f, 1f);
        _onCompass = c.CreateEntry("OnCompass", true, "Show on compass", "Show pings as markers on the compass strip.");
        _onOverlay = c.CreateEntry("OnOverlay", true, "Show in world", "Show pings as icons above the target, visible through walls.");
    }

    private static MelonPreferences_Entry<float> Float(MelonPreferences_Category c, string id, float def, string display, string description, float min, float max)
        => c.CreateEntry(id, def, display, description, validator: new ValueRange<float>(min, max));
}
