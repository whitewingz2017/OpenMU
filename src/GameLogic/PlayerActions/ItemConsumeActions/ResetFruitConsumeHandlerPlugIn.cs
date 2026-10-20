// <copyright file="ResetFruitConsumeHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Views.Character;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Returns invested base-stat points without going below the character class starting value.
/// </summary>
public abstract class ResetFruitConsumeHandlerPlugIn : BaseConsumeHandlerPlugIn, ISupportCustomConfiguration<ResetFruitConfiguration>, ISupportDefaultCustomConfiguration
{
    /// <inheritdoc />
    public ResetFruitConfiguration? Configuration { get; set; }

    /// <summary>
    /// Gets the base attribute reset by this fruit.
    /// </summary>
    protected abstract AttributeDefinition Attribute { get; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new ResetFruitConfiguration();

    /// <inheritdoc />
    public override async ValueTask<bool> ConsumeItemAsync(Player player, Item item, Item? targetItem, FruitUsage fruitUsage)
    {
        if (!this.CheckPreconditions(player, item) || player.SelectedCharacter is not { } character
            || player.Attributes is not { } attributes
            || character.CharacterClass?.StatAttributes.FirstOrDefault(stat => stat.Attribute == this.Attribute && stat.IncreasableByPlayer) is not { } stat)
        {
            return false;
        }

        var settings = this.Configuration ?? new ResetFruitConfiguration();
        int points = (int)Math.Min(Math.Max(0, attributes[this.Attribute] - stat.BaseValue), item.Durability * Math.Max(0, settings.PointsPerUnit));
        if (points <= 0 || character.LevelUpPoints > int.MaxValue - points)
        {
            return false;
        }

        attributes[this.Attribute] -= points;
        character.LevelUpPoints += points;
        item.Durability = 0;
        await player.InvokeViewPlugInAsync<IUpdateCharacterBaseStatsPlugIn>(view => view.UpdateCharacterBaseStatsAsync()).ConfigureAwait(false);
        await player.InvokeViewPlugInAsync<IUpdateCharacterStatsPlugIn>(view => view.UpdateCharacterStatsAsync()).ConfigureAwait(false);
        return true;
    }
}
