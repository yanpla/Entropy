using Reactor.Utilities;
using UnityEngine;
using Random = System.Random;

namespace Entropy.Utilities;

// Caches a walkable grid flood-filled from the local player; empty space alone does not prove reachability.
public static class Placement
{
    private const float Step = 0.5f;

    private const float Clearance = 0.3f;

    private const int MaxSpots = 20000; // Bound the fill on maps with open geometry.

    private static ShipStatus? _mapped;
    private static List<Vector2> _spots = [];

    private static HashSet<(int X, int Y)> _cells = [];
    private static Vector2 _anchor;

    public static Vector2? Find(Random rng, Vector2? near = null, float minDistance = 0f, float maxDistance = 0f)
    {
        var spots = Reachable();

        if (near is { } origin)
        {
            spots = spots
                .Where(spot =>
                {
                    var distance = Vector2.Distance(spot, origin);
                    return distance >= minDistance && distance <= maxDistance;
                })
                .ToList();
        }

        return spots.Count == 0 ? null : spots[rng.Next(spots.Count)];
    }

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

            foreach (var next in Neighbours(cell))
            {
                if (!_cells.Contains(next) || !seen.Add(next)) continue;

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

            var spot = World(cell);
            if (Blocked(spot)) continue;

            _cells.Add(cell);
            _spots.Add(spot);

            foreach (var next in Neighbours(cell))
            {
                if (seen.Add(next)) queue.Enqueue(next);
            }
        }
    }

    // Cardinal steps prevent diagonal corner-cutting.
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

    // Ignore room triggers and doors so closed doors do not permanently exclude rooms.
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
