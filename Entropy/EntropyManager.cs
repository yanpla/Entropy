using Entropy.Modifiers;
using MiraAPI.Modifiers;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Entropy;

/// <summary>
/// The way an action becomes entropy.
/// </summary>
/// <remarks>
/// The value itself lives on the <see cref="EntropyModifier"/> of whoever the host
/// afflicted this game. This is only the road there: a client says what it did, and the
/// host decides whose entropy moves - which is nobody's, for anyone without one.
/// </remarks>
public static class EntropyManager
{
    /// <summary>
    /// Tells the host this client just did something. Safe to call from any client;
    /// whether it lands on the caller or on everybody is the source's business.
    /// </summary>
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
