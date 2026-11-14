// <copyright file="ResonanceCoreDropConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Items;

/// <summary>
/// The configuration of the <see cref="ResonanceCoreDropPlugIn"/>.
/// </summary>
public sealed class ResonanceCoreDropConfiguration
{
    /// <summary>
    /// Gets or sets the chance in percent that a monster which is not a boss drops a Resonance Core.
    /// </summary>
    [Range(0.0, 100.0)]
    [Display(Name = nameof(ResonanceCoreDropResources.MonsterDropChance), ResourceType = typeof(ResonanceCoreDropResources))]
    public double MonsterDropChance { get; set; } = 0.0001;

    /// <summary>
    /// Gets or sets the chance in percent that a kill of a boss drops a Resonance Core,
    /// as long as this boss didn't drop one yet on the same day.
    /// </summary>
    [Range(0.0, 100.0)]
    [Display(Name = nameof(ResonanceCoreDropResources.BossDropChance), ResourceType = typeof(ResonanceCoreDropResources))]
    public double BossDropChance { get; set; } = 10;

    /// <summary>
    /// Gets or sets the numbers of the bosses. Each of them drops at most one Resonance Core per day.
    /// </summary>
    [Display(Name = nameof(ResonanceCoreDropResources.BossMonsterNumbers), ResourceType = typeof(ResonanceCoreDropResources))]
    public IList<short> BossMonsterNumbers { get; set; } = new List<short>
    {
        38, 49, 77, // Balrog, Hydra, Phoenix.
        43, 44, 53, 78, 79, 80, 82, 135, // Golden invasion bosses, Red Dragon, White Wizard.
        275, 295, 340, 349, // Kundun, Erohim, Dark Elf, Balgass.
        361, 362, 363, 364, 459, 561, // Kanturu bosses, Selupan, Medusa.
        504, 505, 506, 507, 508, 509, 510, 511, // Imperial Guardian bosses, including Gaion.
        529, 530, // Doppelganger butchers.
    };
}
