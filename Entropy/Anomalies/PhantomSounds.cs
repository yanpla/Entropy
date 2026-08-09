using System.Collections;
using Entropy.Core;
using UnityEngine;
using Random = System.Random;

namespace Entropy.Anomalies;

/// <summary>
/// Vents that nobody used. The sound is real, the vent is not.
/// </summary>
public class PhantomSounds : Anomaly
{
    public override string Name => "You hear something";

    public override EntropyTier MinTier => EntropyTier.Unstable;

    public override bool CanRun() => ShipStatus.Instance.VentEnterSound;

    public override IEnumerator Run(Random rng)
    {
        var bursts = rng.Next(2, 5);

        for (var i = 0; i < bursts; i++)
        {
            var clip = rng.Next(2) == 0 ? ShipStatus.Instance.VentEnterSound : ShipStatus.Instance.VentExitSound;
            SoundManager.Instance.PlaySound(clip, false, 0.7f);

            yield return new WaitForSeconds((float)(rng.NextDouble() * 2.5f + 0.5f));
        }
    }
}
