using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Map;
using MiraAPI.Events.Vanilla.Player;

namespace Entropy;

/// <summary>
/// Feeds entropy from normal gameplay.
/// </summary>
/// <remarks>
/// Each handler reports from the client that owns the action, so the host attributes it
/// correctly and never double counts. Meetings are not here: every
/// <see cref="EntropyModifier"/> picks those up itself.
/// </remarks>
public static class EntropyEvents
{
    [RegisterEvent]
    public static void OnSabotage(UpdateSystemEvent @event)
    {
        // Every sabotage is requested through the Sabotage system; the other system
        // updates are repairs, doors and map noise.
        if (@event.SystemType == SystemTypes.Sabotage) EntropyManager.Report(EntropySource.Sabotage);
    }

    [RegisterEvent]
    public static void OnTaskComplete(CompleteTaskEvent @event)
    {
        if (@event.Player.AmOwner) EntropyManager.Report(EntropySource.TaskComplete);
    }

    [RegisterEvent]
    public static void OnRoundStart(RoundStartEvent @event)
    {
        // Entropy carries across rounds, but not across games.
        if (@event.TriggeredByIntro) EntropyManager.HostReset();
    }
}
