using Entropy.Options.Modifiers;
using MiraAPI.GameOptions;

namespace Entropy;

// Actions that change entropy.
public enum EntropySource : byte
{
    Sabotage,
    TaskComplete,
    Kill,
    FalseReport,
}

public static class EntropySourceExtensions
{
    public static float Weight(this EntropySource source)
    {
        var settings = OptionGroupSingleton<EntropyModifierSettings>.Instance;

        return source switch
        {
            EntropySource.Sabotage => 8f,

            // Lobby rewards are positive values; applying them reduces entropy.
            EntropySource.TaskComplete => -settings.TaskReward,
            EntropySource.Kill => -settings.KillReward,

            EntropySource.FalseReport => 12f,
            _ => 0f,
        };
    }

    public static bool AffectsEveryone(this EntropySource source) => source is EntropySource.Sabotage;
}
