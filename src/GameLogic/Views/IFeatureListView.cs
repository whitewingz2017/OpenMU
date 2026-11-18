// <copyright file="IFeatureListView.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Features;

/// <summary>Tells the custom client which of its features are active.</summary>
[Guid("9a3e1c57-2b4d-4f80-8e6a-71c5d0b2e914")]
public interface IFeatureListView : IViewPlugIn
{
    /// <summary>Sends the features.</summary>
    /// <param name="answer">The answer.</param>
    /// <returns>The completion.</returns>
    ValueTask ShowAsync(FeatureListAnswer answer);
}
