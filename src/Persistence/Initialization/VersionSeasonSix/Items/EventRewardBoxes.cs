// <copyright file="EventRewardBoxes.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;

using MUnique.OpenMU.DataModel.Configuration;

/// <summary>Gives the standalone firecracker the same editable rewards as Box of Luck +2.</summary>
internal static class EventRewardBoxes
{
    /// <summary>Replaces only the original fireworks-only configuration, preserving customized reward lists.</summary>
    /// <param name="context">The persistence context.</param>
    /// <param name="configuration">The game configuration.</param>
    internal static void Ensure(IContext context, GameConfiguration configuration)
    {
        var box = configuration.Items.SingleOrDefault(item => item.Group == 14 && item.Number == 11);
        var firecracker = configuration.Items.SingleOrDefault(item => item.Group == 14 && item.Number == 63);
        if (box is null || firecracker is null || firecracker.DropItems.Count != 1)
        {
            return;
        }

        var effect = firecracker.DropItems.Single();
        if (effect.SourceItemLevel != 0 || effect.ItemType != SpecialItemType.None || effect.PossibleItems.Count != 0
            || effect.DropEffect != ItemDropEffect.Fireworks || effect.Chance != 1)
        {
            return;
        }

        var rewards = box.DropItems.Where(group => group.SourceItemLevel == 2).ToArray();
        for (var index = 0; index < rewards.Length; index++)
        {
            var target = index == 0 ? effect : context.CreateNew<ItemDropItemGroup>();
            Copy(rewards[index], target);
            if (index > 0)
            {
                firecracker.DropItems.Add(target);
            }
        }
    }

    private static void Copy(ItemDropItemGroup source, ItemDropItemGroup target)
    {
        target.Description = source.Description;
        target.SourceItemLevel = 0;
        target.ItemType = source.ItemType;
        target.Chance = source.Chance;
        target.MinimumLevel = source.MinimumLevel;
        target.MaximumLevel = source.MaximumLevel;
        target.MoneyAmount = source.MoneyAmount;
        target.RequiredCharacterLevel = source.RequiredCharacterLevel;
        target.DropEffect = ItemDropEffect.Fireworks;
        target.HasLuck = source.HasLuck;
        target.HasSkill = source.HasSkill;
        target.OptionLevel = source.OptionLevel;
        target.ExcellentOptions = source.ExcellentOptions;
        foreach (var item in source.PossibleItems)
        {
            target.PossibleItems.Add(item);
        }
    }
}
