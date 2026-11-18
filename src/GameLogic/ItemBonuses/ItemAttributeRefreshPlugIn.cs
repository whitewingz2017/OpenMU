// <copyright file="ItemAttributeRefreshPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.ItemBonuses;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Applies the base power-up attributes which an administrator changes in the admin panel to the items players wear,
/// without a restart and without taking the items off: every few seconds it compares each worn item's attributes with
/// the ones it was given and gives it the changed ones.
/// </summary>
[PlugIn]
[Display(Name = nameof(ItemBonusResources.RefreshName), Description = nameof(ItemBonusResources.RefreshDescription), ResourceType = typeof(ItemBonusResources))]
[Guid("b6d25e04-8a3f-4c71-9d1e-52f7a0c8e9b3")]
public sealed class ItemAttributeRefreshPlugIn : IPeriodicTaskPlugIn
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(3);

    // The attributes' signature each worn item was given its power-ups with.
    private static readonly ConditionalWeakTable<Item, StrongBox<int>> Signatures = new();

    private DateTime _nextCheckUtc = DateTime.MinValue;

    /// <summary>Gets a signature of an item definition's base power-up attributes, with their level bonuses.</summary>
    /// <param name="definition">The definition.</param>
    /// <returns>The signature.</returns>
    public static int Signature(ItemDefinition? definition)
    {
        var hash = default(HashCode);
        foreach (var attribute in definition?.BasePowerUpAttributes ?? Enumerable.Empty<ItemBasePowerUpDefinition>())
        {
            hash.Add(attribute.TargetAttribute?.Id);
            hash.Add(attribute.AggregateType);
            hash.Add(attribute.BaseValue);
            foreach (var bonus in attribute.BonusPerLevelTable?.BonusPerLevel ?? Enumerable.Empty<LevelBonus>())
            {
                hash.Add(bonus.Level);
                hash.Add(bonus.AdditionalValue);
            }
        }

        return hash.ToHashCode();
    }

    /// <inheritdoc />
    public void ForceStart() => this._nextCheckUtc = DateTime.MinValue;

    /// <inheritdoc />
    public async ValueTask ExecuteTaskAsync(GameContext gameContext)
    {
        if (DateTime.UtcNow < this._nextCheckUtc)
        {
            return;
        }

        this._nextCheckUtc = DateTime.UtcNow + Interval;
        foreach (var player in await gameContext.GetPlayersAsync().ConfigureAwait(false))
        {
            if (player.PlayerState.CurrentState != PlayerState.EnteredWorld || player.Attributes is null || player.Inventory is null)
            {
                continue;
            }

            await player.RunPersistenceExclusiveAsync(() =>
            {
                Refresh(player);
                return ValueTask.CompletedTask;
            }).ConfigureAwait(false);
        }
    }

    private static void Refresh(Player player)
    {
        if (player.Attributes is not { } attributes || player.Inventory is not { } inventory)
        {
            return;
        }

        foreach (var item in inventory.EquippedItems.ToList())
        {
            if (!attributes.ItemPowerUps.ContainsKey(item))
            {
                continue;
            }

            var signature = Signature(item.Definition);
            var given = Signatures.GetValue(item, _ => new StrongBox<int>(signature));
            if (given.Value == signature)
            {
                continue;
            }

            // The admin panel changed the definition: the item gets its power-ups again, as when it's put on.
            given.Value = signature;
            if (attributes.ItemPowerUps.Remove(item, out var old))
            {
                foreach (var powerUp in old)
                {
                    powerUp.Dispose();
                }
            }

            attributes.ItemPowerUps.Add(item, player.GameContext.ItemPowerUpFactory.GetPowerUps(item, attributes).ToList());
        }
    }
}
