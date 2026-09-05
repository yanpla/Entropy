using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace Entropy.Anomalies;

// Spawns a local player imitation that follows walkable routes.
public class FakeStranger : Anomaly
{
    private const float Lifetime = 25f;

    private const float MinDistance = 5f;
    private const float MaxDistance = 7f;

    private const float WanderMin = 3f;
    private const float WanderMax = 12f;

    private const float ArrivalDistance = 0.1f;

    private const int UnownedClientId = -2;

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

        if (Placement.Find(rng, feet, MinDistance, MaxDistance) is not { } spot) yield break;

        var stranger = Object.Instantiate(AmongUsClient.Instance.PlayerPrefab);

        DisablePlayerControl(stranger);
        ApplyAppearance(stranger, candidates[rng.Next(candidates.Count)]);
        SetPosition(stranger, spot);
        stranger.cosmetics.SetFlipX(spot.x < feet.x);

        var route = new Queue<Vector2>();

        for (var elapsed = 0f; elapsed < Lifetime; elapsed += Time.deltaTime)
        {
            if (!stranger || !target) break;

            if (route.Count == 0)
            {
                foreach (var waypoint in FindRoute(stranger, rng)) route.Enqueue(waypoint);
            }

            if (route.Count == 0) stranger.MyPhysics.body.velocity = Vector2.zero;
            else if (MoveTowards(stranger, route.Peek())) route.Dequeue();

            SetPosition(stranger, stranger.transform.position);

            yield return null;
        }

        if (stranger) Object.Destroy(stranger.gameObject);
    }

    private static IEnumerable<Vector2> FindRoute(PlayerControl stranger, Random rng)
    {
        var here = (Vector2)stranger.transform.position;

        return Placement.Find(rng, here, WanderMin, WanderMax) is { } destination
            ? Placement.Route(here, destination) ?? []
            : [];
    }

    // Rigidbody velocity drives vanilla walking animations and sprite direction.
    private static bool MoveTowards(PlayerControl stranger, Vector2 waypoint)
    {
        var toGo = waypoint - (Vector2)stranger.transform.position;

        if (toGo.magnitude < ArrivalDistance)
        {
            stranger.MyPhysics.body.velocity = Vector2.zero;

            return true;
        }

        stranger.MyPhysics.body.velocity = toGo.normalized * stranger.MyPhysics.TrueSpeed;

        return false;
    }

    private static void SetPosition(PlayerControl stranger, Vector2 spot) =>
        stranger.transform.position = new Vector3(spot.x, spot.y, spot.y / 1000f);

    private static void DisablePlayerControl(PlayerControl stranger)
    {
        // Awake registers the clone before we can mark it as a fake.
        stranger.notRealPlayer = true;
        stranger.OwnerId = UnownedClientId;
        PlayerControl.AllPlayerControls.Remove(stranger);

        // Keep physics enabled for animation; an unowned ID prevents joystick control.
        stranger.enabled = false;
        if (stranger.NetTransform) stranger.NetTransform.enabled = false;
        if (stranger.Collider) stranger.Collider.enabled = false;
    }

    // Update cosmetics directly: the clone has no player data.
    private static void ApplyAppearance(PlayerControl stranger, PlayerControl impersonated)
    {
        var outfit = impersonated.Data.DefaultOutfit;
        var cosmetics = stranger.cosmetics;

        // Initialize animation offsets so cosmetics follow the body.
        stranger.MyPhysics.SetBodyType(PlayerBodyTypes.Normal);

        cosmetics.SetColor(outfit.ColorId);
        cosmetics.SetHat(outfit.HatId, outfit.ColorId);
        cosmetics.SetVisor(outfit.VisorId, outfit.ColorId);
        cosmetics.SetSkin(outfit.SkinId, outfit.ColorId);

        // Direct cosmetic updates skip vanilla's name positioning.
        cosmetics.SetNamePosition(new Vector3(0f, string.IsNullOrEmpty(outfit.HatId) ? 0.8f : 1f, -0.5f));

        cosmetics.SetName(outfit.PlayerName);
        cosmetics.ToggleNameVisible(true);
    }
}
