using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace Entropy.Anomalies;

/// <summary>
/// A corpse only one player can see, wearing a living player's colours.
/// </summary>
/// <remarks>
/// Spawned from the real dead body prefab, so it looks right, highlights the report
/// button and can be walked up to. It is never networked: reporting it opens no
/// meeting, the body simply is not there any more, and the entropy of having called
/// everyone to a corpse that does not exist goes on the meter.
/// </remarks>
public class FakeBody : Anomaly
{
    private const float Lifetime = 45f;

    /// <summary>Far enough away to have to be noticed, close enough to be found.</summary>
    private const float MinDistance = 2.5f;
    private const float MaxDistance = 8f;

    /// <summary>How much empty space a body needs so it isn't half inside a wall.</summary>
    private const float Clearance = 0.35f;

    private const int Attempts = 15;

    /// <summary>Bodies this client has faked, so a report can tell them from real ones.</summary>
    internal static readonly List<DeadBody> Fakes = [];

    public override string Name => "There is a body";

    public override EntropyTier MinTier => EntropyTier.Volatile;

    public override bool CanRun(PlayerControl target) => Players.Alive().Any(player => player != target);

    public override IEnumerator Run(PlayerControl target, Random rng)
    {
        if (!target.AmOwner) yield break;

        var candidates = Players.Alive().Where(player => player != target).ToList();
        if (candidates.Count == 0) yield break;

        var victim = candidates[rng.Next(candidates.Count)];
        var body = Object.Instantiate(GameManager.Instance.GetDeadBody(victim.Data.Role));

        // Must stay enabled: the report button only lights up for bodies whose component
        // is enabled. The kill animation disables one while it plays and turns it back
        // on afterwards, and copying only the first half is what left this unreportable.
        body.enabled = true;
        body.ParentId = victim.PlayerId;

        for (var i = 0; i < body.bodyRenderers.Length; i++) victim.SetPlayerMaterialColors(body.bodyRenderers[i]);
        victim.SetPlayerMaterialColors(body.bloodSplatter);

        var where = Somewhere(target, rng);
        body.transform.position = new Vector3(where.x, where.y, where.y / 1000f);

        Fakes.Add(body);

        yield return new WaitForSeconds(Lifetime);

        // Still standing there unreported - it gives up and was never there.
        Forget(body);
    }

    /// <summary>
    /// Somewhere near the player that a body could plausibly be lying.
    /// </summary>
    /// <remarks>
    /// Rejection sampling rather than any notion of where the floor is: a spot is good
    /// if nothing solid is sitting on it and nothing solid is between it and the player.
    /// The second test is doing most of the work - it keeps the body in the same open
    /// space the player is standing in, which is both how it stays on walkable ground
    /// and how it stays reportable, since vanilla wants line of sight to report.
    /// </remarks>
    private static Vector2 Somewhere(PlayerControl target, Random rng)
    {
        var from = target.GetTruePosition();

        for (var attempt = 0; attempt < Attempts; attempt++)
        {
            var angle = (float)(rng.NextDouble() * System.Math.PI * 2d);
            var reach = (float)(rng.NextDouble() * (MaxDistance - MinDistance) + MinDistance);
            var spot = from + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * reach;

            if (Physics2D.OverlapCircle(spot, Clearance, Constants.ShipAndObjectsMask)) continue;
            if (PhysicsHelpers.AnythingBetween(from, spot, Constants.ShipAndObjectsMask, false)) continue;

            return spot;
        }

        // Nowhere clear in reach - underfoot is ugly, but it is definitely walkable.
        return from;
    }

    internal static void Forget(DeadBody body)
    {
        Fakes.Remove(body);

        if (body) Object.Destroy(body.gameObject);
    }
}
