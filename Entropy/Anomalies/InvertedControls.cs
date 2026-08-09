using System.Collections;
using Entropy.Core;
using HarmonyLib;
using UnityEngine;
using Random = System.Random;

namespace Entropy.Anomalies;

/// <summary>
/// Up is down. Everyone gets it at once, so at least it is fair.
/// </summary>
public class InvertedControls : Anomaly
{
    private const float Duration = 15f;

    internal static float ActiveUntil;

    public override string Name => "Your controls betray you";

    public override EntropyTier MinTier => EntropyTier.Volatile;

    public override IEnumerator Run(Random rng)
    {
        ActiveUntil = Time.time + Duration;

        yield break;
    }
}

/// <summary>
/// Flips the local player's movement while <see cref="InvertedControls"/> is running.
/// </summary>
[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.SetNormalizedVelocity))]
public static class InvertedControlsPatch
{
    public static void Prefix(PlayerPhysics __instance, ref Vector2 direction)
    {
        if (Time.time > InvertedControls.ActiveUntil || !__instance.myPlayer.AmOwner) return;

        direction = -direction;
    }
}
