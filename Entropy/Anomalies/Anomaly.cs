using System.Collections;
using Entropy.Core;
using Random = System.Random;

namespace Entropy.Anomalies;

/// <summary>
/// A thing the world does to one player.
/// </summary>
/// <remarks>
/// Anomalies are scheduled per player and afflict only their target, so no two people
/// are living in the same game. Most of them are pure client-side illusion: the target
/// sees the lights die or hears a vent that nobody used, and everyone else sees a
/// perfectly ordinary room. Reporting an anomaly should sound like a lie.
/// <para>
/// <see cref="Run"/> executes on every client with an identical <see cref="Random"/>,
/// so anomalies can make their own choices instead of the host shipping every detail
/// over the wire. Each one is responsible for confining itself to its target - usually
/// by returning early unless the target is this client's own player.
/// </para>
/// </remarks>
public abstract class Anomaly
{
    /// <summary>Named for the logs; players are never told this happened.</summary>
    public abstract string Name { get; }

    /// <summary>The lowest tier at which this can be rolled.</summary>
    public abstract EntropyTier MinTier { get; }

    /// <summary>False for anomalies that only fire on purpose, like the collapse.</summary>
    public virtual bool Scheduled => true;

    /// <summary>Whether this can meaningfully happen to <paramref name="target"/> right now.</summary>
    public virtual bool CanRun(PlayerControl target) => true;

    public abstract IEnumerator Run(PlayerControl target, Random rng);
}
