using MelonLoader;
using UnityEngine;
#if MONO
using FishNet;
#else
using Il2CppFishNet;
#endif

[assembly: MelonInfo(
    typeof(Portraits.Portraits),
    Portraits.BuildInfo.Name,
    Portraits.BuildInfo.Version,
    Portraits.BuildInfo.Author
)]
[assembly: MelonColor(1, 255, 0, 0)]
[assembly: MelonGame("TVGS", "Schedule I")]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]

namespace Portraits;

public static class BuildInfo
{
    public const string Name = "Portraits";
    public const string Description = "Generates player portraits from their live avatar";
    public const string Author = "ryanf9802";
    public const string Version = "0.1.0";
}

/// <summary>
/// Spike: render each player's avatar into a small transparent portrait whenever their look changes, and save it as a PNG under
/// UserData/Portraits so we can judge the result. Triggers: the avatar's OnOutfitChanged / OnNakedAppearanceChanged events,
/// first sight of a player, and a periodic signature check as a safety net.
/// </summary>
public class Portraits : MelonMod
{
    private float _nextScan;

    public override void OnInitializeMelon() => LoggerInstance.Msg("Portraits initialized");

    public override void OnUpdate()
    {
        try { PortraitTracker.Update(LoggerInstance); }
        catch (System.Exception e) { LoggerInstance.Error($"PortraitTracker failed: {e}"); }
    }
}
