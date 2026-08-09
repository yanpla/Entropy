using System.Linq;
using Entropy.Anomalies;
using HarmonyLib;

namespace Entropy.Patches;

/// <summary>
/// Swallows the report of a body that does not exist.
/// </summary>
/// <remarks>
/// Vanilla has already run its distance and line of sight checks and set
/// <see cref="DeadBody.Reported"/> by the time this runs, so the fake that was just
/// clicked is the one flagged. Returning false stops the report before any of it
/// reaches the network - no meeting is called and nobody else ever knows.
/// </remarks>
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdReportDeadBody))]
public static class FakeBodyReportPatch
{
    public static bool Prefix()
    {
        // Drop destroyed entries first, or a stale one would swallow a real report.
        FakeBody.Fakes.RemoveAll(body => !body);

        var reported = FakeBody.Fakes.FirstOrDefault(body => body.Reported);
        if (reported is null) return true;

        FakeBody.Forget(reported);
        EntropyManager.Report(EntropySource.FalseReport);

        return false;
    }
}
