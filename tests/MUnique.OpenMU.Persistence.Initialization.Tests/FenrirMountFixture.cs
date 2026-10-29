// <copyright file="FenrirMountFixture.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;

/// <summary>Provides the ordinary Fenrir dependency to minimal configuration update fixtures.</summary>
internal static class FenrirMountFixture
{
    public static void Add(IContext context, GameConfiguration configuration)
    {
        foreach (var number in Enum.GetValues<CharacterClasses.CharacterClassNumber>())
        {
            var characterClass = context.CreateNew<CharacterClass>();
            characterClass.Number = (byte)number;
            configuration.CharacterClasses.Add(characterClass);
        }

        var slot = context.CreateNew<ItemSlotType>();
        slot.SetGuid(8);
        slot.ItemSlots.Add(8);
        configuration.ItemSlotTypes.Add(slot);
        var skill = context.CreateNew<Skill>();
        skill.DamageType = DamageType.Fenrir;
        var mount = context.CreateNew<ItemDefinition>();
        mount.SetGuid(13, 37);
        mount.Group = 13;
        mount.Number = 37;
        mount.Name = "Horn of Fenrir";
        mount.ItemSlot = slot;
        mount.Skill = skill;
        mount.Width = 2;
        mount.Height = 2;
        mount.Durability = 255;
        configuration.Items.Add(mount);
    }
}
