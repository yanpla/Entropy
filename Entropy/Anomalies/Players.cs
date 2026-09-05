namespace Entropy.Anomalies;

// Sorts player lookups by ID so seeded choices use a consistent order.
public static class Players
{
    private static readonly HashSet<byte> KnownDeaths = [];

    public static List<PlayerControl> Alive() => PlayerControl.AllPlayerControls.ToArray()
        .Where(player => player && player.Data is { IsDead: false, Disconnected: false })
        .OrderBy(player => player.PlayerId)
        .ToList();

    // Includes deaths that have not yet been revealed at a meeting.
    public static List<PlayerControl> PresumedAlive() => PlayerControl.AllPlayerControls.ToArray()
        .Where(player => player
            && player.Data is { Disconnected: false }
            && (!player.Data.IsDead || !KnownDeaths.Contains(player.PlayerId)))
        .OrderBy(player => player.PlayerId)
        .ToList();

    public static void RecordKnownDeaths()
    {
        KnownDeaths.Clear();

        foreach (var player in PlayerControl.AllPlayerControls.ToArray()
                     .Where(player => player && player.Data is { IsDead: true }))
        {
            KnownDeaths.Add(player.PlayerId);
        }
    }

    public static PlayerControl? ById(byte playerId) => PlayerControl.AllPlayerControls.ToArray()
        .FirstOrDefault(player => player && player.PlayerId == playerId);
}
