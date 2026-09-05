using System.Collections;
using UnityEngine;
using Random = System.Random;

namespace Entropy.Anomalies;

// Plays a local sabotage alarm and reactor flash.
public class FakeSabotage : Anomaly
{
    public override string Name => "The ship turns on you";

    public override float MinEntropy => 50f;

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
