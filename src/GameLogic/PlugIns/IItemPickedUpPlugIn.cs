// <copyright file="IItemPickedUpPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// A plugin interface which is called when a player picked up an item from the ground.
/// </summary>
/// <remarks>
/// It allows to react on who obtained a rare drop, e.g. to announce it.
/// </remarks>
[Guid("9876BC3B-0188-404C-BA51-F704C3C4C4FC")]
[PlugInPoint("Item picked up", "Plugins which will be executed when a player picked up an item from the ground.")]
public interface IItemPickedUpPlugIn
{
    /// <summary>
    /// Is called when a player picked up an item from the ground.
    /// </summary>
    /// <param name="player">The player who picked up the item.</param>
    /// <param name="droppedItem">The item which was on the ground. Its <see cref="DroppedItem.Position"/> is where it lay.</param>
    ValueTask ItemPickedUpAsync(Player player, DroppedItem droppedItem);
}
