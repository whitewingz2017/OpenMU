// <copyright file="RankTitlesUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.RankTitles;
using MUnique.OpenMU.PlugIns;

/// <summary>Enables the TwinMU title catalog on existing Season 6 configurations.</summary>
[PlugIn]
[Display(Name = nameof(RankTitleResources.UpdateName), Description = nameof(RankTitleResources.UpdateDescription), ResourceType = typeof(RankTitleResources))]
[Guid("a31448c3-0750-487f-b85d-3e682330d660")]
public sealed class RankTitlesUpdatePlugIn : UpdatePlugInBase
{
    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => RankTitleResources.UpdateName;

    /// <inheritdoc />
    public override string Description => RankTitleResources.UpdateDescription;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 7, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override int Version => 2;

    /// <inheritdoc />
    public override DateTime UpdatedAt => new(2026, 11, 7, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override bool IsMandatory => false;

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        VersionSeasonSix.RankTitles.Ensure(context, gameConfiguration);
        return ValueTask.CompletedTask;
    }
}
