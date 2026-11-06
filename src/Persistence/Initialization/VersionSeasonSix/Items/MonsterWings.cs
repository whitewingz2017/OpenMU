// <copyright file="MonsterWings.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.ItemCrafting;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.PlayerActions.Craftings;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Maps;

/// <summary>
/// Adds the monster wings (2.5 wings) 12:262-265, their materials 14:176-179, the material drops and the
/// Chaos Goblin crafting. Existing entries are kept, so administrators' settings are not overwritten.
/// </summary>
/// <remarks>
/// Recipe of CChaosBox::MonsterWingMix (Pegasus): a second wing (rate = price / 9,000,000) or a cape
/// (rate = price / 500,000), a Jewel of Chaos, a Jewel of Creation and one material; at most 60 %,
/// 100,000 zen per percent. The Death King doesn't exist in OpenMU, so its bone drops in the Lost Tower.
/// </remarks>
public static class MonsterWings
{
    /// <summary>
    /// The crafting number (mix type) the client sends.
    /// </summary>
    internal const byte CraftingNumber = 56;

    private const short FirstWingNumber = 262;

    private static readonly (short Number, string Name)[] Materials =
    [
        (176, "Death King's Bone"),
        (177, "Hell Maine's Leather"),
        (178, "Dark Phoenix Flame"),
        (179, "Death Beam Knight's Soul"),
    ];

    /// <summary>
    /// Adds the missing wings, materials, drops and the crafting.
    /// </summary>
    /// <param name="context">The persistence context.</param>
    /// <param name="config">The game configuration.</param>
    public static void Ensure(IContext context, GameConfiguration config)
    {
        var materials = Materials.Select(m => EnsureMaterial(context, config, m.Number, m.Name)).ToArray();
        new Wings(context, config).AddMonsterWings();

        EnsureMapDrop(context, config, LostTower.Number, 60, 0.001, materials[0]);
        EnsureMonsterDrop(context, config, 309, 0.01, materials[1]); // Hell Maine
        EnsureMonsterDrop(context, config, 77, 0.01, materials[2]); // Dark Phoenix
        EnsureMonsterDrop(context, config, 63, 0.01, materials[3]); // Death Beam Knight

        EnsureCrafting(context, config, materials);
    }

    private static ItemDefinition EnsureMaterial(IContext context, GameConfiguration config, short number, string name)
    {
        var item = config.Items.SingleOrDefault(d => d.Group == 14 && d.Number == number);
        if (item is not null && item.Name.ValueInNeutralLanguage != name)
        {
            throw new InvalidOperationException($"Monster wing material item 14:{number} is already in use.");
        }

        if (item is null)
        {
            item = context.CreateNew<ItemDefinition>();
            item.SetGuid(14, number);
            item.Group = 14;
            item.Number = number;
            item.Name = name;
            item.Width = 1;
            item.Height = 1;
            item.Durability = 1;
            item.DropsFromMonsters = false;
            config.Items.Add(item);
        }

        return item;
    }

    private static DropItemGroup? EnsureDropGroup(IContext context, GameConfiguration config, double chance, ItemDefinition material)
    {
        var id = GuidHelper.CreateGuid<DropItemGroup>(FirstWingNumber, material.Number);
        if (config.DropItemGroups.Any(group => group.GetId() == id))
        {
            return null;
        }

        var group = context.CreateNew<DropItemGroup>();
        group.SetGuid(FirstWingNumber, material.Number);
        group.Description = material.Name;
        group.Chance = chance;
        group.PossibleItems.Add(material);
        config.DropItemGroups.Add(group);
        return group;
    }

    private static void EnsureMonsterDrop(IContext context, GameConfiguration config, short monsterNumber, double chance, ItemDefinition material)
    {
        if (config.Monsters.FirstOrDefault(m => m.Number == monsterNumber) is not { } monster
            || EnsureDropGroup(context, config, chance, material) is not { } group)
        {
            return;
        }

        group.Monster = monster;
        monster.DropItemGroups.Add(group);
    }

    private static void EnsureMapDrop(IContext context, GameConfiguration config, byte mapNumber, byte minimumMonsterLevel, double chance, ItemDefinition material)
    {
        if (config.Maps.FirstOrDefault(m => m.Number == mapNumber) is not { } map
            || EnsureDropGroup(context, config, chance, material) is not { } group)
        {
            return;
        }

        group.MinimumMonsterLevel = minimumMonsterLevel;
        map.DropItemGroups.Add(group);
    }

    private static void EnsureCrafting(IContext context, GameConfiguration config, ItemDefinition[] materials)
    {
        var chaosGoblin = config.Monsters.FirstOrDefault(m => m.NpcWindow == NpcWindow.ChaosMachine);
        var wings = Enumerable.Range(FirstWingNumber, materials.Length)
            .Select(number => config.Items.FirstOrDefault(i => i.Group == 12 && i.Number == number))
            .ToList();
        if (chaosGoblin is null || chaosGoblin.ItemCraftings.Any(c => c.Number == CraftingNumber) || wings.Any(w => w is null))
        {
            return;
        }

        var crafting = context.CreateNew<ItemCrafting>();
        crafting.Name = "Monster Wings";
        crafting.Number = CraftingNumber;
        crafting.ItemCraftingHandlerClassName = typeof(MonsterWingsCrafting).FullName!;
        var settings = context.CreateNew<SimpleCraftingSettings>();
        crafting.SimpleCraftingSettings = settings;
        settings.MaximumSuccessPercent = 60;
        settings.MoneyPerFinalSuccessPercentage = 100_000;

        settings.RequiredItems.Add(Required(context, 0, 9_000_000, FindItems(config, (12, 3), (12, 4), (12, 5), (12, 6), (12, 42))));
        settings.RequiredItems.Add(Required(context, 0, 500_000, FindItems(config, (12, 49), (13, 30))));
        settings.RequiredItems.Add(Required(context, 1, 0, FindItems(config, (12, 15)))); // Jewel of Chaos
        settings.RequiredItems.Add(Required(context, 1, 0, FindItems(config, (14, 22)))); // Jewel of Creation
        settings.RequiredItems.Add(Required(context, 1, 0, materials));

        settings.ResultItemSelect = ResultItemSelection.Any;
        settings.ResultItemLuckOptionChance = 20;
        settings.ResultItemExcellentOptionChance = 20;
        settings.ResultItemMaxExcOptionCount = 1;
        foreach (var wing in wings.OfType<ItemDefinition>())
        {
            var result = context.CreateNew<ItemCraftingResultItem>();
            result.ItemDefinition = wing;
            settings.ResultItems.Add(result);
        }

        chaosGoblin.ItemCraftings.Add(crafting);
    }

    private static IEnumerable<ItemDefinition> FindItems(GameConfiguration config, params (byte Group, short Number)[] ids)
    {
        return ids.Select(id => config.Items.FirstOrDefault(i => i.Group == id.Group && i.Number == id.Number)).OfType<ItemDefinition>();
    }

    private static ItemCraftingRequiredItem Required(IContext context, byte minimumAmount, int npcPriceDivisor, IEnumerable<ItemDefinition> items)
    {
        var required = context.CreateNew<ItemCraftingRequiredItem>();
        required.MinimumAmount = minimumAmount;
        required.MaximumAmount = 1;
        required.MinimumItemLevel = 0;
        required.MaximumItemLevel = 15;
        required.NpcPriceDivisor = npcPriceDivisor;
        required.FailResult = MixResult.Disappear;
        required.SuccessResult = MixResult.Disappear;
        foreach (var item in items)
        {
            required.PossibleItems.Add(item);
        }

        return required;
    }
}
