using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Entropy.Core;
using HarmonyLib;
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

/// <summary>
/// Crushes the local player's vision while <see cref="LightsFlicker"/> is running.
/// </summary>
/// <remarks>
/// Airship overrides the calculation, so both implementations are patched.
/// </remarks>
[HarmonyPatch]
public static class LightsFlickerPatch
{
    public static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(ShipStatus), nameof(ShipStatus.CalculateLightRadius));
        yield return AccessTools.Method(typeof(AirshipStatus), nameof(AirshipStatus.CalculateLightRadius));
    }

    public static void Postfix(NetworkedPlayerInfo player, ref float __result)
    {
        if (!LightsFlicker.Blind) return;
        if (player == null || !player.Object || !player.Object.AmOwner) return;

        __result = Mathf.Min(__result, LightsFlicker.Radius);
    }
}
