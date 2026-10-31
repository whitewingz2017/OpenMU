// <copyright file="MuunConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Muun;

/// <summary>Separate pet equipment and progression settings.</summary>
public sealed class MuunConfiguration
{
    /// <summary>Gets or sets the number of storage cells including two equipped positions.</summary>
    [Range(3, 120)] public int Capacity { get; set; } = 102;
    /// <summary>Gets or sets Muun life lost when the owner dies; zero disables the loss.</summary>
    [Range(0, 100)] public int LifeLossOnDeath { get; set; } = 1;
    /// <summary>Gets or sets the configured species. Bonuses are defined on the normal item definitions.</summary>
    public List<MuunDefinition> Pets { get; set; } = Enumerable.Range(0, 6).Select(i => new MuunDefinition
    {
        ItemType = (13 * 512) + 450 + i,
        EvolutionItemType = i % 2 == 0 ? (13 * 512) + 451 + i : -1,
    }).ToList();
}
