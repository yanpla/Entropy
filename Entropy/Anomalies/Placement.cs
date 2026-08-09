using System.Collections.Generic;
using System.Linq;
using Reactor.Utilities;
using UnityEngine;
using Random = System.Random;

namespace Entropy.Anomalies;

/// <summary>
/// Somewhere on the map a thing can be put.
/// </summary>
/// <remarks>
/// Whether a point is on the map is a question about reachability, not geometry, and
/// that is why testing colliders never worked. Outside the ship there is nothing to
/// collide with, so empty space out there looks exactly as clear as an empty corridor;
/// room areas reach through walls and hallways can be catch-alls, so containment says
/// yes to places nobody can stand. No local test can tell the two apart.
/// <para>
/// AmongUs-Pathfinder solves it by flood filling from a point known to be inside and
/// keeping only what the fill reaches. That is what this does: walk outwards from a
/// living player, one step at a time, stopping at anything solid. The walls enclose the
/// ship, so the fill cannot escape it, and everything it reaches is somewhere a player
/// could have walked to.
/// </para>
/// </remarks>
public static class Placement
{
    /// <summary>Spacing of the lattice. Fine enough to fit through doorways.</summary>
    private const float Step = 0.5f;

    /// <summary>How much empty space a thing needs so it isn't half inside a wall.</summary>
    private const float Clearance = 0.3f;

    /// <summary>Stops a runaway fill if the map ever turns out not to be enclosed.</summary>
    private const int MaxSpots = 20000;

    private static ShipStatus? _mapped;
    private static List<Vector2> _spots = [];

    /// <summary>
    /// A random spot a player could have walked to, or null if nothing is in range.
    /// </summary>
    /// <param name="rng">The anomaly's seeded source.</param>
    /// <param name="near">
    /// Where to look around. Leave it out to land anywhere on the map instead.
    /// </param>
    /// <param name="minDistance">Closest it may be to <paramref name="near"/>.</param>
    /// <param name="maxDistance">Furthest it may be from <paramref name="near"/>.</param>
    public static Vector2? Find(Random rng, Vector2? near = null, float minDistance = 0f, float maxDistance = 0f)
    {
        var spots = Reachable();

        if (near is { } origin)
        {
            spots = spots
                .Where(spot => Vector2.Distance(spot, origin) is var away
                    && away >= minDistance
                    && away <= maxDistance)
                .ToList();
        }

        return spots.Count == 0 ? null : spots[rng.Next(spots.Count)];
    }

    private static List<Vector2> Reachable()
    {
        if (_mapped == ShipStatus.Instance && _spots.Count > 0) return _spots;
        if (!ShipStatus.Instance || !PlayerControl.LocalPlayer) return [];

        _mapped = ShipStatus.Instance;
        _spots = Fill(PlayerControl.LocalPlayer.GetTruePosition());

        Logger<EntropyPlugin>.Info($"Placement mapped {_spots.Count} reachable spots");

        return _spots;
    }

    /// <summary>
    /// Every spot reachable from <paramref name="start"/> without passing through
    /// anything solid.
    /// </summary>
    private static List<Vector2> Fill(Vector2 start)
    {
        var spots = new List<Vector2>();
        var seen = new HashSet<(int X, int Y)>();
        var queue = new Queue<(int X, int Y)>();

        seen.Add((0, 0));
        queue.Enqueue((0, 0));

        while (queue.Count > 0 && spots.Count < MaxSpots)
        {
            var cell = queue.Dequeue();
            var spot = start + new Vector2(cell.X * Step, cell.Y * Step);

            if (Blocked(spot)) continue;

            spots.Add(spot);

            // Four directions only. Diagonals would let the fill slip through the corner
            // where two walls meet and escape the ship.
            foreach (var next in new[]
                     {
                         (cell.X + 1, cell.Y), (cell.X - 1, cell.Y),
                         (cell.X, cell.Y + 1), (cell.X, cell.Y - 1),
                     })
            {
                if (seen.Add(next)) queue.Enqueue(next);
            }
        }

        return spots;
    }

    /// <summary>
    /// Whether something solid is sitting on this spot.
    /// </summary>
    /// <remarks>
    /// Triggers are ignored rather than left to the layer mask, because room areas are
    /// themselves triggers on the ship layers and would report the whole map as blocked.
    /// Doors are ignored too: one that happens to be shut while the map is being filled
    /// would wall off every room behind it for the rest of the game.
    /// </remarks>
    private static bool Blocked(Vector2 spot)
    {
        var hits = Physics2D.OverlapCircleAll(spot, Clearance, Constants.ShipAndAllObjectsMask);

        for (var i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];

            if (!hit || hit.isTrigger) continue;
            if (hit.GetComponentInParent<OpenableDoor>()) continue;

            return true;
        }

        return false;
    }
}
