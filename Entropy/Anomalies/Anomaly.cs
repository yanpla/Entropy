using System.Collections;
using Entropy.Core;
using Random = System.Random;

namespace Entropy.Anomalies;

/// <summary>
/// A thing the world does to the players when entropy allows it.
/// </summary>
/// <remarks>
/// <see cref="Run"/> executes on every client with an identical <see cref="Random"/>,
/// so anomalies can make their own choices instead of the host shipping every detail
/// over the wire. Anything that must happen once (a sabotage, a teleport) is guarded
/// by a host or owner check inside the anomaly.
/// </remarks>
public abstract class Anomaly
{
    /// <summary>Shown to players when it fires.</summary>
    public abstract string Name { get; }

    /// <summary>The lowest tier at which this can be rolled.</summary>
    public abstract EntropyTier MinTier { get; }

    /// <summary>False for anomalies that only fire on purpose, like the collapse.</summary>
    public virtual bool Scheduled => true;

    /// <summary>Whether the current map and game state can support it right now.</summary>
    public virtual bool CanRun() => true;

    public abstract IEnumerator Run(Random rng);
}
