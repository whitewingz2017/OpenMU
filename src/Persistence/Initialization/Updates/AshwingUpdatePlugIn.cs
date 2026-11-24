// <copyright file="AshwingUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Maps;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Makes the custom phoenix available on existing Season Six installations.
/// </summary>
[PlugIn]
[Display(Name = "Add Ashwing", Description = "Adds the Ashwing boss to Icarus. Requires the Ashwing client assets.")]
[Guid("094EF11F-F216-4BCF-87B3-07B6892F35FD")]
public class AshwingUpdatePlugIn : UpdatePlugInBase
{
    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => "Add Ashwing";

    /// <inheritdoc />
    public override string Description => "Adds Ashwing (669) at Icarus (47, 189). Requires the Ashwing client assets.";

    /// <inheritdoc />
    public override bool IsMandatory => false;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var map = gameConfiguration.Maps.Single(m => m.Number == Icarus.Number && m.Discriminator == 0);
        Ashwing.Ensure(context, gameConfiguration, map);
        return ValueTask.CompletedTask;
    }
}
