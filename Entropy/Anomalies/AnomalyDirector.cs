using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using Reactor.Utilities;
using Random = System.Random;

namespace Entropy.Anomalies;

/// <summary>
/// Puts an anomaly on the wire and runs it.
/// </summary>
/// <remarks>
/// Deciding when is each player's own <see cref="EntropyModifier"/>; this is only the
/// getting there. The choice is broadcast as an anomaly id, a target and a seed. Every
/// client runs the same anomaly from the same seed, and the anomaly confines itself to
/// its target - which is usually one person's screen and nobody else's.
/// </remarks>
public static class AnomalyDirector
{
    private static readonly Random Rng = new();

    /// <summary>Host only: tells everyone an anomaly is happening to <paramref name="target"/>.</summary>
    public static void Fire(Anomaly anomaly, PlayerControl target)
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
}
