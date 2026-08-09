namespace Entropy;

/// <summary>
/// Rpc ids owned by this mod. Reactor scopes these per mod, so they can't clash
/// with vanilla or other mods.
/// </summary>
public enum EntropyRpc : uint
{
    /// <summary>A client telling the host it performed an <see cref="EntropySource"/>.</summary>
    Report,

    /// <summary>The host telling everyone which anomaly fires, on whom, and with what seed.</summary>
    RunAnomaly,
}
