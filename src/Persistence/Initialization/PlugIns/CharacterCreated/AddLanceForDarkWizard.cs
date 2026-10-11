// <copyright file="AddLanceForDarkWizard.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.PlugIns.CharacterCreated;

using System.Runtime.InteropServices;
using MUnique.OpenMU.Persistence.Initialization.CharacterClasses;
using MUnique.OpenMU.Persistence.Initialization.Skills;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Grants Lance to newly created dark wizards.
/// </summary>
[Guid("B6F45457-74F2-42DF-ACAC-BF6F786B316B")]
[PlugIn]
[Display(Name = nameof(StartingSkillResources.AddLance_Name), Description = nameof(StartingSkillResources.AddLance_Description), ResourceType = typeof(StartingSkillResources))]
public class AddLanceForDarkWizard : AddInitialSkillPlugInBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddLanceForDarkWizard"/> class.
    /// </summary>
    public AddLanceForDarkWizard()
        : base((byte)CharacterClassNumber.DarkWizard, (ushort)SkillNumber.Lance)
    {
    }
}
