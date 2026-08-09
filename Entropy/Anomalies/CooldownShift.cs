using System.Collections;
using Entropy.Core;
using Random = System.Random;

namespace Entropy.Anomalies;

/// <summary>
/// Every impostor's kill cooldown is rerolled - shorter for some, longer for others.
/// </summary>
public class CooldownShift : Anomaly
{
    public override string Name => "Time slips";

    public override EntropyTier MinTier => EntropyTier.Stable;

    public override IEnumerator Run(Random rng)
    {
        // Drawn for every impostor in id order so each client agrees on who got what,
        // even though only one of them can act on it.
        foreach (var player in Players.Alive())
        {
            var cooldown = (float)(rng.NextDouble() * 30f + 2f);

            if (player.AmOwner && player.Data.Role.IsImpostor) player.SetKillTimer(cooldown);
        }

        yield break;
    }
}
