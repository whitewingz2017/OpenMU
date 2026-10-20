// <copyright file="EliteManaPotionConsumeHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Applies the configured instant recovery of the EliteManaPotion cash-shop item.
/// </summary>
[PlugIn]
[Guid("e30cca5a-bddb-4f52-8bdc-d2e06e3b6f94")]
[Display(Name = "EliteManaPotionConsumeHandlerPlugIn_Name", Description = "EliteManaPotionConsumeHandlerPlugIn_Description", ResourceType = typeof(PlugInResources))]
public class EliteManaPotionConsumeHandlerPlugIn : ElitePotionConsumeHandlerPlugIn
{
    /// <inheritdoc />
    public override ItemIdentifier Key => new(71, 14);

    /// <inheritdoc />
    protected override AttributeDefinition CurrentAttribute => Stats.CurrentMana;

    /// <inheritdoc />
    protected override AttributeDefinition MaximumAttribute => Stats.MaximumMana;

    /// <inheritdoc />
    protected override double RecoveryPercentage => 100;
}
