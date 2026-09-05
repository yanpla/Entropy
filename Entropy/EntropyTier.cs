namespace Entropy;

// Entropy thresholds that unlock scheduled anomalies.
public enum EntropyTier
{
    Stable,
    Unstable,
    Volatile,
    Critical,
}
