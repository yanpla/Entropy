using Entropy.Anomalies;
using Entropy.Utilities;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using Reactor.Utilities;
using Random = System.Random;

namespace Entropy.Networking;

public static class AnomalyRpc
{
    [MethodRpc((uint)EntropyRpc.RunAnomaly, LocalHandling = RpcLocalHandling.Before)]
    public static void RpcRunAnomaly(this PlayerControl sender, byte id, byte targetId, int seed)
    {
        if (sender.OwnerId != AmongUsClient.Instance.HostId || id >= AnomalyManager.All.Length) return;

        var target = Players.ById(targetId);
        if (target is null || !target) return;

        var anomaly = AnomalyManager.All[id];
        Logger<EntropyPlugin>.Info($"Anomaly: {anomaly.Name} on {target.Data?.PlayerName} (seed {seed})");
        Coroutines.Start(anomaly.Run(target, new Random(seed)));
    }
}
