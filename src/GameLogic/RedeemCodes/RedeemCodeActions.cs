// <copyright file="RedeemCodeActions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.RedeemCodes;

using System.Runtime.CompilerServices;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Views.Inventory;

/// <summary>Server-authoritative reward delivery, limited and saved atomically with the receipt.</summary>
public sealed class RedeemCodeActions
{
    private readonly ConditionalWeakTable<Player, Attempt> _attempts = new();

    /// <summary>Redeems a code under the same lock as inventory actions and saves.</summary>
    public ValueTask<string> RedeemAsync(Player player, string text)
        => player.RunPersistenceExclusiveAsync(() => this.RedeemCoreAsync(player, text));

    private async ValueTask<string> RedeemCoreAsync(Player player, string text)
    {
        if (player.Account is not { } account || player.SelectedCharacter is not { } character || player.Inventory is null
            || player.IsTemplatePlayer || player.PlayerState.CurrentState != PlayerState.EnteredWorld)
        {
            return "Enter the game before redeeming a code.";
        }
        var attempt = this._attempts.GetValue(player, _ => new());
        if (DateTime.UtcNow < attempt.Next) return "Wait a few seconds before trying another code.";
        attempt.Next = DateTime.UtcNow.AddSeconds(3);
        if (player.OpenedNpc != null || player.TemporaryStorage?.Items.Any() == true || player.ShopStorage?.StoreOpen == true || player.IsCashShopOpen)
            return "Close shops, trade, warehouse and mixing windows before redeeming.";
        var normalized = RedeemCodeRules.Normalize(text);
        if (normalized.Length == 0) return "Invalid redeem code.";
        var code = await player.PersistenceContext.GetByIdAsync<RedeemCode>(RedeemCodeRules.Key(normalized)).ConfigureAwait(false);
        if (code == null) return "Invalid redeem code.";
        try
        {
            if (!code.Enabled || code.ExpiresAt <= DateTime.UtcNow || (code.MaximumUses > 0 && code.UsedCount >= code.MaximumUses))
                return "This redeem code is disabled, expired or fully claimed.";
            var receiptId = RedeemCodeRules.Key($"{code.Id:N}:{(code.PerCharacter ? character.GetId() : account.GetId()):N}");
            var previous = await player.PersistenceContext.GetByIdAsync<RedeemCodeClaim>(receiptId).ConfigureAwait(false);
            if (previous != null)
            {
                player.PersistenceContext.Detach(previous);
                return "You have already redeemed this code.";
            }
            IReadOnlyList<RedeemItemReward> rewards;
            try { rewards = RedeemCodeRules.Rewards(code); }
            catch (Exception ex) when (ex is ArgumentException or System.Text.Json.JsonException) { return "This code has invalid rewards. Contact an administrator."; }
            return await GrantAsync(player, code, receiptId, rewards).ConfigureAwait(false);
        }
        finally { player.PersistenceContext.Detach(code); }
    }

    private static async ValueTask<string> GrantAsync(Player player, RedeemCode code, Guid receiptId, IReadOnlyList<RedeemItemReward> rewards)
    {
        var account = player.Account!;
        var oldC = account.WCoinC; var oldP = account.WCoinP; var oldG = account.GoblinPoints;
        if ((long)oldC + code.WCoinC > int.MaxValue || (long)oldP + code.WCoinP > int.MaxValue || (long)oldG + code.GoblinPoints > int.MaxValue || code.UsedCount == int.MaxValue)
            return "Your reward balance would exceed its limit.";
        var definitions = rewards.Select(r => (Reward: r, Definition: player.GameContext.Configuration.Items.FirstOrDefault(d => d.Group * 512 + d.Number == r.ItemCode))).ToList();
        if (definitions.Any(r => r.Definition == null)) return "A reward item is unavailable. Contact an administrator.";
        var added = new List<Item>();
        RedeemCodeClaim? claim = null;
        var committed = false;
        var oldCount = code.UsedCount; var oldRevision = code.Revision;
        try
        {
            foreach (var (reward, definition) in definitions)
            {
                for (var i = 0; i < reward.Count; i++)
                {
                    var item = player.PersistenceContext.CreateNew<Item>();
                    item.Definition = definition!; item.Level = reward.Level; item.Durability = definition!.Durability;
                    added.Add(item);
                    if (!await player.Inventory!.AddItemAsync(item).ConfigureAwait(false)) return "Not enough inventory space. Free space and try again.";
                }
            }
            account.WCoinC += code.WCoinC; account.WCoinP += code.WCoinP; account.GoblinPoints += code.GoblinPoints;
            claim = player.PersistenceContext.CreateNew<RedeemCodeClaim>();
            claim.Id = receiptId; claim.CodeId = code.Id; claim.AccountId = account.GetId(); claim.CharacterId = player.SelectedCharacter!.GetId();
            claim.CharacterName = player.SelectedCharacter!.Name; claim.RedeemedAt = DateTime.UtcNow;
            code.UsedCount++; code.Revision = Guid.NewGuid();
            if (!await player.SaveProgressAsync().ConfigureAwait(false)) return "The code could not be saved. Please try again.";
            committed = true;
        }
        catch (Exception ex)
        {
            player.Logger.LogWarning(ex, "Redeem code transaction failed for account {AccountId}", account.GetId());
            return "The code could not be saved or was claimed concurrently. Please try again.";
        }
        finally
        {
            if (!committed)
            {
                code.UsedCount = oldCount; code.Revision = oldRevision;
                account.WCoinC = oldC; account.WCoinP = oldP; account.GoblinPoints = oldG;
                foreach (var item in added)
                {
                    if (player.Inventory!.Items.Contains(item)) await player.Inventory.RemoveItemAsync(item).ConfigureAwait(false);
                    player.PersistenceContext.Detach(item);
                }
            }
            if (claim != null) player.PersistenceContext.Detach(claim);
        }
        foreach (var item in added) await player.InvokeViewPlugInAsync<IItemAppearPlugIn>(v => v.ItemAppearAsync(item)).ConfigureAwait(false);
        return "Redeem code claimed successfully. Item and coin/point rewards have been credited.";
    }

    private sealed class Attempt { public DateTime Next { get; set; } }
}
