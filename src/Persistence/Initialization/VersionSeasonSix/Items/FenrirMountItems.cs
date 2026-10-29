// <copyright file="FenrirMountItems.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic;

/// <summary>Gives the imported Fenrir models the normal configured Fenrir riding behavior.</summary>
internal static class FenrirMountItems
{
    /// <summary>Copies normal Fenrir configuration into the existing imported item without replacing its identifier.</summary>
    /// <param name="context">The configuration context.</param>
    /// <param name="configuration">The target configuration.</param>
    /// <param name="group">The imported item group.</param>
    /// <param name="number">The imported item number.</param>
    public static void Ensure(IContext context, GameConfiguration configuration, byte group, short number)
    {
        var original = configuration.Items.SingleOrDefault(d => d.Group == ItemConstants.Fenrir.Group && d.Number == ItemConstants.Fenrir.Number)
            ?? throw new InvalidOperationException("The normal Fenrir definition is required before installing custom Fenrir mounts.");
        var mount = configuration.Items.Single(d => d.Group == group && d.Number == number);
        mount.ItemSlot = original.ItemSlot;
        mount.Skill = original.Skill;
        mount.Durability = original.Durability;
        mount.Width = original.Width;
        mount.Height = original.Height;
        mount.DropLevel = original.DropLevel;
        mount.MaximumItemLevel = original.MaximumItemLevel;
        mount.IsRepairable = original.IsRepairable;
        mount.QualifiedCharacters.Clear();
        foreach (var characterClass in original.QualifiedCharacters)
        {
            mount.QualifiedCharacters.Add(characterClass);
        }

        foreach (var source in original.BasePowerUpAttributes)
        {
            var powerUp = mount.BasePowerUpAttributes.SingleOrDefault(p => p.TargetAttribute == source.TargetAttribute);
            if (powerUp is null)
            {
                powerUp = context.CreateNew<ItemBasePowerUpDefinition>();
                mount.BasePowerUpAttributes.Add(powerUp);
            }

            powerUp.TargetAttribute = source.TargetAttribute;
            powerUp.BaseValue = source.BaseValue;
            powerUp.AggregateType = source.AggregateType;
            powerUp.BonusPerLevelTable = source.BonusPerLevelTable;
        }

        foreach (var source in original.Requirements)
        {
            var requirement = mount.Requirements.SingleOrDefault(r => r.Attribute == source.Attribute);
            if (requirement is null)
            {
                requirement = context.CreateNew<AttributeRequirement>();
                mount.Requirements.Add(requirement);
            }

            requirement.Attribute = source.Attribute;
            requirement.MinimumValue = source.MinimumValue;
        }
    }
}
