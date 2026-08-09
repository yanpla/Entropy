using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Entropy.Core;
using HarmonyLib;
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

        // Disabled the same way a real corpse is; nothing about it needs to think.
        body.enabled = false;
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

/// <summary>
/// Swallows the report of a body that does not exist.
/// </summary>
/// <remarks>
/// Vanilla has already run its distance and line of sight checks and set
/// <see cref="DeadBody.Reported"/> by the time this runs, so the fake that was just
/// clicked is the one flagged. Returning false stops the report before any of it
/// reaches the network - no meeting is called and nobody else ever knows.
/// </remarks>
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdReportDeadBody))]
public static class FakeBodyReportPatch
{
    public static bool Prefix()
    {
        // Drop destroyed entries first, or a stale one would swallow a real report.
        FakeBody.Fakes.RemoveAll(body => !body);

        var reported = FakeBody.Fakes.FirstOrDefault(body => body.Reported);
        if (reported is null) return true;

        FakeBody.Forget(reported);
        EntropyManager.Report(EntropySource.FalseReport);

        return false;
    }
}
