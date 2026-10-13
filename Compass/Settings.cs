using MelonLoader;

namespace Compass;

/// <summary>User-tweakable options, stored in UserData/MelonPreferences.cfg under [Compass].</summary>
public static class Settings
{
    private static MelonPreferences_Entry<bool> _overlayQuestsOnly, _overlayShowNames;
    private static MelonPreferences_Entry<float> _fadeStartFt, _maxDistanceFt, _minAlpha, _facingAngle, _chevronThresholdFt, _chevronSize,
        _overlayMaxFt, _overlayFadeStartFt, _overlayFadeEndFt, _overlayHeightFt, _overlaySize, _overlayOpacity, _overlayFadeInFt, _overlayLookRadius, _overlayNameOpacity, _overlayNameScale;

    /// <summary>Markers closer than this many feet are fully opaque.</summary>
    public static float FadeStartFt => _fadeStartFt.Value;
    /// <summary>Markers farther than this many feet are hidden; markers right at it use MinAlpha.</summary>
    public static float MaxDistanceFt => _maxDistanceFt.Value;
    public static float MinAlpha => _minAlpha.Value;
    /// <summary>Horizontal angle (degrees) within which a marker counts as "faced" and shows its distance.</summary>
    public static float FacingAngle => _facingAngle.Value;
    /// <summary>Show the up/down chevron when the target is at least this many feet above/below you.</summary>
    public static float ChevronThresholdFt => _chevronThresholdFt.Value;
    public static float ChevronSize => _chevronSize.Value;

    // Overlay icons (quest objectives now, pings later); all distances in feet.
    /// <summary>Overlay icons appear within this many feet.</summary>
    public static float OverlayMaxFt => _overlayMaxFt.Value;
    /// <summary>Icons are fully opaque farther than this, and start fading out as you get closer.</summary>
    public static float OverlayFadeStartFt => _overlayFadeStartFt.Value;
    /// <summary>Icons are fully invisible at or closer than this.</summary>
    public static float OverlayFadeEndFt => _overlayFadeEndFt.Value;
    /// <summary>How far above the target the icon floats.</summary>
    public static float OverlayHeightFt => _overlayHeightFt.Value;
    public static float OverlaySize => _overlaySize.Value;
    /// <summary>Show the marker's name under an overlay icon while you look at it.</summary>
    public static bool OverlayShowNames => _overlayShowNames.Value;
    /// <summary>How close to screen center an icon must be to count as "looked at", as a fraction of screen height.</summary>
    public static float OverlayLookRadius => _overlayLookRadius.Value;
    /// <summary>Opacity of the name label at full strength (0-1).</summary>
    public static float OverlayNameOpacity => _overlayNameOpacity.Value;
    /// <summary>Name label font size as a multiple of the compass distance label's.</summary>
    public static float OverlayNameScale => _overlayNameScale.Value;
    /// <summary>Icons fade in from 0 to full over this many feet after entering OverlayMaxFt (so they don't pop in).</summary>
    public static float OverlayFadeInFt => _overlayFadeInFt.Value;
    /// <summary>Overall opacity of overlay icons (0-1), multiplied with the distance fade.</summary>
    public static float OverlayOpacity => _overlayOpacity.Value;
    /// <summary>If true, only numbered quest objectives get overlay icons; if false, map landmarks do too.</summary>
    public static bool OverlayQuestsOnly => _overlayQuestsOnly.Value;

    public static void Init()
    {
        var c = MelonPreferences.CreateCategory("Compass");
        _fadeStartFt = c.CreateEntry("FadeStartFt", 50f, description: "Markers closer than this many feet are fully opaque");
        _maxDistanceFt = c.CreateEntry("MaxDistanceFt", 500f, description: "Markers farther than this many feet are hidden; at this distance they use MinAlpha");
        _minAlpha = c.CreateEntry("MinAlpha", 0.25f, description: "Opacity of markers at MaxDistanceFt (0-1)");
        _facingAngle = c.CreateEntry("FacingAngle", 6f, description: "Show distance only for markers within this many degrees of straight ahead");
        _chevronThresholdFt = c.CreateEntry("ChevronThresholdFt", 10f, description: "Show an up/down chevron when the target is at least this many feet above/below you");
        _overlayMaxFt = c.CreateEntry("OverlayMaxFt", 75f, description: "Overlay icons (quest objectives) show within this many feet");
        _overlayFadeStartFt = c.CreateEntry("OverlayFadeStartFt", 6f, description: "Overlay icons start fading out when closer than this many feet");
        _overlayFadeEndFt = c.CreateEntry("OverlayFadeEndFt", 2f, description: "Overlay icons are fully invisible at this many feet or closer");
        _overlayHeightFt = c.CreateEntry("OverlayHeightFt", 5f, description: "How many feet above the target the overlay icon floats");
        _overlayQuestsOnly = c.CreateEntry("OverlayQuestsOnly", false, description: "true: only numbered quest objectives get overlay icons; false: map landmarks too (NPCs never do)");
        _overlayOpacity = c.CreateEntry("OverlayOpacity", 0.6f, description: "Overall opacity of overlay icons (0-1)");
        _overlayFadeInFt = c.CreateEntry("OverlayFadeInFt", 16.4f, description: "Overlay icons fade in over this many feet after entering OverlayMaxFt (16.4ft = 5m)");
        _overlayShowNames = c.CreateEntry("OverlayShowNames", true, description: "Show the marker name under an overlay icon while you look at it");
        _overlayLookRadius = c.CreateEntry("OverlayLookRadius", 0.05f, description: "How close to screen center an icon must be to count as looked at (fraction of screen height)");
        _overlayNameOpacity = c.CreateEntry("OverlayNameOpacity", 0.85f, description: "Opacity of the name label (0-1)");
        _overlayNameScale = c.CreateEntry("OverlayNameScale", 0.8f, description: "Name label font size relative to the compass distance label");
        _overlaySize = c.CreateEntry("OverlaySize", 22f, description: "Overlay icon size in UI pixels");
        _chevronSize = c.CreateEntry("ChevronSize", 8f, description: "Chevron size in UI pixels");
    }
}
