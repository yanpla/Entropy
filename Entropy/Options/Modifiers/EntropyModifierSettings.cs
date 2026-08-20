using Entropy.Modifiers;
using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.Utilities;

namespace Entropy.Options.Modifiers;

/// <summary>
/// The host's dials for how many people lose their grip, and how fast.
/// </summary>
/// <remarks>
/// Off by default: the host turns it on the way they would turn on a role. The players
/// it lands on are never told, so a lobby that does not know the setting was changed
/// cannot know whether anything is wrong with them.
/// </remarks>
public class EntropyModifierSettings : AbstractOptionGroup<EntropyModifier>
{
    public override string GroupName => "Entropy";

    public override MenuCategory ParentMenu => MenuCategory.Modifiers;

    [ModdedNumberOption("Afflicted Players", 0, 15)]
    public float Amount { get; set; }

    [ModdedNumberOption("Chance", 0, 100, 10, MiraNumberSuffixes.Percent)]
    public float Chance { get; set; } = 100;

    /// <summary>
    /// How long doing nothing takes a player from calm to falling apart. The whole
    /// balance hangs off this: the rewards below are only worth anything relative to it.
    /// </summary>
    [ModdedNumberOption("Seconds To Unravel", 30, 600, 10, MiraNumberSuffixes.Seconds)]
    public float SecondsToFill { get; set; } = 200;

    /// <summary>How much ground a crewmate wins back per task.</summary>
    [ModdedNumberOption("Task Reprieve", 0, 40)]
    public float TaskReward { get; set; } = 15;

    /// <summary>How much ground an impostor wins back per kill.</summary>
    [ModdedNumberOption("Kill Reprieve", 0, 40)]
    public float KillReward { get; set; } = 15;
}
