// <copyright file="IMuunView.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views.Inventory;

/// <summary>Confirmed collection and nearby-player Muun appearances.</summary>
public interface IMuunView : IViewPlugIn
{
    /// <summary>Sends the requesting owner's authoritative inventory and species rules.</summary>
    ValueTask ShowAsync(uint requestId, string status);
    /// <summary>Sends the visible pets of a character in the observer's scope.</summary>
    ValueTask AppearanceAsync(Player owner);
}
