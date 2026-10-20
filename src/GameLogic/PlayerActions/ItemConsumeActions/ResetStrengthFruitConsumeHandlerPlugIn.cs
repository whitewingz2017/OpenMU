// <copyright file="ResetStrengthFruitConsumeHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Resets invested Strength using the matching cash-shop fruit.
/// </summary>
[PlugIn]
[Guid("b7bdac0b-b819-40ed-ba66-51b38278a0df")]
[Display(Name = nameof(PlugInResources.ResetStrengthFruitConsumeHandlerPlugIn_Name), Description = nameof(PlugInResources.ResetStrengthFruitConsumeHandlerPlugIn_Description), ResourceType = typeof(PlugInResources))]
public class ResetStrengthFruitConsumeHandlerPlugIn : ResetFruitConsumeHandlerPlugIn
{
    /// <inheritdoc />
    public override ItemIdentifier Key => new(54, 13);

    /// <inheritdoc />
    protected override AttributeDefinition Attribute => Stats.BaseStrength;
}
