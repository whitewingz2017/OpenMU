// <copyright file="PvpClassRate.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Pvp;

/// <summary>The PvP attack rate and damage percentages for a class family.</summary>
public sealed class PvpClassRate
{
    private const int UnmodifiedPercent = 100;

    /// <summary>Gets or sets the number of the family's base character class.</summary>
    public byte BaseClassNumber { get; set; }

    /// <summary>Gets or sets the percentage of PvP attack rating used when rolling to hit another player.</summary>
    [Range(1, int.MaxValue)]
    public int AttackRatePercent { get; set; } = UnmodifiedPercent;

    /// <summary>Gets or sets the percentage of direct damage dealt against players.</summary>
    [Range(1, int.MaxValue)]
    public int DamagePercent { get; set; } = UnmodifiedPercent;

    /// <summary>Scales the attack rating used to roll a PvP hit.</summary>
    /// <param name="attackRate">The PvP attack rate before the class adjustment.</param>
    /// <returns>The adjusted PvP attack rate.</returns>
    internal float ScaleAttackRate(float attackRate)
        => this.AttackRatePercent > 0 ? attackRate * (this.AttackRatePercent / (float)UnmodifiedPercent) : attackRate;

    /// <summary>Scales direct PvP damage before the shield split.</summary>
    /// <param name="damage">The unadjusted direct damage.</param>
    /// <returns>The adjusted damage, with a minimum of one for a damaging hit.</returns>
    internal int ScaleDamage(int damage)
        => damage > 0 && this.DamagePercent > 0
            ? (int)Math.Clamp((long)damage * this.DamagePercent / UnmodifiedPercent, 1, int.MaxValue)
            : damage;
}
