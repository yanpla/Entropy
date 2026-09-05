using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace Entropy.Anomalies;

// Spawns a local, reportable body using a living player's appearance.
public class FakeBody : Anomaly
{
    private const float Lifetime = 45f;

    private const float MinDistance = 2.5f;
    private const float MaxDistance = 8f;

    private static readonly List<DeadBody> Fakes = [];

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

        // The report button ignores disabled body components.
        body.enabled = true;
        body.ParentId = victim.PlayerId;

        for (var i = 0; i < body.bodyRenderers.Length; i++) victim.SetPlayerMaterialColors(body.bodyRenderers[i]);
        victim.SetPlayerMaterialColors(body.bloodSplatter);

        // Fall back to the target's position if no nearby spot is available.
        var feet = target.GetTruePosition();
        var where = Placement.Find(rng, feet, MinDistance, MaxDistance) ?? feet;
        body.transform.position = new Vector3(where.x, where.y, where.y / 1000f);

        Fakes.Add(body);

        yield return new WaitForSeconds(Lifetime);

        Remove(body);
    }

    internal static bool TryRemoveReported()
    {
        // Vanilla sets Reported before raising the report event.
        Fakes.RemoveAll(body => !body);
        var reported = Fakes.FirstOrDefault(body => body.Reported);
        if (reported is null) return false;

        Remove(reported);
        return true;
    }

    private static void Remove(DeadBody body)
    {
        Fakes.Remove(body);

        if (body) Object.Destroy(body.gameObject);
    }
}
