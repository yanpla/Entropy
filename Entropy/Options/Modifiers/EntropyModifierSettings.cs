using Entropy.Modifiers;
using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;

namespace Entropy.Options.Modifiers;

/// <summary>
/// The host's dial for how many people lose their grip this game.
/// </summary>
/// <remarks>
/// Off by default: the host turns it on the way they would turn on a role. The players
/// it lands on are never told, so a lobby that does not know the setting was changed
/// cannot know whether anything is wrong with them.
/// </remarks>
public class EntropyModifierSettings : AbstractOptionGroup<EntropyModifier>
{
    public override string GroupName => "Entropy";

    [ModdedNumberOption("Afflicted Players", 0, 15)]
    public float Amount { get; set; }

    [ModdedNumberOption("Chance", 0, 100, 10)]
    public float Chance { get; set; } = 100;
}
