// <copyright file="ItemBonusStatistic.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.ItemBonuses;

/// <summary>A line of what the worn items add.</summary>
/// <param name="Statistic">The label of the statistic it is shown beside; empty for a line of its own.</param>
/// <param name="Text">The text, e.g. "+250 Final Defense".</param>
public readonly record struct ItemBonusStatistic(string Statistic, string Text);
