using System.Collections;
using Entropy.Core;
using UnityEngine;
using Random = System.Random;

namespace Entropy.Anomalies;

/// <summary>
/// Some of the breakers trip on their own and reset a few seconds later.
/// </summary>
public class LightsFlicker : Anomaly
{
    public override string Name => "The lights flicker";

    public override EntropyTier MinTier => EntropyTier.Stable;

    public override bool CanRun() => ShipStatus.Instance.Systems.ContainsKey(SystemTypes.Electrical);

    public override IEnumerator Run(Random rng)
    {
        // The switch system xors the mask it is given, so sending the same mask twice
        // flips the breakers off and then back on.
        var breakers = (byte)(rng.Next(1, 1 << SwitchSystem.NumSwitches) | SwitchSystem.DamageSystem);

        if (AmongUsClient.Instance.AmHost)
        {
            ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Electrical, breakers);
        }

        yield return new WaitForSeconds(5f);

        if (AmongUsClient.Instance.AmHost && ShipStatus.Instance)
        {
            ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Electrical, breakers);
        }
    }
}
