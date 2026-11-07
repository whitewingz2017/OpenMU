// <copyright file="TigrisSocketItemsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;
using MUnique.OpenMU.PlugIns;

/// <summary>Adds Tigris armor and Pantera Claws to existing Season 6 configurations.</summary>
[PlugIn]
[Display(Name = nameof(TigrisResources.TigrisSocketItems_Name), Description = nameof(TigrisResources.TigrisSocketItems_Description), ResourceType = typeof(TigrisResources))]
[Guid("ca493bad-57df-48b1-87b4-6d7f12fc74c7")]
public sealed class TigrisSocketItemsUpdatePlugIn : UpdatePlugInBase
{
    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => TigrisResources.TigrisSocketItems_Name;

    /// <inheritdoc />
    public override string Description => TigrisResources.TigrisSocketItems_Description;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 7, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override bool IsMandatory => false;

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        TigrisSocketItems.Ensure(context, gameConfiguration);
        return ValueTask.CompletedTask;
    }
}
