// <copyright file="IRankTitleView.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views.Inventory;

using System.Runtime.InteropServices;

/// <summary>Shows a confirmed title shop and title appearances to custom clients.</summary>
[Guid("ddd69fd3-c678-40da-905c-f66363aa7e56")]
public interface IRankTitleView : IViewPlugIn
{
    /// <summary>Sends prices, ownership, the active title and the current account balance.</summary>
    /// <param name="requestId">The processed request identifier.</param>
    /// <param name="messageKey">The localized status key, or null for a ready catalog.</param>
    ValueTask ShowAsync(uint requestId, string? messageKey);

    /// <summary>Sends an owner's active title to this observer.</summary>
    /// <param name="owner">The title owner.</param>
    ValueTask AppearanceAsync(Player owner);
}
