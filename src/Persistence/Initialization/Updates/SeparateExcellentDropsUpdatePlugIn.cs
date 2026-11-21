// <copyright file="SeparateExcellentDropsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.Persistence.Initialization.Properties;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;
using MUnique.OpenMU.PlugIns;

/// <summary>Separates excellent accessory chances from equipment on existing Season Six servers.</summary>
[PlugIn]
[Display(Name = nameof(ExcellentDropResources.Name), Description = nameof(ExcellentDropResources.Description), ResourceType = typeof(ExcellentDropResources))]
[Guid("3a26748d-f1a6-4a6b-ad25-e8d2167769aa")]
public sealed class SeparateExcellentDropsUpdatePlugIn : UpdatePlugInBase
{
    /// <inheritdoc />
    public override string Name => ExcellentDropResources.Name;

    /// <inheritdoc />
    public override string Description => ExcellentDropResources.Description;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => false;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 21, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration config)
    {
        ExcellentDropGroups.Ensure(context, config);
        return ValueTask.CompletedTask;
    }
}
