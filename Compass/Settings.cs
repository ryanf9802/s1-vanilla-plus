using MelonLoader;
using MelonLoader.Preferences;

namespace Compass;

/// <summary>
/// User-tweakable options, stored in UserData/MelonPreferences.cfg. Every entry has a display name, description and
/// (for numbers) a ValueRange, so the in-game Mods App shows proper sliders. Values are read live each frame, so
/// edits made through that app apply immediately. Category identifiers start with the mod name so the app files
/// them under Compass.
/// </summary>
public static class Settings
{
    private static MelonPreferences_Entry<float> _maxDistanceFt, _fadeStartFt, _minAlpha, _facingAngle, _chevronThresholdFt, _chevronSize;
    private static MelonPreferences_Entry<float> _overlayMaxFt, _overlayFadeInFt, _overlayFadeStartFt, _overlayFadeEndFt, _overlayHeightFt,
        _overlaySize, _overlayOpacity, _overlayLookRadius, _overlayNameOpacity, _overlayNameScale;
    private static MelonPreferences_Entry<bool> _overlayQuestsOnly, _overlayShowNames, _debugLogging, _showPlayers, _playersOnOverlay, _showDealers, _dealersOnOverlay;

    // --- Compass strip -------------------------------------------------------------------------------------------
    /// <summary>Markers farther than this many feet are hidden; markers right at it use MinAlpha.</summary>
    public static float MaxDistanceFt => _maxDistanceFt.Value;
    /// <summary>Markers closer than this many feet are fully opaque.</summary>
    public static float FadeStartFt => _fadeStartFt.Value;
    public static float MinAlpha => _minAlpha.Value;
    /// <summary>Horizontal angle (degrees) within which a marker counts as "faced" and shows its distance.</summary>
    public static float FacingAngle => _facingAngle.Value;
    /// <summary>Show the up/down chevron when the target is at least this many feet above/below you.</summary>
    public static float ChevronThresholdFt => _chevronThresholdFt.Value;
    public static float ChevronSize => _chevronSize.Value;
    /// <summary>Show co-op partners on the compass strip.</summary>
    public static bool ShowPlayers => _showPlayers.Value;
    /// <summary>Show a through-wall icon above co-op partners (within the overlay distance).</summary>
    public static bool PlayersOnOverlay => _playersOnOverlay.Value;
    /// <summary>Show your recruited dealers on the compass strip.</summary>
    public static bool ShowDealers => _showDealers.Value;
    /// <summary>Show a through-wall icon above your recruited dealers (within the overlay distance).</summary>
    public static bool DealersOnOverlay => _dealersOnOverlay.Value;
    public static bool DebugLogging => _debugLogging.Value;

    // --- Overlay icons (distances in feet) -----------------------------------------------------------------------
    /// <summary>Overlay icons appear within this many feet.</summary>
    public static float OverlayMaxFt => _overlayMaxFt.Value;
    /// <summary>Icons fade in from 0 to full over this many feet after entering OverlayMaxFt (so they don't pop in).</summary>
    public static float OverlayFadeInFt => _overlayFadeInFt.Value;
    /// <summary>Icons are fully opaque farther than this, and start fading out as you get closer.</summary>
    public static float OverlayFadeStartFt => _overlayFadeStartFt.Value;
    /// <summary>Icons are fully invisible at or closer than this.</summary>
    public static float OverlayFadeEndFt => _overlayFadeEndFt.Value;
    /// <summary>How far above the target the icon floats.</summary>
    public static float OverlayHeightFt => _overlayHeightFt.Value;
    public static float OverlaySize => _overlaySize.Value;
    /// <summary>Overall opacity of overlay icons (0-1), multiplied with the distance fade.</summary>
    public static float OverlayOpacity => _overlayOpacity.Value;
    /// <summary>If true, only numbered quest objectives get overlay icons; if false, map landmarks do too.</summary>
    public static bool OverlayQuestsOnly => _overlayQuestsOnly.Value;
    /// <summary>Show the marker's name under an overlay icon while you look at it.</summary>
    public static bool OverlayShowNames => _overlayShowNames.Value;
    /// <summary>How close to screen center an icon must be to count as "looked at", as a fraction of screen height.</summary>
    public static float OverlayLookRadius => _overlayLookRadius.Value;
    /// <summary>Opacity of the name label at full strength (0-1).</summary>
    public static float OverlayNameOpacity => _overlayNameOpacity.Value;
    /// <summary>Name label font size as a multiple of the compass distance label's.</summary>
    public static float OverlayNameScale => _overlayNameScale.Value;

    public static void Init()
    {
        var c = MelonPreferences.CreateCategory("Compass", "Compass");
        _maxDistanceFt = Float(c, "MaxDistanceFt", 500f, "Max marker distance (ft)", "Compass markers farther than this are hidden. At this distance they use the minimum opacity.", 50f, 3000f);
        _fadeStartFt = Float(c, "FadeStartFt", 50f, "Full opacity within (ft)", "Compass markers closer than this are fully opaque; they fade toward the minimum opacity as they get farther.", 0f, 1000f);
        _minAlpha = Float(c, "MinAlpha", 0.25f, "Minimum opacity", "Opacity of a compass marker at the max distance.", 0f, 1f);
        _facingAngle = Float(c, "FacingAngle", 6f, "Facing angle (degrees)", "A marker shows its distance label only while it is within this many degrees of straight ahead. Only the closest such marker gets one.", 1f, 45f);
        _chevronThresholdFt = Float(c, "ChevronThresholdFt", 10f, "Elevation chevron threshold (ft)", "Show an up/down chevron after the distance when the target is at least this many feet above or below you.", 1f, 200f);
        _chevronSize = Float(c, "ChevronSize", 8f, "Chevron size", "Size of the elevation chevron in UI pixels.", 2f, 40f);
        _debugLogging = c.CreateEntry("DebugLogging", false, "Debug logging", "Log compass state to the MelonLoader log every few seconds. Leave off unless troubleshooting.");

        var pl = MelonPreferences.CreateCategory("Compass_Players", "Compass - Players");
        _showPlayers = pl.CreateEntry("ShowPlayers", true, "Show players on compass", "Show your co-op partners on the compass, each in their own color.");
        _playersOnOverlay = pl.CreateEntry("PlayersOnOverlay", false, "Show players through walls", "Show an icon above co-op partners within the overlay distance, even through walls.");

        _showDealers = pl.CreateEntry("ShowDealers", true, "Show dealers on compass", "Show your recruited dealers on the compass.");
        _dealersOnOverlay = pl.CreateEntry("DealersOnOverlay", false, "Show dealers through walls", "Show an icon above your recruited dealers within the overlay distance, even through walls.");

        var o = MelonPreferences.CreateCategory("Compass_Overlay", "Compass - Overlay icons");
        _overlayMaxFt = Float(o, "OverlayMaxFt", 50f, "Show within (ft)", "Overlay icons appear above quest markers and landmarks within this distance, even through walls.", 10f, 500f);
        _overlayFadeInFt = Float(o, "OverlayFadeInFt", 16.4f, "Fade-in distance (ft)", "Icons fade in over this many feet after entering range, so they don't pop in.", 0f, 100f);
        _overlayFadeStartFt = Float(o, "OverlayFadeStartFt", 6f, "Start fading out at (ft)", "Icons begin fading out when you are closer than this.", 0f, 50f);
        _overlayFadeEndFt = Float(o, "OverlayFadeEndFt", 2f, "Fully hidden at (ft)", "Icons are invisible at this distance or closer.", 0f, 50f);
        _overlayHeightFt = Float(o, "OverlayHeightFt", 5f, "Height above target (ft)", "How far above the target the icon floats.", 0f, 30f);
        _overlaySize = Float(o, "OverlaySize", 22f, "Icon size", "Overlay icon size in UI pixels.", 8f, 100f);
        _overlayOpacity = Float(o, "OverlayOpacity", 0.6f, "Icon opacity", "Overall opacity of overlay icons, at their most visible.", 0.05f, 1f);
        _overlayQuestsOnly = o.CreateEntry("OverlayQuestsOnly", false, "Quest objectives only", "On: only numbered quest objectives get overlay icons. Off: everything shown on the compass does (landmarks, NPCs, other markers).");
        _overlayShowNames = o.CreateEntry("OverlayShowNames", true, "Show names", "Show the marker's name under an overlay icon while you look at it.");
        _overlayLookRadius = Float(o, "OverlayLookRadius", 0.05f, "Look radius", "How close to screen center an icon must be to count as looked at, as a fraction of screen height.", 0.01f, 0.3f);
        _overlayNameOpacity = Float(o, "OverlayNameOpacity", 0.85f, "Name opacity", "Opacity of the name label.", 0.05f, 1f);
        _overlayNameScale = Float(o, "OverlayNameScale", 0.8f, "Name text size", "Name label font size relative to the compass distance label.", 0.4f, 2f);
    }

    private static MelonPreferences_Entry<float> Float(MelonPreferences_Category c, string id, float def, string display, string description, float min, float max)
        => c.CreateEntry(id, def, display, description, validator: new ValueRange<float>(min, max));
}
