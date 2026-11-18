// <copyright file="ItemBonusStatistic.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.ItemBonuses;

/// <summary>A line of what the worn items add.</summary>
/// <param name="Statistic">The label of the statistic it is shown beside; empty for a line of its own.</param>
/// <param name="Text">The text: the amount beside a statistic, e.g. "+250"; on a line of its own, e.g. "Movement Speed 19".</param>
public readonly record struct ItemBonusStatistic(string Statistic, string Text);
