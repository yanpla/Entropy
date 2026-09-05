using System.Collections;
using UnityEngine;
using Random = System.Random;

namespace Entropy.Anomalies;

// Temporarily changes another player's appearance on the target's client.
public class Shapeshift : Anomaly
{
    private const float Duration = 30f;

    public override string Name => "Someone is wearing another face";

    public override EntropyTier MinTier => EntropyTier.Volatile;

    public override bool CanRun(PlayerControl target) =>
        Shifters(target).Count > 0 && Players.PresumedAlive().Count > 1;

    public override IEnumerator Run(PlayerControl target, Random rng)
    {
        if (!target.AmOwner) yield break;

        var shifters = Shifters(target);
        if (shifters.Count == 0) yield break;

        var shifter = shifters[rng.Next(shifters.Count)];

        var models = Players.PresumedAlive().Where(player => player != shifter).ToList();
        if (models.Count == 0) yield break;

        var model = models[rng.Next(models.Count)];
        var own = shifter.Data.DefaultOutfit;

        Wear(shifter, model.Data.DefaultOutfit);

        yield return new WaitForSeconds(Duration);

        if (shifter) Wear(shifter, own);
    }

    // Keeping the outfit type unchanged avoids writes to networked player data.
    private static void Wear(PlayerControl player, NetworkedPlayerInfo.PlayerOutfit outfit) =>
        player.RawSetOutfit(outfit, PlayerOutfitType.Default);

    // Exclude real shapeshifters: restoring Default would overwrite their outfit state.
    private static List<PlayerControl> Shifters(PlayerControl target) =>
        Players.Alive()
            .Where(player => player != target && player.CurrentOutfitType == PlayerOutfitType.Default)
            .ToList();
}
