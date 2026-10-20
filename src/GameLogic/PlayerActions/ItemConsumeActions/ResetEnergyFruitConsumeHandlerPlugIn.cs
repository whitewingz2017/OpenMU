// <copyright file="ResetEnergyFruitConsumeHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Resets invested Energy using the matching cash-shop fruit.
/// </summary>
[PlugIn]
[Guid("ddc20399-db34-40e2-8e8a-ebaf580e9098")]
[Display(Name = "ResetEnergyFruitConsumeHandlerPlugIn_Name", Description = "ResetEnergyFruitConsumeHandlerPlugIn_Description", ResourceType = typeof(PlugInResources))]
public class ResetEnergyFruitConsumeHandlerPlugIn : ResetFruitConsumeHandlerPlugIn
{
    /// <inheritdoc />
    public override ItemIdentifier Key => new(57, 13);

    /// <inheritdoc />
    protected override AttributeDefinition Attribute => Stats.BaseEnergy;
}
