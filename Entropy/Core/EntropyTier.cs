namespace Entropy.Core;

/// <summary>
/// The quarter of the meter the game is currently in. Anomalies unlock by tier.
/// </summary>
public enum EntropyTier
{
    /// <summary>0-25%.</summary>
    Stable,

    /// <summary>25-50%.</summary>
    Unstable,

    /// <summary>50-75%.</summary>
    Volatile,

    /// <summary>75-100%.</summary>
    Critical,
}
