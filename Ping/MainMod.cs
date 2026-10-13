using Compass;
using Il2CppScheduleOne.Networking;
using Il2CppSteamworks;
using MelonLoader;
using SteamNetworkLib;
using SteamNetworkLib.Core;
using UnityEngine;

[assembly: MelonInfo(typeof(Ping.Ping), Ping.BuildInfo.Name, Ping.BuildInfo.Version, Ping.BuildInfo.Author)]
[assembly: MelonColor(1, 255, 0, 0)]
[assembly: MelonGame("TVGS", "Schedule I")]
[assembly: MelonAdditionalDependencies("Compass-IL2CPP")]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]

namespace Ping;

public static class BuildInfo
{
    public const string Name = "Ping";
    public const string Description = "Co-op ping with per-player colors, shown on the vanilla compass";
    public const string Author = "ryanf9802";
    public const string Version = "0.2.4";
}

/// <summary>
/// Press the ping key to mark where you are looking. Your co-op partners get the same marker (colored for you) on their
/// compass and as a through-wall icon, plus a sound. Works without a lobby too: you just ping yourself.
/// Networking rides on the Steam lobby the game already creates, through SteamNetworkLib.
/// </summary>
public class Ping : MelonMod
{
    private static Color[] Palette => Compass.PlayerColors.Palette;

    private SteamNetworkClient _client;
    private bool _networkReady;
    private bool _handlerRegistered;
    private float _nextInitAttempt;
    private float _nextPingAllowed;
    private float _nextLobbyAdoptCheck;
    private readonly System.Collections.Generic.Dictionary<ulong, float> _lastFromSender = new();

    private const float FeetPerUnit = 3.28084f;

    public override void OnInitializeMelon()
    {
        Settings.Init();
        _client = new SteamNetworkClient(new NetworkRules { EnableRelay = true, AcceptOnlyFriends = false });
        LoggerInstance.Msg("Ping initialized");
    }

    public override void OnDeinitializeMelon() => _client?.Dispose();

    public override void OnUpdate()
    {
        try
        {
            TickNetwork();
            if (_networkReady) _client.ProcessIncomingMessages();

            if (Cursor.lockState == CursorLockMode.Locked // in gameplay, not in a menu
                && Time.time >= _nextPingAllowed
                && PingInput.Pressed(Settings.Key))
            {
                _nextPingAllowed = Time.time + Settings.CooldownSeconds;
                SendPing();
            }
        }
        catch (System.Exception e)
        {
            LoggerInstance.Error($"Ping update failed: {e.Message}");
        }
    }

    // --- networking -------------------------------------------------------------------------------------------------

    private void TickNetwork()
    {
        if (!_networkReady)
        {
            if (Time.realtimeSinceStartup < _nextInitAttempt) return;
            if (_client.TryInitialize(out var error))
            {
                _networkReady = true;
                LoggerInstance.Msg("SteamNetworkLib ready");
            }
            else
            {
                _nextInitAttempt = Time.realtimeSinceStartup + 2f;
                return;
            }
        }
        if (!_handlerRegistered)
        {
            _client.RegisterMessageHandler<PingMessage>(OnPingReceived);
            _handlerRegistered = true;
        }

        // The library follows whichever Steam lobby we enter after it initialized. If we were already in the game's lobby
        // by then, join it explicitly (entering a lobby you are in is harmless).
        if (!_client.IsInLobby && Time.realtimeSinceStartup >= _nextLobbyAdoptCheck)
        {
            _nextLobbyAdoptCheck = Time.realtimeSinceStartup + 3f;
            TryAdoptGameLobby();
        }
    }

    private void TryAdoptGameLobby()
    {
        try
        {
            var lobby = Lobby.Instance;
            if (lobby == null || !lobby.IsInLobby || lobby.LobbyID == 0) return;
            _ = _client.JoinLobbyAsync(new CSteamID(lobby.LobbyID));
            LoggerInstance.Msg($"Joining the game's lobby {lobby.LobbyID} for pings");
        }
        catch (System.Exception e)
        {
            LoggerInstance.Warning($"Could not adopt the game lobby: {e.Message}");
        }
    }

    // --- sending ----------------------------------------------------------------------------------------------------

    private void SendPing()
    {
        var cam = Camera.main;
        if (cam == null) return;

        float maxUnits = Settings.MaxDistanceFt / FeetPerUnit;
        int mask = ~(1 << 2); // everything except "Ignore Raycast"
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0) mask &= ~(1 << playerLayer); // and not ourselves
        if (!PingRaycast.Cast(cam.transform.position, cam.transform.forward, maxUnits, mask, out Vector3 point, out Vector3 normal))
            return;

        Vector3 pos = point + normal * 0.15f;
        ulong me = _networkReady ? _client.LocalPlayerId64 : 0UL;
        ShowPing(pos, me, "You", Il2CppScheduleOne.PlayerScripts.Player.Local?.PlayerName);

        if (_networkReady && _client.IsInLobby)
        {
            _ = _client.BroadcastMessageAsync(new PingMessage(new PingPayload { X = pos.x, Y = pos.y, Z = pos.z, Name = Il2CppScheduleOne.PlayerScripts.Player.Local?.PlayerName }));
        }
    }

    // --- receiving --------------------------------------------------------------------------------------------------

    private void OnPingReceived(PingMessage message, CSteamID sender)
    {
        try
        {
            ulong id = sender.m_SteamID;
            if (id == _client.LocalPlayerId64) return;
            var p = message.Payload;
            if (p == null || !IsFinite(p.X) || !IsFinite(p.Y) || !IsFinite(p.Z)) return;

            // Basic flood protection: one ping per sender per cooldown window.
            float now = Time.time;
            if (_lastFromSender.TryGetValue(id, out float last) && now - last < 0.15f) return;
            _lastFromSender[id] = now;

            // The in-game name is what the compass markers and colors are keyed on; the Steam name may differ.
            string gameName = CleanName(p.Name);
            string steamName = _client.TryGetMember(id, out var member) && member != null ? member.DisplayName : "Partner";
            ShowPing(new Vector3(p.X, p.Y, p.Z), id, gameName ?? steamName, gameName);
        }
        catch (System.Exception e)
        {
            LoggerInstance.Error($"Bad ping from {sender.m_SteamID}: {e.Message}");
        }
    }

    private static bool IsFinite(float f) => !float.IsNaN(f) && !float.IsInfinity(f) && Mathf.Abs(f) < 100000f;

    // --- presenting -------------------------------------------------------------------------------------------------

    /// <summary>Names come from other players' machines: bound the length and drop characters that could inject rich-text tags.</summary>
    private static string CleanName(string n)
    {
        if (string.IsNullOrWhiteSpace(n)) return null;
        n = n.Replace("<", "").Replace(">", "").Trim();
        if (n.Length > 32) n = n.Substring(0, 32);
        return n.Length == 0 ? null : n;
    }

    private void ShowPing(Vector3 pos, ulong playerId, string label, string inGameName)
    {
        // Same color as that player's compass marker: ranked by in-game name; fall back to Steam ID rank if the name is unknown.
        int index = Compass.PlayerColors.IndexFor(inGameName);
        if (index < 0) index = ColorIndex(playerId);
        CompassApi.AddMarker(pos, Palette[index], Settings.LifetimeSeconds, label, Settings.OnCompass, Settings.OnOverlay);
        PingSound.Play(index, pos);
    }

    /// <summary>Same color for the same player on every machine: rank of the Steam ID among current lobby members.</summary>
    private int ColorIndex(ulong playerId)
    {
        if (!_networkReady || !_client.IsInLobby || playerId == 0) return 0;
        var ids = new System.Collections.Generic.List<ulong>();
        foreach (var m in _client.GetLobbyMembers()) ids.Add(m.SteamId64);
        ids.Sort();
        int i = ids.IndexOf(playerId);
        return i < 0 ? (int)(playerId % (ulong)Palette.Length) : i % Palette.Length;
    }
}
