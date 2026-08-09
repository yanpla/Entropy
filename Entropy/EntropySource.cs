namespace Entropy;

/// <summary>
/// An in-game action that moves entropy.
/// </summary>
public enum EntropySource : byte
{
    Sabotage,
    Meeting,
    TaskComplete,

    /// <summary>Someone reported a body that was never there.</summary>
    FalseReport,
}

public static class EntropySourceExtensions
{
    /// <summary>
    /// How many entropy points the action is worth. Task completion is the only way to
    /// push a meter back down.
    /// </summary>
    public static float Weight(this EntropySource source) => source switch
    {
        EntropySource.Sabotage => 8f,
        EntropySource.Meeting => 10f,
        EntropySource.TaskComplete => -2f,

        // Doubting your own eyes feeds the thing that made you doubt them.
        EntropySource.FalseReport => 12f,
        _ => 0f,
    };

    /// <summary>
    /// Whether this lands on everybody or only on whoever caused it. The ship-wide
    /// events are felt ship-wide; what you do alone is yours to carry.
    /// </summary>
    public static bool AffectsEveryone(this EntropySource source) =>
        source is EntropySource.Sabotage or EntropySource.Meeting;
}
