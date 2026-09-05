using Entropy.Anomalies;
using Entropy.Options.Modifiers;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers.Types;
using UnityEngine;
using Random = System.Random;

namespace Entropy.Modifiers;

// Stores a player's entropy and schedules anomalies on the host. Hidden from players.
public class EntropyModifier : GameModifier
{
    public const float Max = 100f;

    public const float Min = -25f;

    private const float CalmGap = 30f;
    private const float ChaoticGap = 10f;

    private const float Jitter = 0.3f;

    private static readonly Random Rng = new();

    private float _timer;

    private bool _collapsed;

    public override string ModifierName => "Entropy";

    public override bool HideOnUi => true;

    public override bool ShowInFreeplay => false;

    public float Value { get; private set; }

    public override int GetAmountPerGame() => (int)OptionGroupSingleton<EntropyModifierSettings>.Instance.Amount;

    public override int GetAssignmentChance() => (int)OptionGroupSingleton<EntropyModifierSettings>.Instance.Chance;

    public override void OnActivate() => _timer = NextDelay();

    public void Add(EntropySource source)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        var options = OptionGroupSingleton<EntropyModifierSettings>.Instance;
        var amount = source switch
        {
            EntropySource.Sabotage => 8f,
            EntropySource.TaskComplete => -options.TaskReward,
            EntropySource.Kill => -options.KillReward,
            EntropySource.FalseReport => 12f,
            _ => 0f,
        };
        Shift(amount);
    }

    private void Shift(float delta) => Value = Mathf.Clamp(Value + delta, Min, Max);

    public void Reset()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        Value = 0f;
        _timer = NextDelay();
        _collapsed = false;
    }

    public override void FixedUpdate()
    {
        if (!AmongUsClient.Instance.AmHost || !Player) return;
        if (!ShipStatus.Instance || MeetingHud.Instance || !AmongUsClient.Instance.IsGameStarted) return;
        if (Player.Data is not { IsDead: false, Disconnected: false }) return;

        Shift(Max / OptionGroupSingleton<EntropyModifierSettings>.Instance.SecondsToFill * Time.fixedDeltaTime);

        // Collapse once per round.
        if (Value >= Max && !_collapsed)
        {
            _collapsed = true;
            AnomalyManager.Fire(AnomalyManager.Collapse, Player);
        }

        // Pause the anomaly timer while entropy is negative.
        if (Value < 0f) return;

        _timer -= Time.fixedDeltaTime;
        if (_timer > 0f) return;

        _timer = NextDelay();

        AnomalyManager.FireRandom(Player, Value);
    }

    private float NextDelay()
    {
        var gap = Mathf.Lerp(CalmGap, ChaoticGap, Value / Max);

        return gap * (1f + (float)(Rng.NextDouble() * 2d - 1d) * Jitter);
    }
}
