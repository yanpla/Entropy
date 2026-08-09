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
        _ => 0f,
    };
}
