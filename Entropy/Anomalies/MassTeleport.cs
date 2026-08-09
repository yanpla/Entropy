using System.Collections;
using System.Linq;
using Entropy.Core;
using UnityEngine;
using Random = System.Random;

namespace Entropy.Anomalies;

/// <summary>
/// Everyone is somewhere else now. Alibis do not survive this.
/// </summary>
public class MassTeleport : Anomaly
{
    public override string Name => "Everyone is somewhere else";

    public override EntropyTier MinTier => EntropyTier.Critical;

    public override bool CanRun() => ShipStatus.Instance.AllVents.Length > 0;

    public override IEnumerator Run(Random rng)
    {
        var vents = ShipStatus.Instance.AllVents.ToArray().OrderBy(vent => vent.Id).ToList();

        foreach (var player in Players.Alive())
        {
            var destination = vents[rng.Next(vents.Count)].transform.position;

            if (player.AmOwner) player.NetTransform.RpcSnapTo(new Vector2(destination.x, destination.y));
        }

        yield break;
    }
}
