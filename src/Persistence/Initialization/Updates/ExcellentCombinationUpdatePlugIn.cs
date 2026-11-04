// <copyright file="ExcellentCombinationUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.PlayerActions.Items;
using MUnique.OpenMU.PlugIns;

/// <summary>Optional update for existing Season Six installations.</summary>
[PlugIn]
[Display(Name = nameof(ExcellentCombinationResources.Name), Description = nameof(ExcellentCombinationResources.Description), ResourceType = typeof(ExcellentCombinationResources))]
[Guid("a01d5e78-a204-4d67-93dc-1e4f731380a2")]
public sealed class ExcellentCombinationUpdatePlugIn : UpdatePlugInBase
{
    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => ExcellentCombinationResources.Name;

    /// <inheritdoc />
    public override string Description => ExcellentCombinationResources.Description;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 5, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override bool IsMandatory => false;

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration config)
    {
        VersionSeasonSix.Items.ResonanceCore.Ensure(context, config);
        if (!config.PlugInConfigurations.Any(p => p.TypeId == typeof(ExcellentCombinationFeaturePlugIn).GUID))
        {
            var feature = context.CreateNew<PlugInConfiguration>();
            feature.TypeId = typeof(ExcellentCombinationFeaturePlugIn).GUID;
            feature.SetConfiguration(new ExcellentCombinationConfiguration(), null);
            feature.IsActive = true;
            config.PlugInConfigurations.Add(feature);
        }

        return ValueTask.CompletedTask;
    }
}
