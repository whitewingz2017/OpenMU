// <copyright file="ItemBonusActions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.ItemBonuses;

using System.Globalization;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Views;

/// <summary>Answers the custom client's request for the picked items' attribute lines.</summary>
public sealed class ItemBonusActions
{
    /// <summary>The most items an answer carries.</summary>
    public const int MaximumItems = 256;

    /// <summary>Sends the picked items' lines; none when the plugin is inactive.</summary>
    /// <param name="player">The player.</param>
    /// <param name="requestId">The request identifier which the answer carries.</param>
    /// <returns>The completion.</returns>
    public ValueTask ExecuteAsync(Player player, uint requestId)
    {
        var answer = new ItemBonusAnswer
        {
            RequestId = requestId,
            Items = ItemBonusFeaturePlugIn.GetItems(player.GameContext)
                .Select(Create)
                .Where(item => item.Lines.Count > 0)
                .Take(MaximumItems)
                .ToList(),
        };
        return player.InvokeViewPlugInAsync<IItemBonusView>(view => view.ShowAsync(answer));
    }

    private static ItemBonusItem Create(ItemDefinition definition) => new()
    {
        Type = (definition.Group * 512) + definition.Number,
        Lines = definition.BasePowerUpAttributes
            .Where(attribute => attribute.TargetAttribute is not null)
            .Select(attribute => new ItemBonusLine
            {
                Text = ItemBonusText.Write(attribute.TargetAttribute!, attribute.AggregateType, attribute.BaseValue),
                ByLevel = ItemBonusText.BonusLevels(attribute).ToDictionary(
                    level => level.ToString(CultureInfo.InvariantCulture),
                    level => ItemBonusText.Write(attribute.TargetAttribute!, attribute.AggregateType, ItemBonusText.ValueAt(attribute, level))),
            })
            .ToList(),
    };
}
