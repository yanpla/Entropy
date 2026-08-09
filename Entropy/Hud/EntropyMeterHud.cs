using Entropy.Core;
using HarmonyLib;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Entropy.Hud;

/// <summary>
/// The entropy meter: the vanilla task bar, cloned and shrunk, sitting to its right.
/// </summary>
/// <remarks>
/// Cloning <see cref="ProgressTracker"/> rather than drawing our own bar means the meter
/// already matches the game's art, segment shader and hud layer. We drop its tracker
/// component so nothing fights us for the fill, and drive the shader directly.
/// </remarks>
[HarmonyPatch(typeof(HudManager))]
public static class EntropyMeterHud
{
    /// <summary>Size relative to the task bar it sits next to.</summary>
    private const float Scale = 0.6f;

    private const float Gap = 0.15f;
    private const int Segments = 10;

    /// <summary>How fast the bar slides toward the real value, in percent per second.</summary>
    private const float SlideSpeed = 45f;

    private static GameObject? _meter;
    private static MeshRenderer? _tiles;
    private static TextMeshPro? _label;
    private static float _shown;

    [HarmonyPostfix]
    [HarmonyPatch(nameof(HudManager.Start))]
    public static void CreateMeter(HudManager __instance)
    {
        var tracker = __instance.GetComponentInChildren<ProgressTracker>(true);
        if (!tracker) return;

        var original = tracker.transform;
        _meter = Object.Instantiate(tracker.gameObject, original.parent);
        _meter.name = "EntropyMeter";

        // Read the cloned tracker's own tiles before dropping it - otherwise it keeps
        // writing task progress into the shader every FixedUpdate.
        var clonedTracker = _meter.GetComponent<ProgressTracker>();
        _tiles = clonedTracker.TileParent;
        Object.Destroy(clonedTracker);

        _meter.transform.localScale = original.localScale * Scale;

        // Butt our left edge against the task bar's right edge, measured in world space.
        // The object's pivot is not the middle of the bar it draws, so offsetting from
        // its position lands somewhere arbitrary - only the rendered edges line up.
        var shift = tracker.TileParent.bounds.max.x + Gap - _tiles.bounds.min.x;
        Nudge(_meter, shift / original.parent.lossyScale.x);

        // The vanilla caption may be a sibling of the bar rather than a child, in which
        // case the clone comes without one and we supply our own.
        _label = _meter.GetComponentInChildren<TextMeshPro>(true);
        _label ??= Object.Instantiate(__instance.TaskPanel.taskText, _meter.transform);
        _label!.transform.localPosition = Vector3.zero;
        _label.rectTransform.sizeDelta = new Vector2(_tiles.bounds.size.x / Scale, 0.4f);
        _label.alignment = TextAlignmentOptions.Center;

        // A cloned vanilla caption still carries its translator, which would put
        // "TOTAL TASKS COMPLETED" back the moment its own Start runs.
        var translator = _label.GetComponent<TextTranslatorTMP>();
        if (translator) Object.Destroy(translator);

        _label.text = "ENTROPY";

        _shown = EntropyManager.Value;
        _meter.SetActive(false);
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(HudManager.Update))]
    public static void RefreshMeter()
    {
        if (!_meter || !_tiles) return;

        // Only on a real change: every enable re-runs the anchor and its layout maths.
        var visible = ShipStatus.Instance && PlayerControl.LocalPlayer && !MeetingHud.Instance;
        if (_meter!.activeSelf != visible) _meter.SetActive(visible);
        if (!visible) return;

        _shown = Mathf.MoveTowards(_shown, EntropyManager.Value, SlideSpeed * Time.deltaTime);

        var fraction = Mathf.Clamp01(_shown / EntropyManager.Max);
        var material = _tiles!.material;

        material.SetFloat("_Buckets", Segments);
        material.SetFloat("_FullBuckets", fraction * Segments);
        material.SetColor("_Color", TierColor(EntropyManager.Tier));
    }

    /// <summary>
    /// Moves a hud element sideways for good.
    /// </summary>
    /// <remarks>
    /// Hud elements are anchored by <see cref="AspectPosition"/>, which rewrites
    /// localPosition from its own offset every time the object is enabled. Moving the
    /// transform directly survives until the next enable and no longer; the anchor
    /// offset is the thing that actually has to change.
    /// </remarks>
    private static void Nudge(GameObject element, float distance)
    {
        var anchor = element.GetComponent<AspectPosition>();
        if (!anchor)
        {
            element.transform.localPosition += new Vector3(distance, 0f, 0f);

            return;
        }

        // Right aligned anchors measure inwards from the right edge, so the sign flips.
        var offset = anchor.DistanceFromEdge;
        offset.x += ((int)anchor.Alignment & (int)AspectPosition.EdgeAlignments.Right) != 0 ? -distance : distance;
        anchor.DistanceFromEdge = offset;
        anchor.AdjustPosition();
    }

    /// <summary>Critical entropy pulses, so a full meter is impossible to miss.</summary>
    private static Color TierColor(EntropyTier tier)
    {
        var color = tier switch
        {
            EntropyTier.Stable => new Color(0.30f, 0.85f, 0.39f),
            EntropyTier.Unstable => new Color(1.00f, 0.84f, 0.04f),
            EntropyTier.Volatile => new Color(1.00f, 0.58f, 0.00f),
            _ => new Color(1.00f, 0.23f, 0.19f),
        };

        if (tier != EntropyTier.Critical) return color;

        return Color.Lerp(color, Color.white, (Mathf.Sin(Time.time * 6f) + 1f) * 0.25f);
    }
}
