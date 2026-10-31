// <copyright file="IExcellentCombinationView.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views.Inventory;

/// <summary>Confirms an excellent combination after authoritative validation.</summary>
public interface IExcellentCombinationView : IViewPlugIn
{
    ValueTask ShowAsync(uint request, string status, bool valid, bool success, int cost, MUnique.OpenMU.DataModel.Entities.Item? result);
}
