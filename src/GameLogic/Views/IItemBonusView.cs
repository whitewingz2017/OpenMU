// <copyright file="IItemBonusView.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.ItemBonuses;

/// <summary>Shows the picked items' attribute lines in the custom client's tooltips.</summary>
[Guid("5b8e2d41-7c3a-4f96-9e10-a4d6c2b8f753")]
public interface IItemBonusView : IViewPlugIn
{
    /// <summary>Sends the lines.</summary>
    /// <param name="answer">The answer.</param>
    /// <returns>The completion.</returns>
    ValueTask ShowAsync(ItemBonusAnswer answer);
}
