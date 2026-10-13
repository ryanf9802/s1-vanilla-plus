using MelonLoader;

namespace Compass;

/// <summary>User-tweakable options, stored in UserData/MelonPreferences.cfg under [Compass].</summary>
public static class Settings
{
    private static MelonPreferences_Entry<float> _fadeStart, _fadeEnd, _minAlpha, _facingAngle, _chevronThresholdFt, _chevronSize;

    /// <summary>Markers closer than this (world units) are fully opaque.</summary>
    public static float FadeStart => _fadeStart.Value;
    /// <summary>Markers at or beyond this distance use MinAlpha.</summary>
    public static float FadeEnd => _fadeEnd.Value;
    public static float MinAlpha => _minAlpha.Value;
    /// <summary>Horizontal angle (degrees) within which a marker counts as "faced" and shows its distance.</summary>
    public static float FacingAngle => _facingAngle.Value;
    /// <summary>Show the up/down chevron when the target is at least this many feet above/below you.</summary>
    public static float ChevronThresholdFt => _chevronThresholdFt.Value;
    public static float ChevronSize => _chevronSize.Value;

    public static void Init()
    {
        var c = MelonPreferences.CreateCategory("Compass");
        _fadeStart = c.CreateEntry("FadeStart", 20f, description: "Markers closer than this (world units) are fully opaque");
        _fadeEnd = c.CreateEntry("FadeEnd", 200f, description: "Markers at or beyond this distance use MinAlpha");
        _minAlpha = c.CreateEntry("MinAlpha", 0.25f, description: "Opacity of the farthest markers (0-1)");
        _facingAngle = c.CreateEntry("FacingAngle", 10f, description: "Show distance only for markers within this many degrees of straight ahead");
        _chevronThresholdFt = c.CreateEntry("ChevronThresholdFt", 10f, description: "Show an up/down chevron when the target is at least this many feet above/below you");
        _chevronSize = c.CreateEntry("ChevronSize", 12f, description: "Chevron size in UI pixels");
    }
}
