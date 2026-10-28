// <copyright file="ICosmeticArmorView.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views.Inventory;

/// <summary>Confirms cosmetic item ownership and equipment after a request.</summary>
public interface ICosmeticArmorView : IViewPlugIn
{
    /// <summary>Sends confirmed skin slots to the owning player.</summary>
    ValueTask ShowAsync(uint requestId, string status);
}
