// <copyright file="IOfflineLevelingView.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Offline;

/// <summary>The view of the offline leveling window.</summary>
[Guid("c82aca62-803a-4285-b5eb-96b207e4c1d4")]
public interface IOfflineLevelingView : IViewPlugIn
{
    /// <summary>Sends the offer, the result of a request, and whether to open the window.</summary>
    /// <param name="answer">The answer.</param>
    /// <returns>The completion of the sending.</returns>
    ValueTask ShowAsync(OfflineLevelingAnswer answer);
}
