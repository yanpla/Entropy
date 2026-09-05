using Entropy.Anomalies;
using Entropy.Modifiers;
using MiraAPI.Events;
using MiraAPI.Modifiers;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Map;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events.Vanilla.Player;

namespace Entropy;

// Reports gameplay actions and resets entropy after meetings.
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
    public static void OnMurder(AfterMurderEvent @event)
    {
        if (@event.Source.AmOwner) EntropyManager.Report(EntropySource.Kill);
    }

    [RegisterEvent]
    public static void OnRoundStart(RoundStartEvent @event) => Players.RecordKnownDeaths();

    [RegisterEvent]
    public static void OnMeetingEnd(EndMeetingEvent @event)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        foreach (var modifier in ModifierUtils.GetActiveModifiers<EntropyModifier>()) modifier.Reset();
    }

    // Mira's event intercepts reports even when IL2CPP inlines CmdReportDeadBody.
    [RegisterEvent]
    public static void OnReportBody(ReportBodyEvent @event)
    {
        if (!@event.Reporter.AmOwner) return;

        if (!FakeBody.TryRemoveReported()) return;
        EntropyManager.Report(EntropySource.FalseReport);

        @event.Cancel();
    }
}
