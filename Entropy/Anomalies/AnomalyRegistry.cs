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
        new CooldownShift(),
        new PlayerSwap(),
        new PhantomSounds(),
        new AnonymousPlayers(),
        new RandomSabotage(),
        new InvertedControls(),
        new MassTeleport(),
        new RealityCollapse(),
    ];

    public static readonly RealityCollapse Collapse = All.OfType<RealityCollapse>().Single();

    public static byte IdOf(Anomaly anomaly) => (byte)System.Array.IndexOf(All, anomaly);

    /// <summary>Anomalies the current tier has unlocked and the current map can support.</summary>
    public static List<Anomaly> Unlocked(EntropyTier tier) => All
        .Where(anomaly => anomaly.Scheduled && anomaly.MinTier <= tier && anomaly.CanRun())
        .ToList();
}
