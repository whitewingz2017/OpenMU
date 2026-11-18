// <copyright file="FeatureListActions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Features;

using MUnique.OpenMU.GameLogic.Views;

/// <summary>Answers the custom client's question which of its features are active.</summary>
public sealed class FeatureListActions
{
    /// <summary>Sends the features.</summary>
    /// <param name="player">The player.</param>
    /// <param name="requestId">The request identifier which the answer carries.</param>
    /// <returns>The completion.</returns>
    public ValueTask ExecuteAsync(Player player, uint requestId)
    {
        var answer = new FeatureListAnswer { RequestId = requestId, Features = FeatureList.Create(player.GameContext) };
        return player.InvokeViewPlugInAsync<IFeatureListView>(view => view.ShowAsync(answer));
    }
}
