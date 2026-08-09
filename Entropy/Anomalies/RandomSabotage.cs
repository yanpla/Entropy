using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Entropy.Core;
using Random = System.Random;

namespace Entropy.Anomalies;

/// <summary>
/// The ship sabotages itself. No impostor spent a cooldown on this one.
/// </summary>
public class RandomSabotage : Anomaly
{
    private static readonly SystemTypes[] Sabotages =
    [
        SystemTypes.Reactor,
        SystemTypes.LifeSupp,
        SystemTypes.Comms,
        SystemTypes.Electrical,
        SystemTypes.Laboratory,
        SystemTypes.HeliSabotage,
        SystemTypes.MushroomMixupSabotage,
    ];

    public override string Name => "The ship turns on you";

    public override EntropyTier MinTier => EntropyTier.Volatile;

    public override bool CanRun() => Available().Count > 0;

    public override IEnumerator Run(Random rng)
    {
        var available = Available();
        if (available.Count == 0) yield break;

        var target = available.Draw(rng);

        // Routed through the Sabotage system so the game applies its own rules for what
        // a sabotage of this type actually does.
        if (AmongUsClient.Instance.AmHost) ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Sabotage, (byte)target);
    }

    private static List<SystemTypes> Available() => Sabotages
        .Where(system => ShipStatus.Instance && ShipStatus.Instance.Systems.ContainsKey(system))
        .OrderBy(system => (int)system)
        .ToList();
}
