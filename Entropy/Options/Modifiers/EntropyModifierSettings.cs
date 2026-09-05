using Entropy.Modifiers;
using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.Utilities;

namespace Entropy.Options.Modifiers;

// Lobby settings for assignment, passive entropy gain, and task/kill rewards.
public class EntropyModifierSettings : AbstractOptionGroup<EntropyModifier>
{
    public override string GroupName => "Entropy";

    public override MenuCategory ParentMenu => MenuCategory.Modifiers;

    [ModdedNumberOption("Afflicted Players", 0, 15)]
    public float Amount { get; set; } = 1;

    [ModdedNumberOption("Chance", 0, 100, 10, MiraNumberSuffixes.Percent)]
    public float Chance { get; set; } = 100;

    [ModdedNumberOption("Seconds To Unravel", 30, 600, 10, MiraNumberSuffixes.Seconds)]
    public float SecondsToFill { get; set; } = 200;

    [ModdedNumberOption("Task Reprieve", 0, 40)]
    public float TaskReward { get; set; } = 15;

    [ModdedNumberOption("Kill Reprieve", 0, 40)]
    public float KillReward { get; set; } = 15;
}
