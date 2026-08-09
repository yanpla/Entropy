using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Entropy.Core;

/// <summary>
/// The entropy meter.
/// </summary>
/// <remarks>
/// The host owns the value. Every client reports the actions it performs, the host
/// applies the weight and broadcasts the new value back. That keeps one number
/// authoritative even though the vanilla hooks we listen to fire in different places
/// on different clients.
/// </remarks>
public static class EntropyManager
{
    public const float Max = 100f;

    /// <summary>Current entropy, 0 to <see cref="Max"/>. Same on every client.</summary>
    public static float Value { get; private set; }

    public static EntropyTier Tier => Value switch
    {
        < 25f => EntropyTier.Stable,
        < 50f => EntropyTier.Unstable,
        < 75f => EntropyTier.Volatile,
        _ => EntropyTier.Critical,
    };

    /// <summary>Raised on every client after <see cref="Value"/> changes.</summary>
    public static event Action<float>? Changed;

    /// <summary>
    /// Reports an action this client just performed. Safe to call from any client;
    /// only the host actually moves the meter.
    /// </summary>
    public static void Report(EntropySource source)
    {
        if (!PlayerControl.LocalPlayer) return;

        RpcReport(PlayerControl.LocalPlayer, (byte)source);
    }

    /// <summary>Puts the meter back to zero. Host only; no-op elsewhere.</summary>
    public static void HostReset() => HostSet(0f);

    private static void HostSet(float value)
    {
        if (!AmongUsClient.Instance.AmHost || !PlayerControl.LocalPlayer) return;

        RpcSet(PlayerControl.LocalPlayer, Mathf.Clamp(value, 0f, Max));
    }

    [MethodRpc((uint)EntropyRpc.Report, LocalHandling = RpcLocalHandling.Before)]
    public static void RpcReport(PlayerControl sender, byte source)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        HostSet(Value + ((EntropySource)source).Weight());
    }

    [MethodRpc((uint)EntropyRpc.Set, LocalHandling = RpcLocalHandling.Before)]
    public static void RpcSet(PlayerControl sender, float value)
    {
        // Ignore anyone claiming to be the host but isn't.
        if (sender.OwnerId != AmongUsClient.Instance.HostId) return;

        Value = value;
        Changed?.Invoke(value);
    }
}
