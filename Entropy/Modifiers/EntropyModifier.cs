using Entropy.Anomalies;
using MiraAPI.Modifiers;
using UnityEngine;
using Random = System.Random;

namespace Entropy.Modifiers;

/// <summary>
/// One player's entropy, and the countdown to whatever it is about to do to them.
/// </summary>
/// <remarks>
/// Hidden: it has no description, so Mira never draws it, and the player it is attached
/// to is never told it exists. Everyone carries one, so two people standing in the same
/// room can be having entirely different games.
/// <para>
/// Every client holds a copy, but only the host's copy is ever written to - it is the
/// only thing that sees every report and the only thing scheduling anomalies. The other
/// copies just sit there at zero.
/// </para>
/// </remarks>
public class EntropyModifier : BaseModifier
{
    public const float Max = 100f;

    /// <summary>
    /// How far below empty a player can bank. Keeping ahead of the chaos buys quiet,
    /// but only so much - without a floor, an early task rush would switch the mod off
    /// for that player for the rest of the game.
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

    public float Value { get; private set; }

    public EntropyTier Tier => Value switch
    {
        < 25f => EntropyTier.Stable,
        < 50f => EntropyTier.Unstable,
        < 75f => EntropyTier.Volatile,
        _ => EntropyTier.Critical,
    };

    /// <summary>Moves this player's entropy. Host only, so every copy agrees by not trying.</summary>
    public void Add(EntropySource source)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        Value = Mathf.Clamp(Value + source.Weight(), Min, Max);
    }

    public void Reset()
    {
        Value = 0f;
        _timer = Gap();
    }

    /// <summary>Nobody performs a meeting, so everyone pays for it.</summary>
    public override void OnMeetingStart() => Add(EntropySource.Meeting);

    public override void FixedUpdate()
    {
        if (!AmongUsClient.Instance.AmHost || !Player) return;
        if (!ShipStatus.Instance || MeetingHud.Instance || !AmongUsClient.Instance.IsGameStarted) return;
        if (Player.Data is not { IsDead: false, Disconnected: false }) return;

        // Negative is banked quiet: this player has out-tasked their own chaos and
        // nothing happens to them until something puts them back above empty. The timer
        // holds where it is, so the reprieve ends where it interrupted.
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
