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
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    [Display(Name = nameof(BossKillRewardResources.Rewards), ResourceType = typeof(BossKillRewardResources))]
    public ICollection<BossKillReward> Rewards { get; set; } = new List<BossKillReward>();
}
