// <copyright file="SetSupportBuffDurationUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.Persistence.Initialization.Properties;
using MUnique.OpenMU.Persistence.Initialization.Skills;
using MUnique.OpenMU.PlugIns;

/// <summary>Applies the requested one-minute duration to Season 6 support buffs.</summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.SetSupportBuffDurationUpdatePlugIn_Name), Description = nameof(PlugInResources.SetSupportBuffDurationUpdatePlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("8c47acb2-09cf-443c-9202-05e4b5a98578")]
public class SetSupportBuffDurationUpdatePlugIn : UpdatePlugInBase
{
    /// <inheritdoc />
    public override string Name => PlugInResources.SetSupportBuffDurationUpdatePlugIn_Name;

    /// <inheritdoc />
    public override string Description => PlugInResources.SetSupportBuffDurationUpdatePlugIn_Description;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => false;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 9, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        SupportBuffDurations.Apply(context, gameConfiguration);
        return ValueTask.CompletedTask;
    }
}
