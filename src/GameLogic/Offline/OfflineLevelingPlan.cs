// <copyright file="OfflineLevelingPlan.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Offline;

/// <summary>How a player pays an offline leveling session: the UTF-8 JSON of the window's start request.</summary>
public sealed class OfflineLevelingPlan
{
    /// <summary>Gets or sets the currency.</summary>
    public OfflineLevelingCurrency Currency { get; set; }

    /// <summary>Gets or sets the prepaid hours; 0 pays every hour until the session stops.</summary>
    public int Hours { get; set; }

    /// <summary>Gets or sets a value indicating whether players can't damage the character.</summary>
    public bool Protection { get; set; }
}
