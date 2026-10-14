using HarmonyLib;
using Il2CppScheduleOne.Quests;
using MelonLoader;

[assembly: MelonInfo(typeof(DealWindow.DealWindow), DealWindow.BuildInfo.Name, DealWindow.BuildInfo.Version, DealWindow.BuildInfo.Author)]
[assembly: MelonColor(1, 255, 0, 0)]
[assembly: MelonGame("TVGS", "Schedule I")]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]

namespace DealWindow;

public static class BuildInfo
{
    public const string Name = "DealWindow";
    public const string Description = "Customer deals stay hidden until their delivery window opens";
    public const string Author = "ryanf9802";
    public const string Version = "0.1.0";
}

/// <summary>
/// Accepted customer deals (Contract quests) appear in the quest HUD, journal, map and compass immediately, even when their
/// delivery window opens hours later. This hides them until the window opens (see docs/dealwindow.md).
/// </summary>
public class DealWindow : MelonMod
{
    public override void OnInitializeMelon()
    {
        Settings.Init();
        // Best-effort Harmony postfixes that stop vanilla from re-showing a hidden deal's map/compass markers between our ticks.
        // The per-frame DealHider is what actually hides things, so a failed patch only costs a frame of flicker.
        TryPatch(typeof(QuestEntry), nameof(QuestEntry.ShouldShowPoI), nameof(Patches.ShouldShowPoI));
        TryPatch(typeof(QuestEntry), nameof(QuestEntry.UpdateCompassElement), nameof(Patches.UpdateCompassElement));
        LoggerInstance.Msg("DealWindow initialized");
    }

    private void TryPatch(System.Type type, string method, string postfix)
    {
        try
        {
            var target = type.GetMethod(method);
            var post = typeof(Patches).GetMethod(postfix);
            HarmonyInstance.Patch(target, postfix: new HarmonyMethod(post));
        }
        catch (System.Exception e)
        {
            LoggerInstance.Warning($"Could not patch {type.Name}.{method}: {e.Message}");
        }
    }

    public override void OnUpdate()
    {
        try { DealHider.Tick(LoggerInstance); }
        catch (System.Exception e) { LoggerInstance.Error($"DealHider failed: {e}"); }
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName) => DealHider.Reset();
}

public static class Patches
{
    public static void ShouldShowPoI(QuestEntry __instance, ref bool __result)
    {
        if (__result && Settings.Enabled && Settings.HideOnMap && DealHider.IsPending(__instance.ParentQuest)) __result = false;
    }

    public static void UpdateCompassElement(QuestEntry __instance)
    {
        if (Settings.Enabled && Settings.HideOnCompass && DealHider.IsPending(__instance.ParentQuest))
        {
            var el = __instance.compassElement;
            if (el != null) el.Visible = false;
        }
    }
}
