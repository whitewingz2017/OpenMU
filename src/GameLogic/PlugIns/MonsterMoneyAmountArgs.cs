// <copyright file="MonsterMoneyAmountArgs.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

/// <summary>
/// Holds the monster and its money amount while money calculation plugins run.
/// </summary>
public sealed class MonsterMoneyAmountArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MonsterMoneyAmountArgs"/> class.
    /// </summary>
    /// <param name="monster">The killed monster definition.</param>
    /// <param name="amount">The original money amount.</param>
    public MonsterMoneyAmountArgs(MonsterDefinition monster, uint amount)
    {
        this.Monster = monster;
        this.Amount = amount;
    }

    /// <summary>
    /// Gets the killed monster definition.
    /// </summary>
    public MonsterDefinition Monster { get; }

    /// <summary>
    /// Gets or sets the total money before party sharing and player bonuses.
    /// </summary>
    public uint Amount { get; set; }
}
