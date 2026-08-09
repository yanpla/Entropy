using Entropy.Core;
using HarmonyLib;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using Reactor.Utilities;
using UnityEngine;
using Random = System.Random;

namespace Entropy.Anomalies;

/// <summary>
/// Decides when something goes wrong.
/// </summary>
/// <remarks>
/// The host rolls on a fixed cadence and the odds rise with the meter, so players can
/// feel chaos coming without being able to predict it. The choice is broadcast as an
/// id plus a seed; every client then runs the same anomaly and makes the same
/// decisions from that seed.
/// </remarks>
public static class AnomalyDirector
{
    private const float RollInterval = 15f;

    /// <summary>Odds of an anomaly per roll at an empty and a full meter.</summary>
    private const float MinChance = 0.10f;
    private const float MaxChance = 0.65f;

    private static readonly Random Rng = new();
    private static float _timer;

    public static void Reset() => _timer = RollInterval;

    private static void Tick(float deltaTime)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!ShipStatus.Instance || MeetingHud.Instance || !AmongUsClient.Instance.IsGameStarted) return;

        if (EntropyManager.Value >= EntropyManager.Max)
        {
            Fire(AnomalyRegistry.Collapse);
            EntropyManager.HostReset();
            _timer = RollInterval;

            return;
        }

        _timer -= deltaTime;
        if (_timer > 0f) return;
        _timer = RollInterval;

        var chance = Mathf.Lerp(MinChance, MaxChance, EntropyManager.Value / EntropyManager.Max);
        if (Rng.NextDouble() > chance) return;

        var unlocked = AnomalyRegistry.Unlocked(EntropyManager.Tier);
        if (unlocked.Count == 0) return;

        Fire(unlocked[Rng.Next(unlocked.Count)]);
    }

    private static void Fire(Anomaly anomaly)
    {
        if (!PlayerControl.LocalPlayer) return;

        RpcRunAnomaly(PlayerControl.LocalPlayer, AnomalyRegistry.IdOf(anomaly), Rng.Next());
    }

    [MethodRpc((uint)EntropyRpc.RunAnomaly, LocalHandling = RpcLocalHandling.Before)]
    public static void RpcRunAnomaly(PlayerControl sender, byte id, int seed)
    {
        if (sender.OwnerId != AmongUsClient.Instance.HostId) return;
        if (id >= AnomalyRegistry.All.Length) return;

        var anomaly = AnomalyRegistry.All[id];

        Logger<EntropyPlugin>.Info($"Anomaly: {anomaly.Name} (seed {seed})");
        Coroutines.Start(anomaly.Run(new Random(seed)));
    }

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.FixedUpdate))]
    private static class TickPatch
    {
        public static void Postfix() => Tick(Time.fixedDeltaTime);
    }
}
