namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Attributes;

/// <summary>Optional Muun definitions, isolated from normal equipment and skin slots.</summary>
public static class MuunItems
{
    /// <summary>Adds missing Muuns without changing administrators' existing bonuses.</summary>
    public static void Ensure(IContext context, GameConfiguration config)
    {
        string[] names = ["Magic Muun", "Evolved Magic Muun", "Fighter Muun", "Evolved Fighter Muun", "Dark Muun", "Evolved Dark Muun"];
        for (short i = 0; i < names.Length; i++)
        {
            var number = (short)(450 + i);
            var existing = config.Items.SingleOrDefault(d => d.Group == 13 && d.Number == number);
            if (existing is not null)
            {
                if (existing.Name.ValueInNeutralLanguage != names[i]) throw new InvalidOperationException($"Muun item 13:{number} is already in use.");
                continue;
            }
            var slot = config.ItemSlotTypes.SingleOrDefault(s => s.ItemSlots.Count == 1 && s.ItemSlots.Single() == 249);
            if (slot is null)
            {
                slot = context.CreateNew<ItemSlotType>(); slot.SetGuid(249); slot.Description = "Muun companion"; slot.ItemSlots.Add(249); config.ItemSlotTypes.Add(slot);
            }
            var item = context.CreateNew<ItemDefinition>(); item.SetGuid(13, number);
            item.Group = 13; item.Number = number; item.Name = names[i]; item.ItemSlot = slot;
            item.Width = 2; item.Height = 2; item.Durability = 100;
            item.IsTradable = true; item.IsStorable = true; item.IsPersonalStoreSellable = true;
            item.IsSellableToNpc = false; item.IsDroppable = true; item.IsRepairable = false;
            var bonus = context.CreateNew<ItemBasePowerUpDefinition>();
            bonus.TargetAttribute = config.Attributes.Single(a => a.Id == (i / 2 == 0 ? Stats.MaximumHealth.Id : i / 2 == 1 ? Stats.AttackSpeedAny.Id : Stats.BonusExperienceRate.Id));
            bonus.BaseValue = i / 2 == 0 ? 50 : i / 2 == 1 ? 1 : 0.01f;
            if (i % 2 == 1) bonus.BaseValue *= 2;
            bonus.AggregateType = AggregateType.AddRaw;
            var table = context.CreateNew<ItemLevelBonusTable>(); table.Name = names[i];
            for (var level = 0; level <= 4; level++) { var entry = context.CreateNew<LevelBonus>(); entry.Level = level; entry.AdditionalValue = bonus.BaseValue * level; table.BonusPerLevel.Add(entry); }
            bonus.BonusPerLevelTable = table; item.BasePowerUpAttributes.Add(bonus); config.Items.Add(item);
        }
        var stone = config.Items.SingleOrDefault(d => d.Group == 14 && d.Number == 250);
        if (stone is not null && stone.Name.ValueInNeutralLanguage != "Muun Evolution Stone") throw new InvalidOperationException("Muun stone item 14:250 is already in use.");
        if (stone is null)
        {
            stone = context.CreateNew<ItemDefinition>(); stone.SetGuid(14, 250); stone.Group = 14; stone.Number = 250; stone.Name = "Muun Evolution Stone";
            stone.Width = 1; stone.Height = 1; stone.Durability = 1; stone.IsTradable = true; stone.IsStorable = true; stone.IsDroppable = true; stone.IsPersonalStoreSellable = true;
            config.Items.Add(stone);
        }
    }
}
