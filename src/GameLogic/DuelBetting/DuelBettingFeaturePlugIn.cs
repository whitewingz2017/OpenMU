// <copyright file="DuelBettingFeaturePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.DuelBetting;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>Exposes opt-in betting configuration without enabling wallet operations.</summary>
[PlugIn]
[Display(Name = nameof(DuelBettingResources.Name), Description = nameof(DuelBettingResources.Description), ResourceType = typeof(DuelBettingResources))]
[Guid("c817045f-8921-4654-8cbe-fcb14459fa0d")]
public sealed class DuelBettingFeaturePlugIn : IFeaturePlugIn, IDisabledByDefault, ISupportCustomConfiguration<DuelBettingSettings>, ISupportDefaultCustomConfiguration
{
    /// <inheritdoc />
    public DuelBettingSettings? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new DuelBettingSettings();
}
