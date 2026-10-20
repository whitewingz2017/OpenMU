// <copyright file="CashShopCatalogItems.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Persistence.Initialization.Properties;

/// <summary>
/// Supplies item identities and cell footprints referenced by the bundled Season 6 shop script.
/// </summary>
internal class CashShopCatalogItems(IContext context, GameConfiguration gameConfiguration) : InitializerBase(context, gameConfiguration)
{
    private static readonly (byte Group, short Number, byte Width, byte Height)[] Items =
    [
        (12, 12, 1, 1),
        (12, 14, 1, 1),
        (12, 17, 1, 1),
        (12, 18, 1, 1),
        (12, 19, 1, 1),
        (12, 30, 1, 1),
        (12, 31, 1, 1),
        (12, 44, 1, 1),
        (12, 45, 1, 1),
        (12, 46, 1, 1),
        (12, 47, 1, 1),
        (12, 131, 3, 2),
        (12, 132, 3, 2),
        (12, 133, 3, 2),
        (12, 134, 3, 2),
        (12, 135, 2, 2),
        (12, 136, 1, 1),
        (12, 140, 1, 1),
        (12, 141, 1, 1),
        (13, 20, 1, 1),
        (13, 43, 1, 1),
        (13, 44, 1, 1),
        (13, 45, 1, 1),
        (13, 46, 1, 1),
        (13, 47, 1, 1),
        (13, 48, 1, 1),
        (13, 54, 1, 1),
        (13, 55, 1, 1),
        (13, 56, 1, 1),
        (13, 57, 1, 1),
        (13, 58, 1, 1),
        (13, 61, 1, 1),
        (13, 62, 1, 1),
        (13, 63, 1, 1),
        (13, 64, 1, 1),
        (13, 65, 1, 1),
        (13, 69, 1, 1),
        (13, 70, 1, 1),
        (13, 76, 1, 1),
        (13, 80, 1, 1),
        (13, 93, 1, 1),
        (13, 94, 1, 1),
        (13, 104, 1, 1),
        (13, 105, 1, 1),
        (13, 106, 1, 1),
        (13, 107, 1, 1),
        (13, 109, 1, 1),
        (13, 110, 1, 1),
        (13, 111, 1, 1),
        (13, 112, 1, 1),
        (13, 113, 1, 1),
        (13, 114, 1, 1),
        (13, 115, 1, 1),
        (13, 122, 1, 1),
        (13, 123, 1, 1),
        (13, 127, 1, 1),
        (13, 128, 1, 1),
        (13, 129, 1, 1),
        (13, 130, 1, 1),
        (13, 132, 1, 2),
        (13, 134, 1, 1),
        (13, 135, 1, 1),
        (13, 136, 1, 1),
        (13, 137, 1, 1),
        (13, 138, 1, 1),
        (13, 139, 1, 1),
        (13, 140, 1, 1),
        (13, 141, 1, 1),
        (13, 142, 1, 1),
        (13, 143, 1, 1),
        (13, 144, 1, 1),
        (14, 11, 1, 1),
        (14, 53, 1, 1),
        (14, 70, 1, 1),
        (14, 71, 1, 1),
        (14, 72, 2, 2),
        (14, 73, 2, 2),
        (14, 74, 2, 2),
        (14, 75, 2, 2),
        (14, 76, 2, 2),
        (14, 77, 2, 2),
        (14, 85, 1, 2),
        (14, 86, 1, 1),
        (14, 87, 1, 1),
        (14, 91, 1, 1),
        (14, 92, 1, 1),
        (14, 95, 1, 1),
        (14, 97, 1, 1),
        (14, 98, 1, 1),
        (14, 112, 1, 1),
        (14, 113, 1, 1),
        (14, 120, 1, 1),
        (14, 133, 1, 1),
        (14, 146, 1, 1),
        (14, 149, 1, 1),
        (14, 162, 1, 1),
        (14, 163, 1, 1),
        (14, 169, 1, 1),
        (15, 8, 1, 2),
        (15, 12, 1, 2),
        (15, 15, 1, 2),
        (15, 18, 1, 2),
        (15, 19, 1, 2),
        (15, 21, 1, 2),
        (15, 22, 1, 2),
        (15, 23, 1, 2),
        (15, 28, 1, 2),
        (15, 29, 1, 2),
        (15, 31, 1, 2),
        (15, 33, 1, 2),
        (15, 35, 1, 2),
        (15, 36, 1, 2),
    ];

    /// <inheritdoc />
    public override void Initialize()
    {
        foreach (var (group, number, width, height) in Items)
        {
            if (this.GameConfiguration.Items.Any(item => item.Group == group && item.Number == number))
            {
                continue;
            }

            var item = this.Context.CreateNew<ItemDefinition>();
            item.Group = group;
            item.Number = number;
            item.Name = ItemNames.ResourceManager.GetLocalizedString($"CashShop_{group}_{number}");
            item.Width = width;
            item.Height = height;
            item.Durability = (byte)(group == 14 && number is 70 or 71 or 133 ? 255 : 1);
            item.IsDroppable = false;
            item.IsTradable = false;
            item.IsRepairable = false;
            item.SetGuid(group, number);
            if (group == 13 && number is 107 or >= 109 and <= 115)
            {
                byte slot = number >= 113 ? (byte)9 : (byte)10;
                item.ItemSlot = this.GameConfiguration.ItemSlotTypes.First(type => type.ItemSlots.Contains(slot));
                item.Durability = 255;
                foreach (var characterClass in this.GameConfiguration.CharacterClasses)
                {
                    item.QualifiedCharacters.Add(characterClass);
                }
            }

            this.GameConfiguration.Items.Add(item);
        }

        this.ConfigureAccessory(107, (Stats.MinimumPhysBaseDmg, 1.15f, AggregateType.Multiplicate), (Stats.MaximumPhysBaseDmg, 1.15f, AggregateType.Multiplicate), (Stats.MinimumWizBaseDmg, 1.15f, AggregateType.Multiplicate), (Stats.MaximumWizBaseDmg, 1.15f, AggregateType.Multiplicate), (Stats.AttackSpeedAny, 10f, AggregateType.AddRaw));
        this.ConfigureAccessory(109, (Stats.MaximumMana, 1.04f, AggregateType.Multiplicate));
        this.ConfigureAccessory(110, (Stats.MaximumHealth, 1.04f, AggregateType.Multiplicate));
        this.ConfigureAccessory(111, (Stats.MoneyAmountRate, 1.5f, AggregateType.Multiplicate));
        this.ConfigureAccessory(112, (Stats.DamageReceiveDecrement, .96f, AggregateType.Multiplicate));
        this.ConfigureAccessory(113, (Stats.ExcellentDamageChance, .1f, AggregateType.AddRaw));
        this.ConfigureAccessory(114, (Stats.AttackSpeedAny, 7f, AggregateType.AddRaw));
        this.ConfigureAccessory(115, (Stats.ManaAfterMonsterKillMultiplier, .08f, AggregateType.AddRaw));
    }

    private void ConfigureAccessory(short number, params (AttributeDefinition Attribute, float Value, AggregateType Type)[] bonuses)
    {
        var item = this.GameConfiguration.Items.Single(item => item.Group == 13 && item.Number == number);
        if (item.BasePowerUpAttributes.Count != 0)
        {
            return;
        }

        foreach (var (attribute, value, aggregate) in bonuses)
        {
            item.BasePowerUpAttributes.Add(this.CreateItemBasePowerUpDefinition(attribute, value, aggregate));
        }

        if (number != 107)
        {
            item.BasePowerUpAttributes.Add(this.CreateItemBasePowerUpDefinition(Stats.HealthRecoveryMultiplier, .03f, AggregateType.AddRaw));
        }
    }
}
