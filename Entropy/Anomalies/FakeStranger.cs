using System.Collections;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace Entropy.Anomalies;

/// <summary>
/// Somebody is standing over there. They are not.
/// </summary>
/// <remarks>
/// Built from the real player prefab rather than the intro cutscene's dummy, so the
/// hat, visor and skin sit where they are supposed to and it is exactly player sized.
/// Vanilla's own <see cref="PlayerControl.notRealPlayer"/> flag keeps it out of
/// <see cref="PlayerControl.AllPlayerControls"/>, which is how the tutorial's dummies
/// avoid being mistaken for people.
/// <para>
/// It wears a living player's outfit and name, stands at a distance the target can see
/// but not touch, and disappears the moment they come close enough to be sure. Nothing
/// the target can do will confirm it happened, which is the entire idea.
/// </para>
/// </remarks>
public class FakeStranger : Anomaly
{
    private const float Lifetime = 20f;

    /// <summary>How far away it appears, and how close the target has to get to dispel it.</summary>
    private const float MinDistance = 5f;
    private const float MaxDistance = 7f;
    private const float VanishRange = 3f;

    public override string Name => "Someone is standing there";

    public override EntropyTier MinTier => EntropyTier.Unstable;

    public override bool CanRun(PlayerControl target) =>
        AmongUsClient.Instance.PlayerPrefab && Players.Alive().Any(player => player != target);

    public override IEnumerator Run(PlayerControl target, Random rng)
    {
        if (!target.AmOwner) yield break;

        var candidates = Players.Alive().Where(player => player != target).ToList();
        if (candidates.Count == 0) yield break;

        var feet = target.GetTruePosition();

        // Nowhere in sight to stand: better no stranger than one inside a wall.
        if (Placement.Find(rng, feet, MinDistance, MaxDistance) is not { } spot) yield break;

        var impersonated = candidates[rng.Next(candidates.Count)];
        var stranger = Object.Instantiate(AmongUsClient.Instance.PlayerPrefab);

        Disown(stranger);
        Dress(stranger, impersonated);

        stranger.transform.position = new Vector3(spot.x, spot.y, spot.y / 1000f);
        stranger.cosmetics.SetFlipX(spot.x < feet.x);

        // Gone before they can get a proper look, and gone anyway if they never try.
        for (var elapsed = 0f; elapsed < Lifetime; elapsed += Time.deltaTime)
        {
            if (!target || Vector2.Distance(target.GetTruePosition(), spot) < VanishRange) break;

            yield return null;
        }

        if (stranger) Object.Destroy(stranger.gameObject);
    }

    /// <summary>
    /// Takes a freshly built player back out of the game it just joined.
    /// </summary>
    /// <remarks>
    /// Awake has already registered it, so the flag goes on and the registration comes
    /// off by hand. Everything that would make it behave like a player is switched off:
    /// it has no data to run on, and it is meant to be scenery.
    /// </remarks>
    private static void Disown(PlayerControl stranger)
    {
        stranger.notRealPlayer = true;
        PlayerControl.AllPlayerControls.Remove(stranger);

        stranger.enabled = false;
        if (stranger.MyPhysics) stranger.MyPhysics.enabled = false;
        if (stranger.NetTransform) stranger.NetTransform.enabled = false;
        if (stranger.Collider) stranger.Collider.enabled = false;
    }

    /// <summary>
    /// Dresses it as somebody who is still alive.
    /// </summary>
    /// <remarks>
    /// Straight onto the cosmetics layer rather than through the PlayerControl helpers,
    /// which write back to player data this thing does not have.
    /// </remarks>
    private static void Dress(PlayerControl stranger, PlayerControl impersonated)
    {
        var outfit = impersonated.Data.DefaultOutfit;
        var cosmetics = stranger.cosmetics;

        cosmetics.SetColor(outfit.ColorId);
        cosmetics.SetHat(outfit.HatId, outfit.ColorId);
        cosmetics.SetVisor(outfit.VisorId, outfit.ColorId);
        cosmetics.SetSkin(outfit.SkinId, outfit.ColorId);
        cosmetics.SetName(outfit.PlayerName);
        cosmetics.ToggleNameVisible(true);
    }
}
