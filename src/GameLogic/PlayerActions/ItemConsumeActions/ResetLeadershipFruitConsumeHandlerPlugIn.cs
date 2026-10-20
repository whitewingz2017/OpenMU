// <copyright file="ResetLeadershipFruitConsumeHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Resets invested Leadership using the matching cash-shop fruit.
/// </summary>
[PlugIn]
[Guid("c515c74b-5419-40db-8a90-995cd5f3dba3")]
[Display(Name = nameof(PlugInResources.ResetLeadershipFruitConsumeHandlerPlugIn_Name), Description = nameof(PlugInResources.ResetLeadershipFruitConsumeHandlerPlugIn_Description), ResourceType = typeof(PlugInResources))]
public class ResetLeadershipFruitConsumeHandlerPlugIn : ResetFruitConsumeHandlerPlugIn
{
    /// <inheritdoc />
    public override ItemIdentifier Key => new(58, 13);

    /// <inheritdoc />
    protected override AttributeDefinition Attribute => Stats.BaseLeadership;
}
