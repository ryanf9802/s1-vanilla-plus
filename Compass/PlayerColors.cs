using UnityEngine;

namespace Compass;

/// <summary>
/// One color per player, the same on every machine: the rank of the player's name among everyone currently in the game,
/// alphabetically. Compass uses it for player markers and Ping for pings, so a player's marker and pings match.
/// </summary>
public static class PlayerColors
{
    public static readonly Color[] Palette =
    {
        new(0.33f, 0.67f, 1.00f), new(1.00f, 0.35f, 0.35f), new(0.43f, 0.86f, 0.47f), new(0.75f, 0.47f, 1.00f),
        new(1.00f, 0.63f, 0.24f), new(0.31f, 0.90f, 0.90f), new(1.00f, 0.51f, 0.78f), new(0.92f, 0.92f, 0.92f),
    };

    /// <summary>Palette index for <paramref name="name"/>, or -1 when no such player is in the game.</summary>
    public static int IndexFor(string name)
    {
        if (string.IsNullOrEmpty(name)) return -1;
        var list = Il2CppScheduleOne.PlayerScripts.Player.PlayerList;
        if (list == null) return -1;
        var names = new System.Collections.Generic.List<string>();
        foreach (var p in list)
        {
            string n = p?.PlayerName;
            if (!string.IsNullOrEmpty(n)) names.Add(n);
        }
        names.Sort(System.StringComparer.Ordinal);
        int i = names.IndexOf(name);
        return i < 0 ? -1 : i % Palette.Length;
    }

    public static Color ColorFor(string name)
    {
        int i = IndexFor(name);
        return Palette[i < 0 ? 0 : i];
    }
}
