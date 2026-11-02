// <copyright file="WingSkinMovementSpeedUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Gives wing and cape skins the movement speed of normal wings. The custom client draws a worn wing
/// skin as wings and runs at wing speed; without the same speed on the server, the walk lags behind
/// and the client gets pulled back.
/// </summary>
[PlugIn]
[Display(Name = "Wing Skin Movement Speed", Description = "Wing and cape skins move as fast as normal wings.")]
[Guid("8d2f6c41-5a7e-4b93-a0d4-3e91c7b5f218")]
public sealed class WingSkinMovementSpeedUpdatePlugIn : UpdatePlugInBase
{
    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => "Wing Skin Movement Speed";

    /// <inheritdoc />
    public override string Description => "Wing and cape skins get the movement speed of normal wings outside the safe zone. Requires the matching TwinMU client.";

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override bool IsMandatory => false;

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        VersionSeasonSix.Items.CosmeticArmorItems.EnsureWingSkinMovementSpeed(context, gameConfiguration);
        return ValueTask.CompletedTask;
    }
}
