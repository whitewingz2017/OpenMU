// <copyright file="TigrisSocketItems.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;

using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.Interfaces;

/// <summary>Adds Rage Fighter socket equipment using the existing Phoenix Soul stat balance.</summary>
public static class TigrisSocketItems
{
    /// <summary>The Tigris set number shared with both client catalogs.</summary>
    public const short SetNumber = 146;

    /// <summary>The Pantera weapon number shared with both client catalogs.</summary>
    public const short WeaponNumber = 52;

    private const short ArmorTemplateNumber = 73;
    private const short WeaponTemplateNumber = 35;
    private const byte DefenseRateSetDiscriminator = 0xFF;

    /// <summary>Adds missing items and set bonuses without replacing existing definitions.</summary>
    /// <param name="context">The configuration persistence context.</param>
    /// <param name="configuration">The Season 6 configuration.</param>
    public static void Ensure(IContext context, GameConfiguration configuration)
    {
        (byte Group, short Number, LocalizedString Name)[] items =
        [
            (7, SetNumber, LocalizedString.FromResource(() => TigrisResources.TigrisHelm)),
            (8, SetNumber, LocalizedString.FromResource(() => TigrisResources.TigrisArmor)),
            (9, SetNumber, LocalizedString.FromResource(() => TigrisResources.TigrisPants)),
            (11, SetNumber, LocalizedString.FromResource(() => TigrisResources.TigrisBoots)),
            (0, WeaponNumber, LocalizedString.FromResource(() => TigrisResources.PanteraClaws)),
        ];
        foreach (var entry in items)
        {
            var existing = configuration.Items.SingleOrDefault(i => i.Group == entry.Group && i.Number == entry.Number);
            if (existing is not null && existing.Name.SourceKey != entry.Name.SourceKey)
            {
                throw new InvalidOperationException($"Item {entry.Group}:{entry.Number} is already used by {existing.Name}.");
            }

            _ = configuration.Items.Single(i => i.Group == entry.Group && i.Number == (entry.Group == 0 ? WeaponTemplateNumber : ArmorTemplateNumber));
        }

        foreach (var entry in items.Where(entry => !configuration.Items.Any(i => i.Group == entry.Group && i.Number == entry.Number)))
        {
            AddItem(context, configuration, entry.Group, entry.Number, entry.Name);
        }

        AddSetBonuses(context, configuration);
    }

    private static void AddItem(IContext context, GameConfiguration configuration, byte group, short number, LocalizedString name)
    {
        var source = configuration.Items.Single(i => i.Group == group && i.Number == (group == 0 ? WeaponTemplateNumber : ArmorTemplateNumber));
        var socketTemplate = configuration.Items.Single(i => i.Group == (group == 0 ? 0 : 8) && i.Number == (group == 0 ? 26 : 52));
        var item = context.CreateNew<ItemDefinition>();
        item.Group = group;
        item.Number = number;
        item.SetGuid(group, number);
        item.Name = name;
        item.Width = source.Width;
        item.Height = source.Height;
        item.DropLevel = source.DropLevel;
        item.DropsFromMonsters = true;
        item.MaximumItemLevel = source.MaximumItemLevel;
        item.Durability = source.Durability;
        item.ItemSlot = source.ItemSlot;
        item.Skill = source.Skill;
        item.MaximumSockets = socketTemplate.MaximumSockets;
        foreach (var characterClass in source.QualifiedCharacters)
        {
            item.QualifiedCharacters.Add(characterClass);
        }

        CopyAttributes(context, source, item);
        foreach (var option in socketTemplate.PossibleItemOptions)
        {
            item.PossibleItemOptions.Add(option);
        }

        configuration.Items.Add(item);
    }

    private static void CopyAttributes(IContext context, ItemDefinition source, ItemDefinition item)
    {
        foreach (var requirement in source.Requirements)
        {
            var copy = context.CreateNew<AttributeRequirement>();
            copy.Attribute = requirement.Attribute;
            copy.MinimumValue = requirement.MinimumValue;
            item.Requirements.Add(copy);
        }

        foreach (var powerUp in source.BasePowerUpAttributes)
        {
            var copy = context.CreateNew<ItemBasePowerUpDefinition>();
            copy.TargetAttribute = powerUp.TargetAttribute;
            copy.BaseValue = powerUp.BaseValue;
            copy.AggregateType = powerUp.AggregateType;
            copy.BonusPerLevelTable = powerUp.BonusPerLevelTable;
            item.BasePowerUpAttributes.Add(copy);
        }
    }

    private static void AddSetBonuses(IContext context, GameConfiguration configuration)
    {
        var armor = configuration.Items.Single(i => i.Group == 8 && i.Number == SetNumber);
        var source = configuration.Items.Single(i => i.Group == 8 && i.Number == ArmorTemplateNumber);
        foreach (var template in source.PossibleItemSetGroups.Where(s => s.AlwaysApplies))
        {
            if (armor.PossibleItemSetGroups.Any(s => s.AlwaysApplies && s.SetLevel == template.SetLevel))
            {
                continue;
            }

            var set = context.CreateNew<ItemSetGroup>();
            set.Name = LocalizedString.FromResource(() => TigrisResources.TigrisSet);
            set.AlwaysApplies = true;
            set.CountDistinct = template.CountDistinct;
            set.MinimumItemCount = template.MinimumItemCount;
            set.SetLevel = template.SetLevel;
            set.Options = template.Options;
            foreach (var item in configuration.Items.Where(i => i.Group is 7 or 8 or 9 or 11 && i.Number == SetNumber))
            {
                var member = context.CreateNew<ItemOfItemSet>();
                member.SetGuid(item.Group, item.Number, set.SetLevel == 0 ? DefenseRateSetDiscriminator : (byte)set.SetLevel);
                member.ItemDefinition = item;
                member.ItemSetGroup = set;
                set.Items.Add(member);
                item.PossibleItemSetGroups.Add(set);
            }

            configuration.ItemSetGroups.Add(set);
        }
    }
}
