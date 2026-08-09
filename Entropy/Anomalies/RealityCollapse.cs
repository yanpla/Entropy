using System.Collections;
using System.Linq;
using Entropy.Core;
using UnityEngine;
using Random = System.Random;

namespace Entropy.Anomalies;

/// <summary>
/// What happens when the meter fills. Everything at once, then the meter empties and
/// the game starts building again from nothing.
/// </summary>
public class RealityCollapse : Anomaly
{
    public override string Name => "REALITY COLLAPSE";

    public override EntropyTier MinTier => EntropyTier.Critical;

    /// <summary>Only fires at a full meter, never on a roll.</summary>
    public override bool Scheduled => false;

    public override IEnumerator Run(Random rng)
    {
        var host = AmongUsClient.Instance.AmHost;

        // Everyone lands in the same place, in the dark, behind closed doors.
        if (ShipStatus.Instance.AllVents.Length > 0)
        {
            var vents = ShipStatus.Instance.AllVents.ToArray().OrderBy(vent => vent.Id).ToList();
            var epicentre = vents[rng.Next(vents.Count)].transform.position;

            foreach (var player in Players.Alive().Where(player => player.AmOwner))
            {
                player.NetTransform.RpcSnapTo(new Vector2(epicentre.x, epicentre.y));
            }
        }

        if (host && ShipStatus.Instance.Systems.ContainsKey(SystemTypes.Electrical))
        {
            ShipStatus.Instance.RpcUpdateSystem(
                SystemTypes.Electrical,
                (byte)(SwitchSystem.DamageSystem | SwitchSystem.SwitchesMask));
        }

        yield return new WaitForSeconds(0.4f);

        if (host)
        {
            foreach (var room in ShipStatus.Instance.AllDoors.ToArray().Select(door => door.Room).Distinct())
            {
                ShipStatus.Instance.RpcCloseDoorsOfType(room);
            }
        }
    }
}
