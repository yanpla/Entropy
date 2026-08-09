using System.Collections.Generic;
using Entropy.Core;
using HarmonyLib;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using Reactor.Utilities;
using UnityEngine;
using Random = System.Random;

namespace Entropy.Anomalies;

/// <summary>
/// Decides who something goes wrong for, and when.
/// </summary>
/// <remarks>
/// Every player runs their own independent schedule, so nobody shares an experience of
/// the round. The gap shrinks as the shared meter fills, which is the one thing all of
/// them can see: they can watch chaos coming without being able to compare notes on
/// what it did. The choice is broadcast as an anomaly id, a target and a seed; every
/// client runs the same anomaly, and the anomaly confines itself to its target.
/// </remarks>
public static class AnomalyDirector
{
    /// <summary>Seconds between one player's anomalies at an empty and at a full meter.</summary>
    private const float CalmGap = 25f;
    private const float ChaoticGap = 4f;

    /// <summary>How far either side of the gap the next one may land, so it can't be counted.</summary>
    private const float Jitter = 0.3f;

    private static readonly Random Rng = new();
    private static readonly Dictionary<byte, float> Timers = new();

    public static void Reset() => Timers.Clear();

    private static void Tick(float deltaTime)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!ShipStatus.Instance || MeetingHud.Instance || !AmongUsClient.Instance.IsGameStarted) return;

        // A negative meter is banked quiet: the crew has out-tasked the chaos and
        // nothing happens to anybody until the killing puts them back above empty.
        // Timers hold where they are, so the reprieve ends where it interrupted.
        if (EntropyManager.Value < 0f) return;

        foreach (var player in Players.Alive())
        {
            if (!Timers.TryGetValue(player.PlayerId, out var remaining)) remaining = Gap();

            remaining -= deltaTime;
            if (remaining > 0f)
            {
                Timers[player.PlayerId] = remaining;

                continue;
            }

            Timers[player.PlayerId] = Gap();

            var unlocked = AnomalyRegistry.Unlocked(EntropyManager.Tier, player);
            if (unlocked.Count == 0) continue;

            Fire(unlocked[Rng.Next(unlocked.Count)], player);
        }
    }

    /// <summary>The wait until a player's next anomaly, from where the meter stands now.</summary>
    private static float Gap()
    {
        var gap = Mathf.Lerp(CalmGap, ChaoticGap, EntropyManager.Value / EntropyManager.Max);

        return gap * (1f + (float)(Rng.NextDouble() * 2d - 1d) * Jitter);
    }

    private static void Fire(Anomaly anomaly, PlayerControl target)
    {
        if (!PlayerControl.LocalPlayer || !target) return;

        RpcRunAnomaly(PlayerControl.LocalPlayer, AnomalyRegistry.IdOf(anomaly), target.PlayerId, Rng.Next());
    }

    [MethodRpc((uint)EntropyRpc.RunAnomaly, LocalHandling = RpcLocalHandling.Before)]
    public static void RpcRunAnomaly(PlayerControl sender, byte id, byte targetId, int seed)
    {
        if (sender.OwnerId != AmongUsClient.Instance.HostId) return;
        if (id >= AnomalyRegistry.All.Length) return;

        var target = Players.ById(targetId);
        if (target is null || !target) return;

        var anomaly = AnomalyRegistry.All[id];

        Logger<EntropyPlugin>.Info($"Anomaly: {anomaly.Name} on {target.Data?.PlayerName} (seed {seed})");
        Coroutines.Start(anomaly.Run(target, new Random(seed)));
    }

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.FixedUpdate))]
    private static class TickPatch
    {
        public static void Postfix() => Tick(Time.fixedDeltaTime);
    }
}
