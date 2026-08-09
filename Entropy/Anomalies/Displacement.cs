using System.Collections;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace Entropy.Anomalies;

/// <summary>
/// One player is somewhere else now.
/// </summary>
/// <remarks>
/// The only anomaly others can actually witness, which is the point: once one real
/// displacement has happened, every hallucination anyone reports becomes plausible.
/// </remarks>
public class Displacement : Anomaly
{
    public override string Name => "You are somewhere else";

    public override EntropyTier MinTier => EntropyTier.Critical;

    public override bool CanRun(PlayerControl target) => ShipStatus.Instance.AllVents.Length > 0;

    public override IEnumerator Run(PlayerControl target, Random rng)
    {
        var vents = ShipStatus.Instance.AllVents.ToArray().OrderBy(vent => vent.Id).ToList();
        var destination = vents[rng.Next(vents.Count)].transform.position;

        // Only the owning client moves its own player; the snap networks from there.
        if (target.AmOwner) target.NetTransform.RpcSnapTo(new Vector2(destination.x, destination.y));

        yield break;
    }
}
