using Entropy.Utilities;
using System.Collections;
using Random = System.Random;

namespace Entropy.Anomalies;

// Teleports the target; the owning client networks the new position.
public class Displacement : Anomaly
{
    public override string Name => "You are somewhere else";

    public override float MinEntropy => 75f;

    public override IEnumerator Run(PlayerControl target, Random rng)
    {
        // Only the owning client moves its own player; the snap networks from there.
        if (!target.AmOwner) yield break;

        // Nowhere clear to arrive: staying put beats landing through the floor.
        if (Placement.Find(rng) is not { } spot) yield break;

        target.NetTransform.RpcSnapTo(spot);
    }
}
