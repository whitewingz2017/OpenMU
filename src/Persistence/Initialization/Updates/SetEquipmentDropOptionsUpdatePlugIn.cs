// <copyright file="SetEquipmentDropOptionsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.Persistence.Initialization.Properties;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;
using MUnique.OpenMU.PlugIns;

/// <summary>Installs the 30% luck chance and two-socket drop limit on existing Season 6 servers.</summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.SetEquipmentDropOptionsUpdatePlugIn_Name), Description = nameof(PlugInResources.SetEquipmentDropOptionsUpdatePlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("d1f3b264-50a6-4e08-9a87-7c8261d59bda")]
public sealed class SetEquipmentDropOptionsUpdatePlugIn : UpdatePlugInBase
{
    /// <inheritdoc />
    public override string Name => PlugInResources.SetEquipmentDropOptionsUpdatePlugIn_Name;

    /// <inheritdoc />
    public override string Description => PlugInResources.SetEquipmentDropOptionsUpdatePlugIn_Description;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => false;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 9, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        EquipmentDropOptions.Apply(context, gameConfiguration);
        return ValueTask.CompletedTask;
    }
}
