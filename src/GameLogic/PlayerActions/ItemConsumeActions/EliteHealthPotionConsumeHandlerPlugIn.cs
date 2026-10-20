// <copyright file="EliteHealthPotionConsumeHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Applies the configured instant recovery of the EliteHealthPotion cash-shop item.
/// </summary>
[PlugIn]
[Guid("10abc56c-c04a-464e-af48-befb07f7ffbe")]
[Display(Name = "EliteHealthPotionConsumeHandlerPlugIn_Name", Description = "EliteHealthPotionConsumeHandlerPlugIn_Description", ResourceType = typeof(PlugInResources))]
public class EliteHealthPotionConsumeHandlerPlugIn : ElitePotionConsumeHandlerPlugIn
{
    /// <inheritdoc />
    public override ItemIdentifier Key => new(70, 14);

    /// <inheritdoc />
    protected override AttributeDefinition CurrentAttribute => Stats.CurrentHealth;

    /// <inheritdoc />
    protected override AttributeDefinition MaximumAttribute => Stats.MaximumHealth;

    /// <inheritdoc />
    protected override double RecoveryPercentage => 100;
}
