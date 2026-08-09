using System.Collections;
using Entropy.Core;
using Random = System.Random;

namespace Entropy.Anomalies;

/// <summary>
/// Two players trade places, with no say in the matter.
/// </summary>
public class PlayerSwap : Anomaly
{
    public override string Name => "Two of you have been rearranged";

    public override EntropyTier MinTier => EntropyTier.Unstable;

    public override bool CanRun() => Players.Alive().Count >= 2;

    public override IEnumerator Run(Random rng)
    {
        var candidates = Players.Alive();
        var first = candidates.Draw(rng);
        var second = candidates.Draw(rng);

        var firstPosition = first.transform.position;
        var secondPosition = second.transform.position;

        // Each client moves only the player it owns; the snap is networked from there.
        if (first.AmOwner) first.NetTransform.RpcSnapTo(secondPosition);
        if (second.AmOwner) second.NetTransform.RpcSnapTo(firstPosition);

        yield break;
    }
}
