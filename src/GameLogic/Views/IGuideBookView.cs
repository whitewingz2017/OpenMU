// <copyright file="IGuideBookView.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.GuideBook;

/// <summary>The view of the Guide Book.</summary>
[Guid("41d502d9-5606-4d93-a4f0-1a329629c3b8")]
public interface IGuideBookView : IViewPlugIn
{
    /// <summary>Sends a guide, or only the status of a refused request.</summary>
    /// <param name="requestId">The processed request identifier; zero when a game master published the guide.</param>
    /// <param name="messageKey">The localized status key, or <c>null</c> for no status.</param>
    /// <param name="shown">The guide which is sent.</param>
    ValueTask ShowAsync(uint requestId, string? messageKey, GuideBookShown shown);
}
