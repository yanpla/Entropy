using Entropy.Modifiers;
using MiraAPI.Modifiers;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Entropy.Networking;

public static class ReportRpc
{
    [MethodRpc((uint)EntropyRpc.Report, LocalHandling = RpcLocalHandling.Before)]
    public static void RpcReportEntropy(this PlayerControl sender, byte source)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        var action = (EntropySource)source;
        if (action == EntropySource.Sabotage)
        {
            foreach (var modifier in ModifierUtils.GetActiveModifiers<EntropyModifier>()) modifier.Add(action);
        }
        else
        {
            sender.GetModifier<EntropyModifier>()?.Add(action);
        }
    }
}
