using Entropy.Anomalies;
using Entropy.Options.Modifiers;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers.Types;
using UnityEngine;
using Random = System.Random;

namespace Entropy.Modifiers;

/// <summary>
/// One player's entropy, and the countdown to whatever it is about to do to them.
/// </summary>
/// <remarks>
/// Assigned at the start of the game like a role, in whatever quantity the host set.
/// The player is never told: it has no description and is never drawn, so an afflicted
/// player and an unafflicted one see exactly the same screen until something happens
/// that should not have. Not knowing whether you have it is the point - a player who
/// could check would just be a player with a strange role.
/// <para>
/// Every client holds a copy, but only the host's copy is ever written to - it is the
/// only thing that sees every report and the only thing scheduling anomalies.
/// </para>
/// </remarks>
public class EntropyModifier : GameModifier
{
    public const float Max = 100f;

    /// <summary>
    /// How far ahead of the drift a player can get. Working hard buys quiet, but only
    /// so much - without a floor, a task rush or a killing spree would switch the mod
    /// off for that player for the rest of the game.
    /// </summary>
    public const float Min = -25f;

    /// <summary>Seconds between this player's anomalies at an empty and at a full meter.</summary>
    private const float CalmGap = 25f;
    private const float ChaoticGap = 4f;

    /// <summary>How far either side of the gap the next one may land, so it can't be counted.</summary>
    private const float Jitter = 0.3f;

    private static readonly Random Rng = new();

    private float _timer;

    public override string ModifierName => "Entropy";

    /// <summary>Never drawn, and never listed in freeplay. They are not to know.</summary>
    public override bool HideOnUi => true;

    public override bool ShowInFreeplay => false;

    public float Value { get; private set; }

    public EntropyTier Tier => Value switch
    {
        < 25f => EntropyTier.Stable,
        < 50f => EntropyTier.Unstable,
        < 75f => EntropyTier.Volatile,
        _ => EntropyTier.Critical,
    };

    public override int GetAmountPerGame() => (int)OptionGroupSingleton<EntropyModifierSettings>.Instance.Amount;

    public override int GetAssignmentChance() => (int)OptionGroupSingleton<EntropyModifierSettings>.Instance.Chance;

    public override void OnActivate() => _timer = Gap();

    /// <summary>Moves this player's entropy. Host only, so every copy agrees by not trying.</summary>
    public void Add(EntropySource source)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        Shift(source.Weight());
    }

    private void Shift(float delta) => Value = Mathf.Clamp(Value + delta, Min, Max);

    /// <summary>Nobody performs a meeting, so everyone carrying this pays for it.</summary>
    public override void OnMeetingStart() => Add(EntropySource.Meeting);

    public override void FixedUpdate()
    {
        if (!AmongUsClient.Instance.AmHost || !Player) return;
        if (!ShipStatus.Instance || MeetingHud.Instance || !AmongUsClient.Instance.IsGameStarted) return;
        if (Player.Data is not { IsDead: false, Disconnected: false }) return;

        // The meter fills by itself. Standing still is the losing move for everyone.
        Shift(Max / OptionGroupSingleton<EntropyModifierSettings>.Instance.SecondsToFill * Time.fixedDeltaTime);

        // Negative is banked quiet: this player has got far enough ahead of the drift
        // that nothing happens to them until it catches back up. The timer holds where
        // it is, so the reprieve ends where it interrupted.
        if (Value < 0f) return;

        _timer -= Time.fixedDeltaTime;
        if (_timer > 0f) return;

        _timer = Gap();

        var unlocked = AnomalyRegistry.Unlocked(Tier, Player);
        if (unlocked.Count == 0) return;

        AnomalyDirector.Fire(unlocked[Rng.Next(unlocked.Count)], Player);
    }

    /// <summary>The wait until this player's next anomaly, from their own entropy.</summary>
    private float Gap()
    {
        var gap = Mathf.Lerp(CalmGap, ChaoticGap, Value / Max);

        return gap * (1f + (float)(Rng.NextDouble() * 2d - 1d) * Jitter);
    }
}
