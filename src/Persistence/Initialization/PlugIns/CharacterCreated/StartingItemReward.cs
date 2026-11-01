// <copyright file="StartingItemReward.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.PlugIns.CharacterCreated;

using MUnique.OpenMU.GameLogic.PlugIns;

/// <summary>One starting item, placed automatically into the bag.</summary>
public sealed class StartingItemReward
{
    /// <summary>Gets or sets the item group.</summary>
    [Range(0, 15)]
    public byte Group { get; set; }

    /// <summary>Gets or sets the item number within its group.</summary>
    [Range(0, 511)]
    public short Number { get; set; }

    /// <summary>Gets or sets the item level.</summary>
    [Range(0, 15)]
    public byte Level { get; set; }

    /// <summary>Gets or sets the number of individual items.</summary>
    [Range(1, 64)]
    public int Quantity { get; set; } = 1;

    /// <summary>Gets or sets a value indicating whether the item has Luck.</summary>
    [Display(Name = nameof(AdministrationResources.StartingLuck), ResourceType = typeof(AdministrationResources))]
    public bool Luck { get; set; }

    /// <summary>Gets or sets a value indicating whether the weapon has its skill.</summary>
    [Display(Name = nameof(AdministrationResources.StartingSkill), ResourceType = typeof(AdministrationResources))]
    public bool Skill { get; set; }

    /// <summary>Gets or sets the additional option level. Four means +16 damage or defense.</summary>
    [Range(0, 7)]
    [Display(Name = nameof(AdministrationResources.StartingOptionLevel), Description = nameof(AdministrationResources.StartingOptionDescription), ResourceType = typeof(AdministrationResources))]
    public byte AdditionalOptionLevel { get; set; }

    /// <summary>Gets or sets the excellent option numbers supported by the item.</summary>
    [Display(Name = nameof(AdministrationResources.StartingExcellent), Description = nameof(AdministrationResources.StartingExcellentDescription), ResourceType = typeof(AdministrationResources))]
    public ICollection<int> ExcellentOptions { get; set; } = [];

    /// <inheritdoc />
    public override string ToString() => $"{this.Group}:{this.Number} +{this.Level} x{this.Quantity}";
}
