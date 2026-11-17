// <copyright file="IMarketView.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Market;

/// <summary>The view of the Marketplace.</summary>
[Guid("90efe4b7-c5bd-4a52-9726-45dc2a7b3e6d")]
public interface IMarketView : IViewPlugIn
{
    /// <summary>Sends the answer of a request: its result, the currencies, a page and the player's listings.</summary>
    /// <param name="answer">The answer.</param>
    /// <returns>The completion of the sending.</returns>
    ValueTask ShowAsync(MarketAnswer answer);
}
