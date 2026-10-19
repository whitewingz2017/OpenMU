// <copyright file="IJewelBankView.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views.Inventory;

using MUnique.OpenMU.GameLogic.JewelBank;

/// <summary>Displays a confirmed bank snapshot to the requesting client.</summary>
public interface IJewelBankView : IViewPlugIn
{
    /// <summary>Displays balances after the request has completed.</summary>
    /// <param name="request">The request identifier.</param>
    /// <param name="status">The result message.</param>
    /// <param name="rows">The confirmed balances.</param>
    ValueTask ShowAsync(uint request, string status, IReadOnlyList<JewelBankRow> rows);
}
