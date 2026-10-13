using MelonLoader;

namespace Compass;

/// <summary>User-tweakable options, stored in UserData/MelonPreferences.cfg under [Compass].</summary>
public static class Settings
{
    private static MelonPreferences_Entry<float> _fadeStartFt, _maxDistanceFt, _minAlpha, _facingAngle, _chevronThresholdFt, _chevronSize;

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

    public static void Init()
    {
        var c = MelonPreferences.CreateCategory("Compass");
        _fadeStartFt = c.CreateEntry("FadeStartFt", 50f, description: "Markers closer than this many feet are fully opaque");
        _maxDistanceFt = c.CreateEntry("MaxDistanceFt", 500f, description: "Markers farther than this many feet are hidden; at this distance they use MinAlpha");
        _minAlpha = c.CreateEntry("MinAlpha", 0.25f, description: "Opacity of markers at MaxDistanceFt (0-1)");
        _facingAngle = c.CreateEntry("FacingAngle", 10f, description: "Show distance only for markers within this many degrees of straight ahead");
        _chevronThresholdFt = c.CreateEntry("ChevronThresholdFt", 10f, description: "Show an up/down chevron when the target is at least this many feet above/below you");
        _chevronSize = c.CreateEntry("ChevronSize", 4f, description: "Chevron size in UI pixels");
    }
}
