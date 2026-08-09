namespace Entropy.Core;

/// <summary>
/// An in-game action that moves the entropy meter.
/// </summary>
public enum EntropySource : byte
{
    Kill,
    Sabotage,
    Meeting,
    Vent,
    TaskComplete,

    /// <summary>Someone reported a body that was never there.</summary>
    FalseReport,
}

public static class EntropySourceExtensions
{
    /// <summary>
    /// How many entropy points the action is worth. Task completion is the only
    /// way for the crew to push the meter back down.
    /// </summary>
    public static float Weight(this EntropySource source) => source switch
    {
        EntropySource.Kill => 15f,
        EntropySource.Sabotage => 8f,
        EntropySource.Meeting => 10f,
        EntropySource.Vent => 3f,
        EntropySource.TaskComplete => -2f,

        // Doubting your own eyes feeds the thing that made you doubt them.
        EntropySource.FalseReport => 12f,
        _ => 0f,
    };
}
