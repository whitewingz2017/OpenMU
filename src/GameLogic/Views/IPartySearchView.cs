// <copyright file="IPartySearchView.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PartySearch;

/// <summary>The view of the Party Search.</summary>
[Guid("5defea6a-559e-4cac-a1ee-62faad1d8fee")]
public interface IPartySearchView : IViewPlugIn
{
    /// <summary>Sends the result of a request with the player's own listing and the listed parties.</summary>
    /// <param name="requestId">The processed request identifier.</param>
    /// <param name="result">The result of the request.</param>
    ValueTask ShowAsync(uint requestId, PartySearchResult result);
}
