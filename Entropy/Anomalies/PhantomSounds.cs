using System.Collections;
using UnityEngine;
using Random = System.Random;

namespace Entropy.Anomalies;

/// <summary>
/// Vents nobody used, heard by one person.
/// </summary>
public class PhantomSounds : Anomaly
{
    public override string Name => "You hear something";

    public override EntropyTier MinTier => EntropyTier.Unstable;

    public override bool CanRun(PlayerControl target) => ShipStatus.Instance.VentEnterSound;

    public override IEnumerator Run(PlayerControl target, Random rng)
    {
        if (!target.AmOwner) yield break;

        var bursts = rng.Next(2, 5);

        for (var i = 0; i < bursts; i++)
        {
            var clip = rng.Next(2) == 0 ? ShipStatus.Instance.VentEnterSound : ShipStatus.Instance.VentExitSound;
            SoundManager.Instance.PlaySound(clip, false, 0.7f);

            yield return new WaitForSeconds((float)(rng.NextDouble() * 2.5d + 0.5d));
        }
    }
}
