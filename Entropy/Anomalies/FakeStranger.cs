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
/// walked. You can follow it, catch it up, stand in it - it just keeps walking, and then
/// one moment it is not there. Nothing you did made it go, and nothing you can do will
/// prove it was ever there, which is the entire idea.
/// </para>
/// </remarks>
public class FakeStranger : Anomaly
{
    private const float Lifetime = 25f;

    /// <summary>How far away it first appears.</summary>
    private const float MinDistance = 5f;
    private const float MaxDistance = 7f;

    /// <summary>How far it will wander for in one go.</summary>
    private const float WanderMin = 3f;
    private const float WanderMax = 12f;

    /// <summary>Close enough to a waypoint to call it reached.</summary>
    private const float Arrival = 0.1f;

    /// <summary>A client id nobody holds, so the fake is owned by no one.</summary>
    private const int Nobody = -2;

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

        for (var elapsed = 0f; elapsed < Lifetime; elapsed += Time.deltaTime)
        {
            if (!stranger || !target) break;

            if (route.Count == 0)
            {
                foreach (var waypoint in Wander(stranger, rng)) route.Enqueue(waypoint);
            }

            if (route.Count == 0) stranger.MyPhysics.body.velocity = Vector2.zero;
            else if (Step(stranger, route.Peek())) route.Dequeue();

            Depth(stranger);

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

    /// <summary>
    /// Steers towards a waypoint, reporting whether it arrived.
    /// </summary>
    /// <remarks>
    /// Drives the rigidbody rather than the transform, because that is the one thing
    /// vanilla watches. Given a velocity, its own physics play the walk cycle, animate the
    /// skin and flip the sprite - and the hat and visor ride along, which they will not do
    /// for anything moved behind the game's back.
    /// </remarks>
    private static bool Step(PlayerControl stranger, Vector2 waypoint)
    {
        var toGo = waypoint - Where(stranger);

        if (toGo.magnitude < Arrival)
        {
            stranger.MyPhysics.body.velocity = Vector2.zero;

            return true;
        }

        stranger.MyPhysics.body.velocity = toGo.normalized * stranger.MyPhysics.TrueSpeed;

        return false;
    }

    private static Vector2 Where(PlayerControl stranger) => stranger.transform.position;

    /// <summary>Puts it down, with the depth players are sorted by.</summary>
    private static void Stand(PlayerControl stranger, Vector2 spot) =>
        stranger.transform.position = new Vector3(spot.x, spot.y, spot.y / 1000f);

    /// <summary>Keeps it sorted against everything else as it walks up and down.</summary>
    private static void Depth(PlayerControl stranger)
    {
        var here = stranger.transform.position;

        stranger.transform.position = new Vector3(here.x, here.y, here.y / 1000f);
    }

    /// <summary>
    /// Takes a freshly built player back out of the game it just joined.
    /// </summary>
    /// <remarks>
    /// Awake has already registered it, so the flag goes on and the registration comes off
    /// by hand. <see cref="PlayerControl"/> itself is switched off because it has no data
    /// to run on, and so is the networking, because nobody else is to know.
    /// <para>
    /// Its physics stay on. That component is what watches the rigidbody and drives the
    /// walk cycle, the skin and the hat and visor off it; disabling it was why the clothes
    /// hung in mid air. Instead the owner is set to nobody, so its own update never mistakes
    /// this for the local player and starts steering it with our joystick.
    /// </para>
    /// </remarks>
    private static void Disown(PlayerControl stranger)
    {
        stranger.notRealPlayer = true;
        stranger.OwnerId = Nobody;
        PlayerControl.AllPlayerControls.Remove(stranger);

        stranger.enabled = false;
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

        // Builds the body and hands the animation system the offsets that hats and visors
        // ride on. Nothing else does this - a real player gets it when the game sets their
        // body type - so without it the clothes hang wherever the prefab left them and do
        // not follow the walk.
        stranger.MyPhysics.SetBodyType(PlayerBodyTypes.Normal);

        cosmetics.SetColor(outfit.ColorId);
        cosmetics.SetHat(outfit.HatId, outfit.ColorId);
        cosmetics.SetVisor(outfit.VisorId, outfit.ColorId);
        cosmetics.SetSkin(outfit.SkinId, outfit.ColorId);

        // Vanilla lifts the name clear of the head whenever it sets a hat, and higher when
        // there is one to clear. Setting the hat straight onto the cosmetics skips that,
        // which leaves the name sitting in the middle of the body.
        cosmetics.SetNamePosition(new Vector3(0f, string.IsNullOrEmpty(outfit.HatId) ? 0.8f : 1f, -0.5f));

        cosmetics.SetName(outfit.PlayerName);
        cosmetics.ToggleNameVisible(true);
    }
}
