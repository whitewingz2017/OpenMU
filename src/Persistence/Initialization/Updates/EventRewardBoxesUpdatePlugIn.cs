// <copyright file="EventRewardBoxesUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.Persistence.Initialization.Properties;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;
using MUnique.OpenMU.PlugIns;

/// <summary>Adds editable rewards to the standalone firecracker on existing Season Six servers.</summary>
[PlugIn]
[Display(Name = nameof(EventBoxResources.Name), Description = nameof(EventBoxResources.Description), ResourceType = typeof(EventBoxResources))]
[Guid("abc4fcad-dcf6-42ac-9070-97c48a751a9d")]
public sealed class EventRewardBoxesUpdatePlugIn : UpdatePlugInBase
{
    /// <inheritdoc />
    public override string Name => EventBoxResources.Name;

    /// <inheritdoc />
    public override string Description => EventBoxResources.Description;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => false;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 21, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration config)
    {
        EventRewardBoxes.Ensure(context, config);
        return ValueTask.CompletedTask;
    }
}
