using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Map;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events.Vanilla.Player;
using MiraAPI.Events.Vanilla.Usables;

namespace Entropy.Core;

/// <summary>
/// Feeds the meter from normal gameplay.
/// </summary>
/// <remarks>
/// Each handler reports exactly once, from the client that owns the action, so the
/// host never double counts. Meetings are the exception: nobody "performs" them, so
/// the host claims that one.
/// </remarks>
public static class EntropyEvents
{
    [RegisterEvent]
    public static void OnMurder(AfterMurderEvent @event)
    {
        if (@event.Source.AmOwner) EntropyManager.Report(EntropySource.Kill);
    }

    [RegisterEvent]
    public static void OnSabotage(UpdateSystemEvent @event)
    {
        // Every sabotage is requested through the Sabotage system; the other system
        // updates are repairs, doors and map noise.
        if (@event.SystemType == SystemTypes.Sabotage) EntropyManager.Report(EntropySource.Sabotage);
    }

    [RegisterEvent]
    public static void OnMeetingStart(StartMeetingEvent @event)
    {
        if (AmongUsClient.Instance.AmHost) EntropyManager.Report(EntropySource.Meeting);
    }

    [RegisterEvent]
    public static void OnEnterVent(EnterVentEvent @event)
    {
        if (@event.Player.AmOwner) EntropyManager.Report(EntropySource.Vent);
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
