// <copyright file="PvpBalanceSettings.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Pvp;

using MUnique.OpenMU.DataModel.Composition;

/// <summary>The PvP attack rating and damage settings by class family.</summary>
public sealed class PvpBalanceSettings
{
    private const byte RageFighterClassNumber = 24;

    /// <summary>Gets or sets the rates by base class. Unlisted families use 100 percent.</summary>
    /// <remarks>The non-reset preset raises Rage Fighter accuracy against opponents with PvP defense mastery.</remarks>
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    public ICollection<PvpClassRate> ClassRates { get; set; } =
    [
        new() { BaseClassNumber = RageFighterClassNumber, AttackRatePercent = 120 },
    ];
}
