using SteamNetworkLib.Models;

namespace Ping;

/// <summary>What a ping carries: a world position and the sender's in-game name. The name is cosmetic (label and color); the sender's real identity comes from Steam.</summary>
public class PingPayload
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    /// <summary>The sender's in-game player name, so receivers can color the ping like that player's compass marker. Null from older versions.</summary>
    public string Name { get; set; }
}

public class PingMessage : TypedP2PMessage<PingPayload>
{
    public override string MessageType => "S1VANILLAPLUS_PING";

    public PingMessage() { }
    public PingMessage(PingPayload payload) : base(payload) { }
}
