using System.Collections;
using System.Linq;
using Entropy.Core;
using UnityEngine;
using Random = System.Random;

namespace Entropy.Anomalies;

/// <summary>
/// Names come off. For a while, everyone is just a colour.
/// </summary>
public class AnonymousPlayers : Anomaly
{
    private const float Duration = 25f;

    public override string Name => "Nobody remembers your name";

    public override EntropyTier MinTier => EntropyTier.Unstable;

    public override IEnumerator Run(Random rng)
    {
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
