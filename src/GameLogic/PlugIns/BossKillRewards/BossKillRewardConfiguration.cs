// <copyright file="BossKillRewardConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.BossKillRewards;

using MUnique.OpenMU.DataModel.Composition;

/// <summary>
/// The configuration of the <see cref="BossKillRewardPlugIn"/>.
/// </summary>
public sealed class BossKillRewardConfiguration
{
    /// <summary>Gets or sets a value indicating whether all players are told who killed a boss of the <see cref="Rewards"/>.</summary>
    [Display(Name = nameof(BossKillRewardResources.AnnounceKills), ResourceType = typeof(BossKillRewardResources))]
    public bool AnnounceKills { get; set; } = true;

    /// <summary>Gets or sets the rewards per boss. A boss which isn't listed gives no points.</summary>
    /// <remarks>
    /// By default the bosses of the Resonance Core drops, each with 5 Goblin Points.
    /// The names are the ones of the monster list of the admin panel.
    /// </remarks>
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    [Display(Name = nameof(BossKillRewardResources.Rewards), ResourceType = typeof(BossKillRewardResources))]
    public ICollection<BossKillReward> Rewards { get; set; } = new List<BossKillReward>
    {
        new() { MonsterNumber = 38, GoblinPoints = 5 }, // Balrog
        new() { MonsterNumber = 49, GoblinPoints = 5 }, // Hydra
        new() { MonsterNumber = 77, GoblinPoints = 5 }, // Phoenix of Darkness
        new() { MonsterNumber = 43, GoblinPoints = 5 }, // Golden Budge Dragon
        new() { MonsterNumber = 44, GoblinPoints = 5 }, // Red Dragon
        new() { MonsterNumber = 53, GoblinPoints = 5 }, // Golden Titan
        new() { MonsterNumber = 78, GoblinPoints = 5 }, // Golden Goblin
        new() { MonsterNumber = 79, GoblinPoints = 5 }, // Golden Dragon
        new() { MonsterNumber = 80, GoblinPoints = 5 }, // Golden Lizard King
        new() { MonsterNumber = 82, GoblinPoints = 5 }, // Golden Tantallos
        new() { MonsterNumber = 135, GoblinPoints = 5 }, // White Wizard
        new() { MonsterNumber = 275, GoblinPoints = 5 }, // Kundun
        new() { MonsterNumber = 295, GoblinPoints = 5 }, // Erohim
        new() { MonsterNumber = 340, GoblinPoints = 5 }, // Dark Elf
        new() { MonsterNumber = 349, GoblinPoints = 5 }, // Balgass
        new() { MonsterNumber = 361, GoblinPoints = 5 }, // Nightmare
        new() { MonsterNumber = 362, GoblinPoints = 5 }, // Maya left hand
        new() { MonsterNumber = 363, GoblinPoints = 5 }, // Maya right hand
        new() { MonsterNumber = 364, GoblinPoints = 5 }, // Maya
        new() { MonsterNumber = 459, GoblinPoints = 5 }, // Selupan
        new() { MonsterNumber = 561, GoblinPoints = 5 }, // Medusa
        new() { MonsterNumber = 504, GoblinPoints = 5 }, // Gayion the Gladiator
        new() { MonsterNumber = 505, GoblinPoints = 5 }, // Jerry
        new() { MonsterNumber = 506, GoblinPoints = 5 }, // Raymond
        new() { MonsterNumber = 507, GoblinPoints = 5 }, // Lucas
        new() { MonsterNumber = 508, GoblinPoints = 5 }, // Fred
        new() { MonsterNumber = 509, GoblinPoints = 5 }, // Hammerize
        new() { MonsterNumber = 510, GoblinPoints = 5 }, // Dual Berserker
        new() { MonsterNumber = 511, GoblinPoints = 5 }, // Devil Lord
        new() { MonsterNumber = 529, GoblinPoints = 5 }, // Terrible Butcher
        new() { MonsterNumber = 530, GoblinPoints = 5 }, // Mad Butcher
    };
}
