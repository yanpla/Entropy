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

    /// <summary>The lattice the fill reached, and where its origin sits in the world.</summary>
    private static HashSet<(int X, int Y)> _cells = [];
    private static Vector2 _anchor;

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

    /// <summary>
    /// Waypoints from one spot to another that never cross anything solid, or null when
    /// there is no way round.
    /// </summary>
    /// <remarks>
    /// A breadth first search over the same lattice the fill produced. It is walking the
    /// exact set of cells a player could have reached on foot, so any route it returns is
    /// one a player could have taken.
    /// </remarks>
    public static List<Vector2>? Route(Vector2 from, Vector2 to)
    {
        if (Reachable().Count == 0) return null;

        var start = Cell(from);
        var goal = Cell(to);

        if (!_cells.Contains(start) || !_cells.Contains(goal)) return null;

        var cameFrom = new Dictionary<(int X, int Y), (int X, int Y)>();
        var seen = new HashSet<(int X, int Y)> { start };
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();

            if (cell == goal) return Retrace(cameFrom, start, goal);

            foreach (var next in Neighbours(cell).Where(next => _cells.Contains(next) && seen.Add(next)))
            {
                cameFrom[next] = cell;
                queue.Enqueue(next);
            }
        }

        return null;
    }

    private static List<Vector2> Retrace(
        Dictionary<(int X, int Y), (int X, int Y)> cameFrom,
        (int X, int Y) start,
        (int X, int Y) goal)
    {
        var route = new List<Vector2>();

        for (var cell = goal; cell != start; cell = cameFrom[cell]) route.Add(World(cell));

        route.Reverse();

        return route;
    }

    private static List<Vector2> Reachable()
    {
        if (_mapped == ShipStatus.Instance && _spots.Count > 0) return _spots;
        if (!ShipStatus.Instance || !PlayerControl.LocalPlayer) return [];

        _mapped = ShipStatus.Instance;
        _anchor = PlayerControl.LocalPlayer.GetTruePosition();
        Fill();

        Logger<EntropyPlugin>.Info($"Placement mapped {_spots.Count} reachable spots");

        return _spots;
    }

    /// <summary>
    /// Every spot reachable from <see cref="_anchor"/> without passing through anything
    /// solid.
    /// </summary>
    private static void Fill()
    {
        _spots = [];
        _cells = [];

        var seen = new HashSet<(int X, int Y)> { (0, 0) };
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue((0, 0));

        while (queue.Count > 0 && _spots.Count < MaxSpots)
        {
            var cell = queue.Dequeue();

            if (Blocked(World(cell))) continue;

            _cells.Add(cell);
            _spots.Add(World(cell));

            foreach (var next in Neighbours(cell))
            {
                if (seen.Add(next)) queue.Enqueue(next);
            }
        }
    }

    /// <summary>
    /// Four directions only. Diagonals would let a fill slip through the corner where two
    /// walls meet and escape the ship, and would let a walker cut the same corner.
    /// </summary>
    private static IEnumerable<(int X, int Y)> Neighbours((int X, int Y) cell)
    {
        yield return (cell.X + 1, cell.Y);
        yield return (cell.X - 1, cell.Y);
        yield return (cell.X, cell.Y + 1);
        yield return (cell.X, cell.Y - 1);
    }

    private static Vector2 World((int X, int Y) cell) => _anchor + new Vector2(cell.X * Step, cell.Y * Step);

    private static (int X, int Y) Cell(Vector2 world) => (
        Mathf.RoundToInt((world.x - _anchor.x) / Step),
        Mathf.RoundToInt((world.y - _anchor.y) / Step));

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
