using System.Collections;
using Entropy.Core;
using UnityEngine;
using Random = System.Random;

namespace Entropy.Anomalies;

/// <summary>
/// The alarm one player hears for a sabotage that never happened.
/// </summary>
/// <remarks>
/// Replaces triggering a real sabotage, which is shared state and so cannot happen to
/// one person. The target gets the sound and the red flash; the ship is untouched, the
/// reactor is fine, and nobody else has anything to fix.
/// </remarks>
public class FakeSabotage : Anomaly
{
    public override string Name => "The ship turns on you";

    public override EntropyTier MinTier => EntropyTier.Volatile;

    public override IEnumerator Run(PlayerControl target, Random rng)
    {
        if (!target.AmOwner) yield break;

        var hud = HudManager.Instance;
        if (!hud) yield break;

        if (ShipStatus.Instance.SabotageSound) SoundManager.Instance.PlaySound(ShipStatus.Instance.SabotageSound, false);

        hud.StartReactorFlash();

        yield return new WaitForSeconds((float)(rng.NextDouble() * 4d + 3d));

        hud.StopReactorFlash();
    }
}
