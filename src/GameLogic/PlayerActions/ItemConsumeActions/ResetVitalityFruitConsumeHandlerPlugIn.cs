// <copyright file="ResetVitalityFruitConsumeHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Resets invested Vitality using the matching cash-shop fruit.
/// </summary>
[PlugIn]
[Guid("0abff12d-eb32-4bdf-a631-9b2e44e6712e")]
[Display(Name = nameof(PlugInResources.ResetVitalityFruitConsumeHandlerPlugIn_Name), Description = nameof(PlugInResources.ResetVitalityFruitConsumeHandlerPlugIn_Description), ResourceType = typeof(PlugInResources))]
public class ResetVitalityFruitConsumeHandlerPlugIn : ResetFruitConsumeHandlerPlugIn
{
    /// <inheritdoc />
    public override ItemIdentifier Key => new(56, 13);

    /// <inheritdoc />
    protected override AttributeDefinition Attribute => Stats.BaseVitality;
}
