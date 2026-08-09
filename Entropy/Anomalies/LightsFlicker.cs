using System.Collections;
using UnityEngine;
using Random = System.Random;

namespace Entropy.Anomalies;

/// <summary>
/// The lights go out for one person. The breakers are fine.
/// </summary>
/// <remarks>
/// This never touches the electrical system, so nobody else darkens and no fix-lights
/// task appears. The target is simply blind for a few seconds in a fully lit ship.
/// </remarks>
public class LightsFlicker : Anomaly
{
    internal const float Radius = 0.35f;

    private static float _blindUntil;

    public override string Name => "The lights flicker";

    public override EntropyTier MinTier => EntropyTier.Stable;

    public override IEnumerator Run(PlayerControl target, Random rng)
    {
        if (!target.AmOwner) yield break;

        // Two or three stutters rather than one clean outage, so it reads as a fault.
        var flickers = rng.Next(2, 4);

        for (var i = 0; i < flickers; i++)
        {
            var darkness = (float)(rng.NextDouble() * 1.5d + 0.5d);
            _blindUntil = Time.time + darkness;

            yield return new WaitForSeconds(darkness);
            yield return new WaitForSeconds((float)(rng.NextDouble() * 0.6d + 0.2d));
        }
    }

    internal static bool Blind => Time.time < _blindUntil;
}
