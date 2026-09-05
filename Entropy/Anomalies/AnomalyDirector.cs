using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using Reactor.Utilities;
using Random = System.Random;

namespace Entropy.Anomalies;

// Broadcasts host-selected anomalies with a target and random seed.
public static class AnomalyDirector
{
    private static readonly Random Rng = new();

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
