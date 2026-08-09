using System.Collections.Generic;
using System.Linq;
using Random = System.Random;

namespace Entropy.Anomalies;

/// <summary>
/// Shared player lookups for anomalies.
/// </summary>
/// <remarks>
/// Everything is ordered by player id. Anomalies pick from these lists using a shared
/// seed, so the ordering has to be identical on every client - and
/// <see cref="PlayerControl.AllPlayerControls"/> is not.
/// </remarks>
public static class Players
{
    /// <summary>Deaths the crew has been shown, which is every death up to a round start.</summary>
    private static readonly HashSet<byte> Buried = [];

    public static List<PlayerControl> Alive() => PlayerControl.AllPlayerControls.ToArray()
        .Where(player => player && player.Data is { IsDead: false, Disconnected: false })
        .OrderBy(player => player.PlayerId)
        .ToList();

    /// <summary>
    /// Everyone the crew still believes is walking around: the living, plus anyone who
    /// has died since the last meeting without being found.
    /// </summary>
    /// <remarks>
    /// A meeting hud lists the dead, so a death stops being a secret the moment one is
    /// called - and an ejection is watched by everybody. Both are settled by the time the
    /// next round starts, which is when <see cref="Bury"/> writes them off.
    /// </remarks>
    public static List<PlayerControl> PresumedAlive() => PlayerControl.AllPlayerControls.ToArray()
        .Where(player => player
            && player.Data is { Disconnected: false }
            && (!player.Data.IsDead || !Buried.Contains(player.PlayerId)))
        .OrderBy(player => player.PlayerId)
        .ToList();

    /// <summary>
    /// Marks every death so far as common knowledge. Called when a round begins, which is
    /// the moment after everybody has seen the meeting and the ejection.
    /// </summary>
    public static void Bury()
    {
        Buried.Clear();

        foreach (var player in PlayerControl.AllPlayerControls.ToArray()
                     .Where(player => player && player.Data is { IsDead: true }))
        {
            Buried.Add(player.PlayerId);
        }
    }

    public static PlayerControl? ById(byte playerId) => PlayerControl.AllPlayerControls.ToArray()
        .FirstOrDefault(player => player && player.PlayerId == playerId);

    /// <summary>Removes and returns a random entry, so callers can draw without repeats.</summary>
    public static T Draw<T>(this List<T> items, Random rng)
    {
        var index = rng.Next(items.Count);
        var item = items[index];
        items.RemoveAt(index);

        return item;
    }
}
