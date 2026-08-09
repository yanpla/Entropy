using Entropy.Options.Modifiers;
using MiraAPI.GameOptions;

namespace Entropy;

/// <summary>
/// An in-game action that moves entropy.
/// </summary>
public enum EntropySource : byte
{
    Sabotage,
    Meeting,

    /// <summary>A crewmate's way of holding entropy off.</summary>
    TaskComplete,

    /// <summary>An impostor's way of holding entropy off.</summary>
    Kill,

    /// <summary>Someone reported a body that was never there.</summary>
    FalseReport,
}

public static class EntropySourceExtensions
{
    /// <summary>
    /// How many entropy points the action is worth.
    /// </summary>
    /// <remarks>
    /// Entropy climbs on its own, so everything here is measured against that drift:
    /// doing your job pushes back, and the two jobs are worth the same. A crewmate
    /// finishing a task and an impostor landing a kill each buy about the same reprieve,
    /// which is why neither side can stand still.
    /// </remarks>
    public static float Weight(this EntropySource source)
    {
        var settings = OptionGroupSingleton<EntropyModifierSettings>.Instance;

        return source switch
        {
            EntropySource.Sabotage => 8f,
            EntropySource.Meeting => 10f,

            // The host sets these as the ground you win back, so they read as positive
            // in the lobby and are spent as negative here.
            EntropySource.TaskComplete => -settings.TaskReward,
            EntropySource.Kill => -settings.KillReward,

            // Doubting your own eyes feeds the thing that made you doubt them.
            EntropySource.FalseReport => 12f,
            _ => 0f,
        };
    }

    /// <summary>
    /// Whether this lands on everybody or only on whoever caused it. The ship-wide
    /// events are felt ship-wide; what you do alone is yours to carry.
    /// </summary>
    public static bool AffectsEveryone(this EntropySource source) =>
        source is EntropySource.Sabotage or EntropySource.Meeting;
}
