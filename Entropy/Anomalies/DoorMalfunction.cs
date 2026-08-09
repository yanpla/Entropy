using System.Collections;
using System.Linq;
using Entropy.Core;
using Random = System.Random;

namespace Entropy.Anomalies;

/// <summary>
/// Doors slam shut in rooms nobody sabotaged.
/// </summary>
public class DoorMalfunction : Anomaly
{
    public override string Name => "The doors have a mind of their own";

    public override EntropyTier MinTier => EntropyTier.Stable;

    public override bool CanRun() => ShipStatus.Instance.AllDoors.Count > 0;

    public override IEnumerator Run(Random rng)
    {
        var rooms = ShipStatus.Instance.AllDoors.ToArray()
            .Select(door => door.Room)
            .Distinct()
            .OrderBy(room => (int)room)
            .ToList();

        var count = System.Math.Min(rng.Next(1, 4), rooms.Count);

        for (var i = 0; i < count; i++)
        {
            var room = rooms.Draw(rng);

            if (AmongUsClient.Instance.AmHost) ShipStatus.Instance.RpcCloseDoorsOfType(room);
        }

        yield break;
    }
}
