using SteamNetworkLib.Models;

namespace Ping;

/// <summary>What a ping carries: just a world position. Sender identity comes from Steam, not from the payload.</summary>
public class PingPayload
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
}

public class PingMessage : TypedP2PMessage<PingPayload>
{
    public override string MessageType => "S1VANILLAPLUS_PING";

    public PingMessage() { }
    public PingMessage(PingPayload payload) : base(payload) { }
}
