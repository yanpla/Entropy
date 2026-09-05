using Entropy.Modifiers;
using MiraAPI.Modifiers;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Entropy;

// Routes action reports to the host and updates affected modifiers.
public static class EntropyManager
{
    public static void Report(EntropySource source)
    {
        if (!PlayerControl.LocalPlayer) return;

        RpcReport(PlayerControl.LocalPlayer, (byte)source);
    }

    [MethodRpc((uint)EntropyRpc.Report, LocalHandling = RpcLocalHandling.Before)]
    public static void RpcReport(PlayerControl sender, byte source)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        var kind = (EntropySource)source;

        if (!kind.AffectsEveryone())
        {
            sender.GetModifier<EntropyModifier>()?.Add(kind);

            return;
        }

        foreach (var modifier in ModifierUtils.GetActiveModifiers<EntropyModifier>()) modifier.Add(kind);
    }
}
