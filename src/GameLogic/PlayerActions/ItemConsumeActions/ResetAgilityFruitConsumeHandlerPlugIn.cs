// <copyright file="ResetAgilityFruitConsumeHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Resets invested Agility using the matching cash-shop fruit.
/// </summary>
[PlugIn]
[Guid("a5ca2c81-0e01-4cd4-85b2-20a8e4dbbdb9")]
[Display(Name = nameof(PlugInResources.ResetAgilityFruitConsumeHandlerPlugIn_Name), Description = nameof(PlugInResources.ResetAgilityFruitConsumeHandlerPlugIn_Description), ResourceType = typeof(PlugInResources))]
public class ResetAgilityFruitConsumeHandlerPlugIn : ResetFruitConsumeHandlerPlugIn
{
    /// <inheritdoc />
    public override ItemIdentifier Key => new(55, 13);

    /// <inheritdoc />
    protected override AttributeDefinition Attribute => Stats.BaseAgility;
}
