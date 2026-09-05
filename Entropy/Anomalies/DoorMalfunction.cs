using System.Collections;
using UnityEngine;
using Random = System.Random;

namespace Entropy.Anomalies;

// Closes nearby doors locally, then reopens them after six seconds.
public class DoorMalfunction : Anomaly
{
    private const float Duration = 6f;

    public override string Name => "The doors have a mind of their own";

    public override float MinEntropy => 0f;

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
