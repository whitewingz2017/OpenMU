// <copyright file="BossKillReward.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.BossKillRewards;

/// <summary>
/// The points which the killer of a boss gets.
/// </summary>
public sealed class BossKillReward
{
    /// <summary>Gets or sets the number of the boss monster, e.g. 275 for Kundun.</summary>
    [Range(0, short.MaxValue)]
    [Display(Name = nameof(BossKillRewardResources.MonsterNumber), ResourceType = typeof(BossKillRewardResources))]
    public short MonsterNumber { get; set; }

    /// <summary>Gets or sets the WCoinC which the killer gets.</summary>
    [Range(0, int.MaxValue)]
    [Display(Name = nameof(BossKillRewardResources.WCoinC), ResourceType = typeof(BossKillRewardResources))]
    public int WCoinC { get; set; }

    /// <summary>Gets or sets the WCoinP which the killer gets.</summary>
    [Range(0, int.MaxValue)]
    [Display(Name = nameof(BossKillRewardResources.WCoinP), ResourceType = typeof(BossKillRewardResources))]
    public int WCoinP { get; set; }

    /// <summary>Gets or sets the Goblin Points which the killer gets.</summary>
    [Range(0, int.MaxValue)]
    [Display(Name = nameof(BossKillRewardResources.GoblinPoints), ResourceType = typeof(BossKillRewardResources))]
    public int GoblinPoints { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"#{this.MonsterNumber}: {this.WCoinC} WCoinC, {this.WCoinP} WCoinP, {this.GoblinPoints} Goblin Points";
}
