using System.Collections.Generic;
using System.Linq;

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
        new DoorMalfunction(),
        new PhantomSounds(),
        new Shapeshift(),
        new FakeSabotage(),
        new Displacement(),
        new FakeStranger(),
        new FakeBody(),
        new RealityCollapse(),
    ];

    /// <summary>The one anomaly nothing rolls for; the meter has to fill.</summary>
    public static readonly RealityCollapse Collapse = All.OfType<RealityCollapse>().Single();

    public static byte IdOf(Anomaly anomaly) => (byte)System.Array.IndexOf(All, anomaly);

    /// <summary>What the tier has unlocked and can meaningfully happen to this player.</summary>
    public static List<Anomaly> Unlocked(EntropyTier tier, PlayerControl target) => All
        .Where(anomaly => anomaly.Scheduled && anomaly.MinTier <= tier && anomaly.CanRun(target))
        .ToList();
}
