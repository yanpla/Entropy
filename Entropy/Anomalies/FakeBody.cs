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

        var where = target.transform.position;
        where.z = where.y / 1000f;
        body.transform.position = where;

        Fakes.Add(body);

        yield return new WaitForSeconds(Lifetime);

        // Still standing there unreported - it gives up and was never there.
        Forget(body);
    }

    internal static void Forget(DeadBody body)
    {
        Fakes.Remove(body);

        if (body) Object.Destroy(body.gameObject);
    }
}
