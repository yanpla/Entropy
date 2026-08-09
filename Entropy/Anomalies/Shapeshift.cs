using System.Collections;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace Entropy.Anomalies;

/// <summary>
/// One player is wearing somebody else's face, and only the target can see it.
/// </summary>
/// <remarks>
/// Nobody shapeshifted. On every other screen the two of them look like themselves, so
/// the target watches a player walk around as someone who is standing somewhere else -
/// and there is no vote, no role and no cooldown to point at afterwards.
/// </remarks>
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

        // Anyone the crew still thinks is walking around, the target included. A player
        // lying dead in a corridor nobody has found is the best face to wear: the target
        // sees them alive and well, and cannot say so without explaining how they know.
        var models = Players.PresumedAlive().Where(player => player != shifter).ToList();
        if (models.Count == 0) yield break;

        var model = models[rng.Next(models.Count)];
        var own = shifter.Data.DefaultOutfit;

        Wear(shifter, model.Data.DefaultOutfit);

        yield return new WaitForSeconds(Duration);

        // Back to themselves as if nothing happened, whether or not anyone was looking.
        if (shifter) Wear(shifter, own);
    }

    /// <summary>
    /// Puts an outfit on a player, on this client only.
    /// </summary>
    /// <remarks>
    /// Vanilla's own <see cref="PlayerControl.Shapeshift"/> is no good here: animated, it
    /// casts the player's role to a shapeshifter and would throw for anybody else, and
    /// either way it records the new look in networked player data.
    /// <para>
    /// <see cref="PlayerControl.RawSetOutfit"/> asked for the default outfit type does the
    /// cosmetic half and nothing else - both of its writes to player data are skipped when
    /// the type is unchanged - so the change lives and dies on this screen.
    /// </para>
    /// </remarks>
    private static void Wear(PlayerControl player, NetworkedPlayerInfo.PlayerOutfit outfit) =>
        player.RawSetOutfit(outfit, PlayerOutfitType.Default);

    /// <summary>
    /// Players who could plausibly change, which is not the target and not anybody the
    /// game has genuinely shapeshifted - putting their default back would strip an outfit
    /// out of real player data.
    /// </summary>
    private static System.Collections.Generic.List<PlayerControl> Shifters(PlayerControl target) =>
        Players.Alive()
            .Where(player => player != target && player.CurrentOutfitType == PlayerOutfitType.Default)
            .ToList();
}
