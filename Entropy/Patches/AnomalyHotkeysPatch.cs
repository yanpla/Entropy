using Entropy.Anomalies;
using HarmonyLib;
using Reactor.Utilities;
using UnityEngine;

namespace Entropy.Patches;

// Host testing shortcuts: number keys 1–8 trigger anomalies in registry order.
[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class AnomalyHotkeysPatch
{
    public static void Postfix(HudManager __instance)
    {
        if (!AmongUsClient.Instance.AmHost || !AmongUsClient.Instance.IsGameStarted) return;
        if (!ShipStatus.Instance || !PlayerControl.LocalPlayer || MeetingHud.Instance) return;

        // Otherwise typing a number into chat sets off an anomaly.
        if (__instance.Chat && __instance.Chat.IsOpenOrOpening) return;

        for (var index = 0; index < AnomalyManager.All.Length && index < 9; index++)
        {
            if (!Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + index))) continue;

            var anomaly = AnomalyManager.All[index];

            Logger<EntropyPlugin>.Info($"Hotkey {index + 1} fired {anomaly.Name}");
            AnomalyManager.Fire(anomaly, PlayerControl.LocalPlayer);
        }
    }
}
