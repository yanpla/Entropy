using System.Collections;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace Entropy.Anomalies;

/// <summary>
/// The doors nearest one player shut, on that player's screen only.
/// </summary>
/// <remarks>
/// Closed locally, so the collider really does block the target while everyone else
/// walks through an open doorway. They are reopened by us rather than by the door
/// system, which knows nothing about any of this.
/// </remarks>
public class DoorMalfunction : Anomaly
{
    private const float Duration = 6f;

    public override string Name => "The doors have a mind of their own";

    public override EntropyTier MinTier => EntropyTier.Stable;

    public override bool CanRun(PlayerControl target) => ShipStatus.Instance.AllDoors.Count > 0;

    public override IEnumerator Run(PlayerControl target, Random rng)
    {
        if (!target.AmOwner) yield break;

        var here = target.GetTruePosition();
        var doors = ShipStatus.Instance.AllDoors.ToArray()
            .Where(door => door && door.IsOpen)
            .OrderBy(door => Vector2.Distance(here, door.transform.position))
            .Take(rng.Next(2, 5))
            .ToList();

        foreach (var door in doors) door.SetDoorway(false);

        yield return new WaitForSeconds(Duration);

        foreach (var door in doors.Where(door => door)) door.SetDoorway(true);
    }
}
