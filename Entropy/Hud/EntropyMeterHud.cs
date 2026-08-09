using Entropy.Core;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace Entropy.Hud;

/// <summary>
/// Draws the meter at the top of the screen: STABLE ███░░░░░░░ CHAOTIC.
/// </summary>
/// <remarks>
/// The bar is built from TMP <c>mark</c> highlights over blank space rather than block
/// characters, because the vanilla font has no glyphs for those.
/// </remarks>
[HarmonyPatch(typeof(HudManager))]
public static class EntropyMeterHud
{
    private const int Segments = 20;
    private const string Blank = " "; // nbsp; TMP trims plain spaces at line edges

    private static TextMeshPro? _meter;

    [HarmonyPostfix]
    [HarmonyPatch(nameof(HudManager.Start))]
    public static void CreateMeter(HudManager __instance)
    {
        _meter = UnityEngine.Object.Instantiate(__instance.TaskPanel.taskText, __instance.transform);
        _meter.gameObject.name = "EntropyMeter";
        _meter.transform.localPosition = new Vector3(0f, 2.6f, 0f);
        _meter.rectTransform.sizeDelta = new Vector2(6f, 0.5f);
        _meter.alignment = TextAlignmentOptions.Center;
        _meter.fontSize = _meter.fontSizeMin = _meter.fontSizeMax = 1.6f;
        _meter.gameObject.SetActive(false);
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(HudManager.Update))]
    public static void RefreshMeter()
    {
        if (!_meter) return;

        var inGame = ShipStatus.Instance && PlayerControl.LocalPlayer && !MeetingHud.Instance;
        _meter!.gameObject.SetActive(inGame);

        if (inGame) _meter.text = Render(EntropyManager.Value);
    }

    private static string Render(float value)
    {
        var filled = Mathf.Clamp(Mathf.RoundToInt(value / EntropyManager.Max * Segments), 0, Segments);
        var color = TierColor(EntropyManager.Tier);

        var bar = $"<mark={color}>{Bar(filled)}</mark><mark=#00000066>{Bar(Segments - filled)}</mark>";

        return $"<size=70%>STABLE</size> {bar} <size=70%>CHAOTIC</size>";
    }

    private static string Bar(int segments) => segments <= 0 ? string.Empty : string.Concat(Enumerable.Repeat(Blank, segments));

    private static string TierColor(EntropyTier tier) => tier switch
    {
        EntropyTier.Stable => "#4CD964FF",
        EntropyTier.Unstable => "#FFD60AFF",
        EntropyTier.Volatile => "#FF9500FF",
        _ => "#FF3B30FF",
    };
}
