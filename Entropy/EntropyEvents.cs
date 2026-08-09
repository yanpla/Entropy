using System.Linq;
using Entropy.Anomalies;
using Entropy.Modifiers;
using MiraAPI.Events;
using MiraAPI.Modifiers;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Map;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events.Vanilla.Player;

namespace Entropy;

/// <summary>
/// Feeds entropy from normal gameplay.
/// </summary>
/// <remarks>
/// Each handler reports from the client that owns the action, so the host attributes it
/// correctly and never double counts. A meeting adds nothing: it wipes every
/// <see cref="EntropyModifier"/> back to zero instead, so each round starts from calm.
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

    /// <summary>An impostor has no tasks to steady themselves with, only this.</summary>
    [RegisterEvent]
    public static void OnMurder(AfterMurderEvent @event)
    {
        if (@event.Source.AmOwner) EntropyManager.Report(EntropySource.Kill);
    }

    /// <summary>
    /// A round starts once everyone has seen who is missing, so every death behind us is
    /// now common knowledge and only the next one can be a secret.
    /// </summary>
    [RegisterEvent]
    public static void OnRoundStart(RoundStartEvent @event) => Players.Bury();

    /// <summary>Everyone walks out of a meeting calm, whatever they walked in as.</summary>
    [RegisterEvent]
    public static void OnMeetingEnd(EndMeetingEvent @event)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        foreach (var modifier in ModifierUtils.GetActiveModifiers<EntropyModifier>()) modifier.Reset();
    }

    /// <summary>
    /// Swallows the report of a body that was never there.
    /// </summary>
    /// <remarks>
    /// Mira raises this from both the host's report path and the outgoing network
    /// message, so cancelling here stops the meeting whoever called it - unlike
    /// patching CmdReportDeadBody, which il2cpp inlines and Harmony never reaches.
    /// The clicked body has already had <see cref="DeadBody.Reported"/> set by the time
    /// we get here, so the flagged fake is the one that was just reported.
    /// </remarks>
    [RegisterEvent]
    public static void OnReportBody(ReportBodyEvent @event)
    {
        if (!@event.Reporter.AmOwner) return;

        // Drop destroyed entries first, or a stale one would swallow a real report.
        FakeBody.Fakes.RemoveAll(body => !body);

        var reported = FakeBody.Fakes.FirstOrDefault(body => body.Reported);
        if (reported is null) return;

        FakeBody.Forget(reported);
        EntropyManager.Report(EntropySource.FalseReport);

        @event.Cancel();
    }
}
