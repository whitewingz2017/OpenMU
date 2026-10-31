// <copyright file="BossSocketDropsConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

/// <summary>Defines the monsters permitted to drop socket equipment.</summary>
public class BossSocketDropsConfiguration
{
    /// <summary>Gets or sets the boss monster numbers. Unknown/custom bosses must be added here.</summary>
    public IList<short> BossMonsterNumbers { get; set; } = new List<short>
    {
        38, 49, 77, // Balrog, Hydra, Phoenix.
        43, 44, 53, 78, 79, 80, 82, 135, // Golden invasion bosses, Red Dragon, White Wizard.
        275, 295, 340, 349, // Kundun, Erohim, Dark Elf, Balgass.
        361, 362, 363, 364, 459, 561, // Kanturu bosses, Selupan, Medusa.
        504, 505, 506, 507, 508, 509, 510, 511, // Imperial Guardian bosses, including Gaion.
        529, 530, // Doppelganger butchers.
    };

    /// <summary>Gets or sets the Devil Square map numbers; only their boss wave is allowed.</summary>
    public IList<short> DevilSquareMapNumbers { get; set; } = new List<short> { 9, 32 };

    /// <summary>Gets or sets the boss wave number used by the configured Devil Square event.</summary>
    public int DevilSquareBossWave { get; set; } = 10;
}
