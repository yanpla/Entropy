using Entropy.Networking;
using Random = System.Random;

namespace Entropy.Anomalies;

public static class AnomalyManager
{
    private static readonly Random Rng = new();

    public static readonly RealityCollapse Collapse = new();

    // Array indices are RPC IDs. Append entries without changing existing positions.
    public static readonly Anomaly[] All =
    [
        new DoorMalfunction(),
        new PhantomSounds(),
        new Shapeshift(),
        new FakeSabotage(),
        new Displacement(),
        new FakeStranger(),
        new FakeBody(),
        Collapse,
    ];

    public static void FireRandom(PlayerControl target, float entropy)
    {
        var choices = All.Where(anomaly => anomaly.Scheduled
            && anomaly.MinEntropy <= entropy && anomaly.CanRun(target)).ToList();

        if (choices.Count > 0) Fire(choices[Rng.Next(choices.Count)], target);
    }

    public static void Fire(Anomaly anomaly, PlayerControl target)
    {
        if (!PlayerControl.LocalPlayer || !target) return;

        var id = (byte)Array.IndexOf(All, anomaly);
        PlayerControl.LocalPlayer.RpcRunAnomaly(id, target.PlayerId, Rng.Next());
    }
}
