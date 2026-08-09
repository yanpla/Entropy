using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace Entropy.Anomalies;

/// <summary>
/// Somebody is walking around over there. They are not.
/// </summary>
/// <remarks>
/// Built from the real player prefab rather than the intro cutscene's dummy, so the
/// hat, visor and skin sit where they are supposed to and it is exactly player sized.
/// Vanilla's own <see cref="PlayerControl.notRealPlayer"/> flag keeps it out of
/// <see cref="PlayerControl.AllPlayerControls"/>, which is how the tutorial's dummies
/// avoid being mistaken for people.
/// <para>
/// It wears a living player's outfit and name and walks routes a player could have
/// walked, then disappears the moment the target comes close enough to be sure. Nothing
/// the target can do will confirm it happened, which is the entire idea.
/// </para>
/// </remarks>
public class FakeStranger : Anomaly
{
    private const float Lifetime = 25f;

    /// <summary>How far away it appears, and how close the target has to get to dispel it.</summary>
    private const float MinDistance = 5f;
    private const float MaxDistance = 7f;
    private const float VanishRange = 3f;

    /// <summary>How far it will wander for in one go.</summary>
    private const float WanderMin = 3f;
    private const float WanderMax = 12f;

    /// <summary>Close enough to a waypoint to call it reached.</summary>
    private const float Arrival = 0.05f;

    /// <summary>Fallback pace if the local player has no physics to copy.</summary>
    private const float Pace = 2.5f;

    public override string Name => "Someone is walking there";

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

        var stranger = Object.Instantiate(AmongUsClient.Instance.PlayerPrefab);

        Disown(stranger);
        Dress(stranger, candidates[rng.Next(candidates.Count)]);
        Stand(stranger, spot);
        stranger.cosmetics.SetFlipX(spot.x < feet.x);

        var route = new Queue<Vector2>();
        var walking = false;

        for (var elapsed = 0f; elapsed < Lifetime; elapsed += Time.deltaTime)
        {
            if (!stranger || !target) break;
            if (Vector2.Distance(target.GetTruePosition(), Where(stranger)) < VanishRange) break;

            if (route.Count == 0)
            {
                foreach (var waypoint in Wander(stranger, rng)) route.Enqueue(waypoint);
            }

            // Only on the change, or the animation restarts from its first frame every
            // time round and the legs never actually move.
            if (walking != route.Count > 0)
            {
                walking = route.Count > 0;

                if (walking) stranger.MyPhysics.Animations.PlayRunAnimation();
                else stranger.MyPhysics.Animations.PlayIdleAnimation();
            }

            if (route.Count > 0 && Step(stranger, route.Peek())) route.Dequeue();

            yield return null;
        }

        if (stranger) Object.Destroy(stranger.gameObject);
    }

    /// <summary>
    /// A route to somewhere else it could plausibly have walked to, or nothing.
    /// </summary>
    /// <remarks>
    /// A stranger that stands perfectly still reads as a prop. One that walks reads as a
    /// person, right up until it is not there any more.
    /// </remarks>
    private static IEnumerable<Vector2> Wander(PlayerControl stranger, Random rng)
    {
        var here = Where(stranger);

        return Placement.Find(rng, here, WanderMin, WanderMax) is { } destination
            ? Placement.Route(here, destination) ?? []
            : [];
    }

    /// <summary>Moves one frame towards a waypoint, reporting whether it arrived.</summary>
    private static bool Step(PlayerControl stranger, Vector2 waypoint)
    {
        var here = Where(stranger);

        // Copy the real walking speed rather than guess at one, so it moves like a player.
        var pace = PlayerControl.LocalPlayer && PlayerControl.LocalPlayer.MyPhysics
            ? PlayerControl.LocalPlayer.MyPhysics.TrueSpeed
            : Pace;

        var moved = Vector2.MoveTowards(here, waypoint, pace * Time.deltaTime);
        Stand(stranger, moved);

        if (!Mathf.Approximately(moved.x, here.x)) stranger.cosmetics.SetFlipX(moved.x < here.x);

        return Vector2.Distance(moved, waypoint) < Arrival;
    }

    private static Vector2 Where(PlayerControl stranger) => stranger.transform.position;

    /// <summary>Puts it down, with the depth players are sorted by.</summary>
    private static void Stand(PlayerControl stranger, Vector2 spot) =>
        stranger.transform.position = new Vector3(spot.x, spot.y, spot.y / 1000f);

    /// <summary>
    /// Takes a freshly built player back out of the game it just joined.
    /// </summary>
    /// <remarks>
    /// Awake has already registered it, so the flag goes on and the registration comes
    /// off by hand. Everything that would make it behave like a player is switched off:
    /// it has no data to run on, and it is meant to be scenery. Its physics component
    /// stays for the animations hanging off it, which work fine while disabled.
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
