using System.Collections;
using System.Linq;
using Entropy.Core;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace Entropy.Anomalies;

/// <summary>
/// Somebody is standing over there. They are not.
/// </summary>
/// <remarks>
/// Wears a living player's outfit and name, at a distance the target can see but not
/// touch, and disappears the moment they come close enough to be sure. Nothing the
/// target can do will confirm it happened, which is the entire idea.
/// </remarks>
public class FakeStranger : Anomaly
{
    private const float Lifetime = 20f;

    /// <summary>How far away it appears, and how close the target has to get to dispel it.</summary>
    private const float Distance = 6f;
    private const float VanishRange = 3f;

    public override string Name => "Someone is standing there";

    public override EntropyTier MinTier => EntropyTier.Unstable;

    public override bool CanRun(PlayerControl target) =>
        HudManager.Instance && HudManager.Instance.IntroPrefab && Players.Alive().Any(player => player != target);

    public override IEnumerator Run(PlayerControl target, Random rng)
    {
        if (!target.AmOwner) yield break;

        var candidates = Players.Alive().Where(player => player != target).ToList();
        if (candidates.Count == 0) yield break;

        var impersonated = candidates[rng.Next(candidates.Count)];

        var stranger = Object.Instantiate(HudManager.Instance.IntroPrefab.PlayerPrefab);
        stranger.gameObject.name = "EntropyStranger";
        stranger.gameObject.layer = target.gameObject.layer;
        stranger.UpdateFromPlayerOutfit(
            impersonated.Data.DefaultOutfit,
            PlayerMaterial.MaskType.None,
            false,
            false);
        stranger.SetName(impersonated.Data.PlayerName);

        var angle = (float)(rng.NextDouble() * System.Math.PI * 2d);
        var spot = target.transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * Distance;
        spot.z = spot.y / 1000f;
        stranger.transform.position = spot;
        stranger.SetFlipX(spot.x < target.transform.position.x);

        // Gone before they can get a proper look, and gone anyway if they never try.
        for (var elapsed = 0f; elapsed < Lifetime; elapsed += Time.deltaTime)
        {
            if (!target || Vector2.Distance(target.GetTruePosition(), spot) < VanishRange) break;

            yield return null;
        }

        if (stranger) Object.Destroy(stranger.gameObject);
    }
}
