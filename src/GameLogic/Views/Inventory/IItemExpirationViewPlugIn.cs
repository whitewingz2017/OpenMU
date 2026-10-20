// <copyright file="IItemExpirationViewPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views.Inventory;

using System.Runtime.InteropServices;

/// <summary>
/// Shows absolute expiration timestamps for timed inventory items.
/// </summary>
[Guid("2eebe103-6a68-4fdb-b98f-def679d73bf2")]
public interface IItemExpirationViewPlugIn : IViewPlugIn
{
    /// <summary>
    /// Refreshes all timed inventory entries after the inventory has been sent.
    /// </summary>
    ValueTask ShowItemExpirationsAsync();

    /// <summary>
    /// Shows the expiration of one item after it has appeared or changed slots.
    /// </summary>
    /// <param name="item">The item.</param>
    ValueTask ShowItemExpirationAsync(Item item);
}
