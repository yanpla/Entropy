namespace Entropy.Anomalies;

// Array indices are RPC IDs. Append entries; do not reorder or remove them.
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

    public static readonly RealityCollapse Collapse = All.OfType<RealityCollapse>().Single();

    public static byte IdOf(Anomaly anomaly) => (byte)System.Array.IndexOf(All, anomaly);

    public static List<Anomaly> Unlocked(EntropyTier tier, PlayerControl target) => All
        .Where(anomaly => anomaly.Scheduled && anomaly.MinTier <= tier && anomaly.CanRun(target))
        .ToList();
}
