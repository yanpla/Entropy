using System.Collections;
using Random = System.Random;

namespace Entropy.Anomalies;

// Runs on every client with the same seed; each anomaly must restrict effects to its target.
public abstract class Anomaly
{
    public abstract string Name { get; }

    public abstract float MinEntropy { get; }

    // Unscheduled anomalies are fired explicitly, such as reality collapse.
    public virtual bool Scheduled => true;

    public virtual bool CanRun(PlayerControl target) => true;

    public abstract IEnumerator Run(PlayerControl target, Random rng);
}
