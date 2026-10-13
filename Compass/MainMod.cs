using System.Collections;
using MelonLoader;
using Compass.Helpers;
using UnityEngine;
using Il2CppScheduleOne.UI.Compass;

[assembly: MelonInfo(
    typeof(Compass.Compass),
    Compass.BuildInfo.Name,
    Compass.BuildInfo.Version,
    Compass.BuildInfo.Author
)]
[assembly: MelonColor(1, 255, 0, 0)]
[assembly: MelonGame("TVGS", "Schedule I")]

// Specify platform domain based on build target (remove this if your mod supports both via S1API)
#if MONO
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]
#else
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]
#endif

namespace Compass;

public static class BuildInfo
{
    public const string Name = "Compass";
    public const string Description = "Vanilla+ compass with distance and elevation markers";
    public const string Author = "ryanf9802";
    public const string Version = "0.1.0";
}

public class Compass : MelonMod
{
    private static MelonLogger.Instance Logger;

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        Settings.Init();
        Logger.Msg("Compass initialized");
    }

    public override void OnLateUpdate() => CompassEnhancer.Tick();

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        Logger.Debug($"Scene loaded: {sceneName}");
        if (sceneName == "Main")
        {
            Logger.Debug("Main scene loaded, waiting for player");
            MelonCoroutines.Start(Utils.WaitForPlayer(DoStuff()));
        }
    }

    // Recon probe: periodically log the vanilla compass state so we can learn how elements behave.
    private IEnumerator DoStuff()
    {
        yield return new WaitForSeconds(10f);
        while (true)
        {
            try { if (Settings.DebugLogging) DumpCompass(); }
            catch (System.Exception e) { Logger.Error($"Probe failed: {e}"); }
            yield return new WaitForSeconds(8f);
        }
    }

    private static void DumpCompass()
    {
        var cm = CompassManager.Instance;
        if (cm == null) { Logger.Msg("[probe] CompassManager.Instance is null"); return; }
        var camPos = cm.cam != null ? cm.cam.position : Vector3.zero;
        Logger.Msg($"[probe] enabled={cm.CompassEnabled} range={cm.CompassUIRange} fullAlphaRange={cm.FullAlphaRange} angleDiv={cm.AngleDivisor} " +
                   $"labelThreshold={CompassManager.DISTANCE_LABEL_THRESHOLD} notches={CompassManager.NOTCH_COUNT} elements={cm.elements.Count} cam={camPos} enhancer(frames={CompassEnhancer.Frames} applied={CompassEnhancer.Applied} failed={CompassEnhancer.Failed} overlayDrawn={Overlay.LastDrawn})");
        foreach (var el in cm.elements)
        {
            if (!el.Visible) continue;
            var t = el.TargetTransform;
            var label = el.DistanceLabel;
            string dist = t != null ? Vector3.Distance(camPos, t.position).ToString("F1") : "n/a";
            Logger.Msg($"[probe]  el target='{(t != null ? t.name : "null")}' pos={(t != null ? t.position.ToString() : "n/a")} dist={dist} " +
                       $"visible={el.Visible} labelText='{(label != null ? label.text : "null")}' labelActive={(label != null && label.gameObject.activeInHierarchy)} " +
                       $"alpha={(el.Group != null ? el.Group.alpha : -1f)}");
        }
    }
}
