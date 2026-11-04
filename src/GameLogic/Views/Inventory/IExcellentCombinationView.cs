// <copyright file="IExcellentCombinationView.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views.Inventory;

/// <summary>Confirms an excellent combination after authoritative validation.</summary>
public interface IExcellentCombinationView : IViewPlugIn
{
    /// <summary>Sends authoritative rules, counts and quote or committed attempt status.</summary>
    /// <param name="request">The request identifier.</param>
    /// <param name="status">The localized status.</param>
    /// <param name="valid">Whether the quote is valid.</param>
    /// <param name="success">Whether a committed attempt succeeded.</param>
    /// <param name="completed">Whether resources were consumed.</param>
    /// <param name="settings">The settings used.</param>
    /// <param name="availableCores">The current usable core count.</param>
    /// <param name="selectedCores">The quoted or consumed count.</param>
    /// <param name="result">The main gear after the attempt.</param>
    /// <returns>The asynchronous send.</returns>
    ValueTask ShowAsync(uint request, string status, bool valid, bool success, bool completed, MUnique.OpenMU.GameLogic.PlayerActions.Items.ExcellentCombinationConfiguration settings, int availableCores, int selectedCores, MUnique.OpenMU.DataModel.Entities.Item? result);
}
