// <copyright file="BossKillRewardPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.BossKillRewards;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.CashShop;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Gives the killer of a configured boss WCoinC, WCoinP and Goblin Points, and tells all players who killed it.
/// </summary>
/// <remarks>
/// Only the player who killed the boss gets the points, not the party. The points are saved before the
/// player is told about them, so a failed save gives nothing.
/// </remarks>
[PlugIn]
[Display(Name = nameof(BossKillRewardResources.Name), Description = nameof(BossKillRewardResources.Description), ResourceType = typeof(BossKillRewardResources))]
[Guid("30d1940d-36af-49b6-ae9c-b07574b771fb")]
public sealed class BossKillRewardPlugIn : IAttackableGotKilledPlugIn, ISupportCustomConfiguration<BossKillRewardConfiguration>, ISupportDefaultCustomConfiguration, IDisabledByDefault
{
    /// <summary>The key of the announcement in the player messages.</summary>
    internal const string AnnouncementKey = "BossKillReward_Announcement";

    /// <summary>The key of the message to the killer in the player messages.</summary>
    internal const string RewardMessageKey = "BossKillReward_Received";

    /// <inheritdoc />
    public BossKillRewardConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new BossKillRewardConfiguration();

    /// <inheritdoc />
    public async ValueTask AttackableGotKilledAsync(IAttackable killed, IAttacker? killer)
    {
        if (killed is not AttackableNpcBase monster
            || killed.IsSummonedMonster
            || this.Configuration is not { } configuration
            || (killer as Player ?? (killer as IPlayerSurrogate)?.Owner) is not { } player
            || configuration.Rewards.FirstOrDefault(r => r.Enabled && r.MonsterNumber == monster.Definition.Number) is not { } reward)
        {
            return;
        }

        if (configuration.AnnounceKills)
        {
            await player.GameContext.ShowGlobalLocalizedMessageAsync(MessageType.GoldenCenter, AnnouncementKey, player.Name, monster.Definition.Designation.ValueInNeutralLanguage).ConfigureAwait(false);
        }

        if (reward is { WCoinC: <= 0, WCoinP: <= 0, GoblinPoints: <= 0 })
        {
            return;
        }

        var granted = await player.RunPersistenceExclusiveAsync(() => GrantAsync(player, reward)).ConfigureAwait(false);
        if (granted)
        {
            await player.ShowLocalizedBlueMessageAsync(RewardMessageKey, reward.WCoinC, reward.WCoinP, reward.GoblinPoints, monster.Definition.Designation.ValueInNeutralLanguage).ConfigureAwait(false);
        }
    }

    private static async ValueTask<bool> GrantAsync(Player player, BossKillReward reward)
    {
        if (player.Account is not { } account)
        {
            return false;
        }

        await player.ApplyPendingCashShopCoinGrantsAsync().ConfigureAwait(false);
        var (oldWCoinC, oldWCoinP, oldGoblinPoints) = (account.WCoinC, account.WCoinP, account.GoblinPoints);
        var committed = false;
        try
        {
            account.WCoinC = Add(oldWCoinC, reward.WCoinC);
            account.WCoinP = Add(oldWCoinP, reward.WCoinP);
            account.GoblinPoints = Add(oldGoblinPoints, reward.GoblinPoints);
            committed = await player.SaveProgressAsync().ConfigureAwait(false);
            return committed;
        }
        catch (Exception exception)
        {
            player.Logger.LogError(exception, "Granting the boss kill reward failed for {Player}.", player);
            return false;
        }
        finally
        {
            if (!committed)
            {
                (account.WCoinC, account.WCoinP, account.GoblinPoints) = (oldWCoinC, oldWCoinP, oldGoblinPoints);
            }
        }
    }

    private static int Add(int balance, int amount) => (int)Math.Min(int.MaxValue, (long)balance + Math.Max(0, amount));
}
