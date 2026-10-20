// <copyright file="EliteShieldPotionConsumeHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Applies the configured instant recovery of the EliteShieldPotion cash-shop item.
/// </summary>
[PlugIn]
[Guid("3a53272b-dc93-470b-9131-6a4b86a1be9f")]
[Display(Name = nameof(PlugInResources.EliteShieldPotionConsumeHandlerPlugIn_Name), Description = nameof(PlugInResources.EliteShieldPotionConsumeHandlerPlugIn_Description), ResourceType = typeof(PlugInResources))]
public class EliteShieldPotionConsumeHandlerPlugIn : ElitePotionConsumeHandlerPlugIn
{
    /// <inheritdoc />
    public override ItemIdentifier Key => new(133, 14);

    /// <inheritdoc />
    protected override AttributeDefinition CurrentAttribute => Stats.CurrentShield;

    /// <inheritdoc />
    protected override AttributeDefinition MaximumAttribute => Stats.MaximumShield;

    /// <inheritdoc />
    protected override double RecoveryPercentage => 65;
}
