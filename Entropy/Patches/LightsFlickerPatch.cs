using System.Collections.Generic;
using System.Reflection;
using Entropy.Anomalies;
using HarmonyLib;
using UnityEngine;

namespace Entropy.Patches;

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
