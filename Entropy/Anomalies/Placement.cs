using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace Entropy.Anomalies;

/// <summary>
/// Finding somewhere on the map that a thing can plausibly be.
/// </summary>
/// <remarks>
/// Anomalies put bodies, strangers and players onto the floor, and all of them need the
/// same guarantee: inside the map, and not inside the scenery. Rooms are what make that
/// checkable - a room collider is an area the map promises is part of itself, whereas a
/// bounding box, a vent or an offset from a player is only a point that might be.
/// </remarks>
public static class Placement
{
    /// <summary>How much empty space a thing needs so it isn't half inside a wall.</summary>
    private const float Clearance = 0.4f;

    private const int Attempts = 25;

    /// <summary>
    /// A random spot with solid floor under it, or null if nothing suitable turned up.
    /// </summary>
    /// <param name="rng">The anomaly's seeded source.</param>
    /// <param name="near">
    /// Where to look around. Leave it out to land anywhere on the map instead.
    /// </param>
    /// <param name="minDistance">Closest it may be to <paramref name="near"/>.</param>
    /// <param name="maxDistance">Furthest it may be from <paramref name="near"/>.</param>
    public static Vector2? Find(Random rng, Vector2? near = null, float minDistance = 0f, float maxDistance = 0f)
    {
        if (!ShipStatus.Instance) return null;

        var rooms = ShipStatus.Instance.AllRooms.ToArray().Where(room => room && room.roomArea).ToList();
        if (rooms.Count == 0) return null;

        // Hallways count as map, but they are long thin diagonal things whose bounding
        // box is mostly outside them, so they make poor places to aim for.
        var destinations = rooms.Where(room => room.RoomId != SystemTypes.Hallway).ToList();
        if (destinations.Count == 0) destinations = rooms;

        for (var attempt = 0; attempt < Attempts; attempt++)
        {
            var spot = near is { } origin
                ? origin + Offset(rng, minDistance, maxDistance)
                : Somewhere(rng, destinations[rng.Next(destinations.Count)]);

            if (!rooms.Any(room => room.roomArea.OverlapPoint(spot))) continue;
            if (Physics2D.OverlapCircle(spot, Clearance, Constants.ShipAndObjectsMask)) continue;

            return spot;
        }

        return null;
    }

    private static Vector2 Offset(Random rng, float minDistance, float maxDistance)
    {
        var angle = (float)(rng.NextDouble() * Math.PI * 2d);
        var reach = (float)(rng.NextDouble() * (maxDistance - minDistance) + minDistance);

        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * reach;
    }

    private static Vector2 Somewhere(Random rng, PlainShipRoom room)
    {
        var bounds = room.roomArea.bounds;

        return new Vector2(
            (float)(rng.NextDouble() * bounds.size.x + bounds.min.x),
            (float)(rng.NextDouble() * bounds.size.y + bounds.min.y));
    }
}
