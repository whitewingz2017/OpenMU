// <copyright file="CashShopEffectItems.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Attributes;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Persistence.Initialization.CharacterClasses;

/// <summary>
/// Configures Season 6 purchased bonuses using the normal attribute and magic-effect system.
/// </summary>
internal class CashShopEffectItems(IContext context, GameConfiguration gameConfiguration) : InitializerBase(context, gameConfiguration)
{
    /// <inheritdoc />
    public override void Initialize()
    {
        // Existing databases predate this stat; power-up creation resolves persisted attributes.
        // Register it before configuring Wealth or Divinity, rather than only their base values.
        if (this.GameConfiguration.Attributes.All(attribute => attribute.Id != Stats.ItemDropRate.Id))
        {
            this.GameConfiguration.Attributes.Add(this.Context.CreateNew<AttributeDefinition>(
                Stats.ItemDropRate.Id, Stats.ItemDropRate.Designation, Stats.ItemDropRate.Description));
        }

        this.Configure(13, 43, 29, 60, (Stats.ExperienceRate, 0.5f, AggregateType.AddRaw));
        this.Configure(13, 44, 30, 60, (Stats.ExperienceRate, 1f, AggregateType.AddRaw), (Stats.ItemDropRate, 1.5f, AggregateType.Multiplicate));
        this.Configure(13, 45, 31, 60, (Stats.ExperienceRate, 0f, AggregateType.Multiplicate), (Stats.MasterExperienceRate, 0f, AggregateType.Multiplicate));
        this.Configure(13, 62, 87, 13, (Stats.ExperienceRate, 0.5f, AggregateType.AddRaw), (Stats.HealthRecoveryMultiplier, 0.05f, AggregateType.AddRaw));
        this.Configure(13, 63, 88, 13, (Stats.ItemDropRate, 1.5f, AggregateType.Multiplicate), (Stats.ManaRecoveryMultiplier, 0.05f, AggregateType.AddRaw));
        this.Configure(13, 93, 101, 60, (Stats.MasterExperienceRate, 0.5f, AggregateType.AddRaw));
        this.Configure(13, 94, 102, 60, (Stats.MasterExperienceRate, 1f, AggregateType.AddRaw), (Stats.ItemDropRate, 1.5f, AggregateType.Multiplicate));
        this.Configure(13, 104, 113, 58, (Stats.MaximumAbility, 1.1f, AggregateType.Multiplicate));
        this.Configure(13, 105, 114, 58, (Stats.MaximumShield, 1.1f, AggregateType.Multiplicate));
        this.Configure(14, 72, 44, 67, (Stats.AttackSpeedAny, 15f, AggregateType.AddRaw));
        this.Configure(14, 73, 45, 68, (Stats.DefenseFinal, 60f, AggregateType.AddRaw));
        this.Configure(14, 74, 46, 69, (Stats.PhysicalBaseDmg, 30f, AggregateType.AddRaw));
        this.Configure(14, 75, 47, 70, (Stats.WizardryBaseDmg, 30f, AggregateType.AddRaw), (Stats.CurseBaseDmg, 30f, AggregateType.AddRaw));
        this.Configure(14, 76, 48, 71, (Stats.MaximumHealth, 200f, AggregateType.AddRaw));
        this.Configure(14, 77, 49, 72, (Stats.MaximumMana, 300f, AggregateType.AddRaw));
        this.Configure(14, 97, 89, 73, (Stats.CriticalDamageBonus, 20f, AggregateType.AddRaw));
        this.Configure(14, 98, 90, 74, (Stats.ExcellentDamageBonus, 20f, AggregateType.AddRaw));

        this.Configure(13, 128, 122, 76, (Stats.CriticalDamageChance, .1f, AggregateType.AddRaw));
        this.Configure(13, 129, 123, 76, (Stats.ExcellentDamageChance, .1f, AggregateType.AddRaw));
        this.Configure(13, 130, 124, 77, (Stats.MaximumHealth, 50f, AggregateType.AddRaw));
        this.Configure(13, 132, 126, 77, (Stats.MaximumHealth, 100f, AggregateType.AddRaw), (Stats.MaximumShield, 500f, AggregateType.AddRaw));
        this.Configure(13, 134, 128, 78, (Stats.MoneyAmountRate, 1.2f, AggregateType.Multiplicate));

        foreach (short number in new short[] { 93, 94 })
        {
            var masterSeal = this.GameConfiguration.Items.Single(item => item.Group == 13 && item.Number == number);
            if (masterSeal.QualifiedCharacters.Count == 0)
            {
                foreach (var masterClass in this.GameConfiguration.CharacterClasses.Where(characterClass => characterClass.IsMasterClass))
                {
                    masterSeal.QualifiedCharacters.Add(masterClass);
                }
            }
        }

        foreach (var characterClass in this.GameConfiguration.CharacterClasses)
        {
            if (characterClass.BaseAttributeValues.All(value => value.Definition != Stats.ItemDropRate))
            {
                characterClass.BaseAttributeValues.Add(CharacterClassHelper.CreateConstValueAttribute(this.Context, this.GameConfiguration, 1, Stats.ItemDropRate));
            }
        }
    }

    private void Configure(byte group, short number, short effectNumber, byte conflictGroup, params (AttributeDefinition Attribute, float Value, AggregateType Type)[] bonuses)
    {
        var item = this.GameConfiguration.Items.Single(item => item.Group == group && item.Number == number);
        if (item.ConsumeEffect is not null)
        {
            return;
        }

        var effect = this.GameConfiguration.MagicEffects.FirstOrDefault(effect => effect.Number == effectNumber);
        if (effect is null)
        {
            effect = this.Context.CreateNew<MagicEffectDefinition>();
            effect.Number = effectNumber;
            effect.SubType = conflictGroup;
            effect.Name = item.Name;
            effect.SendDuration = true;
            effect.StopByDeath = false;
            effect.Duration = this.Context.CreateNew<PowerUpDefinitionValue>();
            effect.Duration.ConstantValue.Value = 1800;
            this.GameConfiguration.MagicEffects.Add(effect);
        }

        if (effect.PowerUpDefinitions.Count == 0)
        {
            foreach (var (attribute, value, aggregate) in bonuses)
            {
                var bonus = this.CreatePowerUpDefinition(attribute, value == 0 ? 1 : value, aggregate);
                bonus.Boost!.ConstantValue.Value = value;
                if (group == 13 && number is 104 or 105)
                {
                    bonus.Boost.ConstantValue.Value = 0;
                    bonus.Boost.ConstantValue.AggregateType = AggregateType.AddRaw;
                    var scaling = this.Context.CreateNew<AttributeRelationship>();
                    scaling.InputAttribute = Stats.Level.GetPersistent(this.GameConfiguration);
                    scaling.InputOperand = number == 104 ? 1 : 10;
                    scaling.InputOperator = InputOperator.Multiply;
                    scaling.AggregateType = AggregateType.AddRaw;
                    scaling.TargetAttribute = attribute.GetPersistent(this.GameConfiguration);
                    bonus.Boost.RelatedValues.Add(scaling);
                }

                effect.PowerUpDefinitions.Add(bonus);
            }
        }

        item.ConsumeEffect = effect;
    }
}
