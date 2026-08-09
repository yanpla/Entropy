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
    public static List<PlayerControl> Alive() => PlayerControl.AllPlayerControls.ToArray()
        .Where(player => player && player.Data is { IsDead: false, Disconnected: false })
        .OrderBy(player => player.PlayerId)
        .ToList();

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
