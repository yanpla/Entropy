using System.Collections.Generic;
using System.Linq;
using Entropy.Core;

namespace Entropy.Anomalies;

/// <summary>
/// Every anomaly in the game.
/// </summary>
/// <remarks>
/// An anomaly's index in <see cref="All"/> is its id on the wire, so entries may be
/// appended but never reordered or removed.
/// </remarks>
public static class AnomalyRegistry
{
    public static readonly Anomaly[] All =
    [
        new LightsFlicker(),
        new DoorMalfunction(),
        new PhantomSounds(),
        new AnonymousPlayers(),
        new FakeSabotage(),
        new Displacement(),
        new FakeStranger(),
        new FakeBody(),
    ];

    public static byte IdOf(Anomaly anomaly) => (byte)System.Array.IndexOf(All, anomaly);

    /// <summary>What the tier has unlocked and can meaningfully happen to this player.</summary>
    public static List<Anomaly> Unlocked(EntropyTier tier, PlayerControl target) => All
        .Where(anomaly => anomaly.Scheduled && anomaly.MinTier <= tier && anomaly.CanRun(target))
        .ToList();
}
