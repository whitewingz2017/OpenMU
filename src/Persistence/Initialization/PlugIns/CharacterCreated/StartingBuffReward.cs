// <copyright file="StartingBuffReward.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.PlugIns.CharacterCreated;

/// <summary>
/// One starting buff, e.g. a Seal of Wealth or a Scroll of Protection, which is active from the creation of the character.
/// </summary>
/// <remarks>
/// The buff is the consume effect of the item, so it works like the same buff bought in the cash shop:
/// it is kept while the character is offline and ends at a fixed time.
/// </remarks>
public sealed class StartingBuffReward
{
    /// <summary>Gets or sets the group of the item whose consume effect is the buff.</summary>
    [Range(0, 15)]
    public byte Group { get; set; }

    /// <summary>Gets or sets the number of the item whose consume effect is the buff.</summary>
    [Range(0, 511)]
    public short Number { get; set; }

    /// <summary>Gets or sets the duration, counted from the creation of the character.</summary>
    public StartingBuffDuration Duration { get; set; } = StartingBuffDuration.OneDay;

    /// <inheritdoc />
    public override string ToString() => $"{this.Group}:{this.Number} {this.Duration}";
}
