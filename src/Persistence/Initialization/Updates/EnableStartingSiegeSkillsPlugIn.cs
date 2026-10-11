// <copyright file="EnableStartingSiegeSkillsPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.Persistence.Initialization.CharacterClasses;
using MUnique.OpenMU.Persistence.Initialization.PlugIns.CharacterCreated;
using MUnique.OpenMU.Persistence.Initialization.Skills;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Enables automatic class-specific siege skill grants on existing Season 6 installations.
/// </summary>
[PlugIn]
[Guid("476DBD17-86C1-48ED-A4B2-177381F392E4")]
[Display(Name = nameof(StartingSkillResources.Update_Name), Description = nameof(StartingSkillResources.Update_Description), ResourceType = typeof(StartingSkillResources))]
public class EnableStartingSiegeSkillsPlugIn : UpdatePlugInBase
{
    /// <inheritdoc />
    public override string Name => StartingSkillResources.Update_Name;

    /// <inheritdoc />
    public override string Description => StartingSkillResources.Update_Description;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 10, 11, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var lance = gameConfiguration.Skills.First(skill => skill.Number == (short)SkillNumber.Lance);
        var wizard = gameConfiguration.CharacterClasses.First(characterClass => characterClass.Number == (byte)CharacterClassNumber.DarkWizard);
        if (!lance.QualifiedCharacters.Contains(wizard))
        {
            lance.QualifiedCharacters.Add(wizard);
        }

        Type[] grantTypes =
        [
            typeof(AddCrescentMoonSlashForDarkKnight), typeof(AddLanceForDarkWizard),
            typeof(AddStarfallForFairyElf), typeof(AddSpiralSlashForMagicGladiator),
            typeof(AddFireBlastForDarkLord), typeof(AddLanceForSummoner), typeof(AddChargeForRageFighter),
        ];
        foreach (var grantType in grantTypes)
        {
            var configuration = gameConfiguration.PlugInConfigurations.FirstOrDefault(entry => entry.TypeId == grantType.GUID);
            if (configuration is null)
            {
                configuration = context.CreateNew<PlugInConfiguration>();
                configuration.SetGuid(grantType.GUID);
                configuration.TypeId = grantType.GUID;
                gameConfiguration.PlugInConfigurations.Add(configuration);
            }

            configuration.IsActive = true;
        }

        return ValueTask.CompletedTask;
    }
}
