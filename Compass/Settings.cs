using MelonLoader;

namespace Compass;

/// <summary>User-tweakable options, stored in UserData/MelonPreferences.cfg under [Compass].</summary>
public static class Settings
{
    private static MelonPreferences_Entry<float> _fadeStart, _fadeEnd, _minAlpha, _facingAngle, _levelTolerance;
    private static MelonPreferences_Entry<bool> _showElevation;

    /// <summary>Markers closer than this (world units) are fully opaque.</summary>
    public static float FadeStart => _fadeStart.Value;
    /// <summary>Markers farther than this are at MinAlpha.</summary>
    public static float FadeEnd => _fadeEnd.Value;
    public static float MinAlpha => _minAlpha.Value;
    /// <summary>Horizontal angle (degrees) within which a marker counts as "faced" and shows its distance.</summary>
    public static float FacingAngle => _facingAngle.Value;
    /// <summary>Height differences below this many world units are treated as level.</summary>
    public static float LevelTolerance => _levelTolerance.Value;
    public static bool ShowElevation => _showElevation.Value;

    public static void Init()
    {
        var c = MelonPreferences.CreateCategory("Compass");
        _fadeStart = c.CreateEntry("FadeStart", 50f, description: "Markers closer than this are fully opaque");
        _fadeEnd = c.CreateEntry("FadeEnd", 400f, description: "Markers farther than this use MinAlpha");
        _minAlpha = c.CreateEntry("MinAlpha", 0.35f, description: "Opacity of the farthest markers (0-1)");
        _facingAngle = c.CreateEntry("FacingAngle", 10f, description: "Show distance for any marker within this many degrees of straight ahead");
        _levelTolerance = c.CreateEntry("LevelTolerance", 3f, description: "Height differences below this are not shown");
        _showElevation = c.CreateEntry("ShowElevation", true, description: "Append +/- height difference to the distance label");
    }
}
