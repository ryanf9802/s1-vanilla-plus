using MelonLoader;
using MelonLoader.Preferences;

namespace DealWindow;

/// <summary>DealWindow settings, stored in UserData/MelonPreferences.cfg under [DealWindow]. Written to be shown by the Mods App (display names, ranges).</summary>
public static class Settings
{
    private static MelonPreferences_Entry<bool> _enabled, _hud, _journal, _map, _compass, _debug;
    private static MelonPreferences_Entry<int> _leadMinutes;

    public static bool Enabled => _enabled.Value;
    /// <summary>Minutes before the window opens at which the deal starts to show. 0 = exactly when it opens.</summary>
    public static int LeadMinutes => _leadMinutes.Value;
    public static bool HideInHud => _hud.Value;
    public static bool HideInJournal => _journal.Value;
    public static bool HideOnMap => _map.Value;
    public static bool HideOnCompass => _compass.Value;
    public static bool DebugLogging => _debug.Value;

    public static void Init()
    {
        var c = MelonPreferences.CreateCategory("DealWindow", "Deal Window");
        _enabled = c.CreateEntry("Enabled", true, "Hide deals until their window", "Customer deals you have accepted stay hidden until their delivery window opens.");
        _leadMinutes = c.CreateEntry("LeadMinutes", 0, "Show early (min)", "Show a deal this many in-game minutes before its window opens. 0 shows it exactly when the window opens.", validator: new ValueRange<int>(0, 240));
        _hud = c.CreateEntry("HideInHud", true, "Hide in quest HUD", "Keep not-yet-open deals out of the quest list on screen.");
        _journal = c.CreateEntry("HideInJournal", true, "Hide in journal", "Keep not-yet-open deals out of the journal's quest list.");
        _map = c.CreateEntry("HideOnMap", true, "Hide on map", "Keep not-yet-open deals' map markers hidden.");
        _compass = c.CreateEntry("HideOnCompass", true, "Hide on compass", "Keep not-yet-open deals' compass markers (and their through-wall icons) hidden.");
        _debug = c.CreateEntry("DebugLogging", false, "Debug logging", "Log when deals are hidden and shown.");
    }
}
