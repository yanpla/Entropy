using Entropy.Anomalies;
using HarmonyLib;
using Reactor.Utilities;
using UnityEngine;

namespace Entropy.Patches;

/// <summary>
/// Number keys fire the matching anomaly at yourself, for testing.
/// </summary>
/// <remarks>
/// Anomalies are rare on purpose and most of them only happen to one person, which makes
/// waiting for one a poor way to find out whether it works. The keys follow
/// <see cref="AnomalyRegistry.All"/> in order, so 1 is the first entry in that list.
/// <para>
/// Host only, because firing one is the host's job - a client pressing a key would send
/// an anomaly everybody else ignores. This is a debug tool and it is not hidden behind
/// anything, so anyone hosting a real lobby with this build can set off whatever they
/// like by leaning on the number row.
/// </para>
/// </remarks>
[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class AnomalyHotkeysPatch
{
    public static void Postfix(HudManager __instance)
    {
        if (!AmongUsClient.Instance.AmHost || !AmongUsClient.Instance.IsGameStarted) return;
        if (!ShipStatus.Instance || !PlayerControl.LocalPlayer || MeetingHud.Instance) return;

        // Otherwise typing a number into chat sets off an anomaly.
        if (__instance.Chat && __instance.Chat.IsOpenOrOpening) return;

        for (var index = 0; index < AnomalyRegistry.All.Length && index < 9; index++)
        {
            if (!Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + index))) continue;

            var anomaly = AnomalyRegistry.All[index];

            Logger<EntropyPlugin>.Info($"Hotkey {index + 1} fired {anomaly.Name}");
            AnomalyDirector.Fire(anomaly, PlayerControl.LocalPlayer);
        }
    }
}
