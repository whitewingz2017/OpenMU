// <copyright file="AddCombatResistanceAttributesUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Globalization;
using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Persistence.Initialization.Properties;
using MUnique.OpenMU.PlugIns;

/// <summary>Adds the configurable combat resistances for existing Season 6 games.</summary>
[PlugIn]
[Guid("BAF79F22-E5B2-4BFA-9D4F-8A6773BB562C")]
public sealed class AddCombatResistanceAttributesUpdatePlugIn : UpdatePlugInBase
{
    /// <inheritdoc />
    public override string Name => PlugInResources.ResourceManager.GetString("AddCombatResistanceAttributesUpdatePlugIn_Name", CultureInfo.CurrentUICulture)!;

    /// <inheritdoc />
    public override string Description => PlugInResources.ResourceManager.GetString("AddCombatResistanceAttributesUpdatePlugIn_Description", CultureInfo.CurrentUICulture)!;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 27, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        foreach (var attribute in new[] { Stats.ExcellentDamageResistance, Stats.IgnoreDefenseResistance })
        {
            if (gameConfiguration.Attributes.All(existing => existing.Id != attribute.Id))
            {
                this.AddStatIfNotExists(context, gameConfiguration, attribute);
            }
        }

        return ValueTask.CompletedTask;
    }
}
