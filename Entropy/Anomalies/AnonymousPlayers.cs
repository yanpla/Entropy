using System.Collections;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace Entropy.Anomalies;

/// <summary>
/// One player stops being able to read anyone's name.
/// </summary>
/// <remarks>
/// Name visibility is drawn per client, so this costs the target every alibi they were
/// keeping track of while everyone around them carries on naming each other.
/// </remarks>
public class AnonymousPlayers : Anomaly
{
    private const float Duration = 25f;

    public override string Name => "Nobody remembers your name";

    public override EntropyTier MinTier => EntropyTier.Unstable;

    public override IEnumerator Run(PlayerControl target, Random rng)
    {
        if (!target.AmOwner) yield break;

        SetNamesVisible(false);

        yield return new WaitForSeconds(Duration);

        SetNamesVisible(true);
    }

    private static void SetNamesVisible(bool visible)
    {
        foreach (var player in PlayerControl.AllPlayerControls.ToArray().Where(player => player && player.cosmetics))
        {
            player.cosmetics.ToggleNameVisible(visible);
        }
    }
}
