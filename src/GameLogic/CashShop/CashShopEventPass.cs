// <copyright file="CashShopEventPass.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.CashShop;

/// <summary>
/// Configures an alternate cash-shop ticket without bypassing event admission checks.
/// </summary>
public class CashShopEventPass
{
    /// <summary>
    /// Gets or sets the event which accepts the pass.
    /// </summary>
    public MiniGameType Event { get; set; }

    /// <summary>
    /// Gets or sets the item group.
    /// </summary>
    public byte ItemGroup { get; set; } = 13;

    /// <summary>
    /// Gets or sets the item number.
    /// </summary>
    public short ItemNumber { get; set; }

    /// <summary>
    /// Gets or sets the event levels accepted by this ticket; empty means all levels.
    /// </summary>
    public IList<int> Levels { get; set; } = new List<int>();

    /// <summary>
    /// Gets or sets a value indicating whether a timed pass can be reused until its deadline.
    /// </summary>
    public bool ReusableUntilExpiration { get; set; } = true;
}
