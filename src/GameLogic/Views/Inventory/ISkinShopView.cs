// <copyright file="ISkinShopView.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views.Inventory;

/// <summary>Shows a skin shop catalog or the result of a purchase.</summary>
public interface ISkinShopView : IViewPlugIn
{
    /// <summary>Sends authoritative prices, balance and standard bonus choices.</summary>
    /// <param name="requestId">The processed request identifier.</param>
    /// <param name="status">The result shown to the player.</param>
    /// <param name="includeCatalog">Whether to send current prices and bonus choices.</param>
    ValueTask ShowAsync(uint requestId, string status, bool includeCatalog);
}
