using System.Collections;
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

    public override IEnumerator Run(PlayerControl target, Random rng)
    {
        // Only the owning client moves its own player; the snap networks from there.
        if (!target.AmOwner) yield break;

        // Nowhere clear to arrive: staying put beats landing through the floor.
        if (Placement.Find(rng) is not { } spot) yield break;

        target.NetTransform.RpcSnapTo(spot);
    }
}
