// <copyright file="PartySearchFeaturePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PartySearch;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The Party Search of the custom client: leaders list their party with a minimum level, the wanted classes,
/// guild only and an optional password; other players browse the list and join without asking. The listings
/// live in memory, per game server, and end when the leader leaves the game.
/// </summary>
[PlugIn]
[Display(Name = nameof(PartySearchResources.Name), Description = nameof(PartySearchResources.Description), ResourceType = typeof(PartySearchResources))]
[Guid("f81170b0-0ae6-48ab-a394-368b05d0d16f")]
public sealed class PartySearchFeaturePlugIn : IFeaturePlugIn, IPlayerStateChangedPlugIn
{
    /// <summary>Gets the listed parties of this game server.</summary>
    public PartySearchRegistry Registry { get; } = new();

    /// <summary>Gets the listed parties, if the feature is active.</summary>
    /// <param name="context">The game context.</param>
    /// <returns>The listed parties, or <c>null</c> if the Party Search is disabled.</returns>
    public static PartySearchRegistry? Listings(IGameContext context)
        => context.FeaturePlugIns.GetPlugIn<PartySearchFeaturePlugIn>()?.Registry;

    /// <inheritdoc />
    public ValueTask PlayerStateChangedAsync(Player player, State previousState, State currentState)
    {
        // A listing which outlived its leader's last session ends when the character enters the game again.
        if (currentState == PlayerState.EnteredWorld && previousState == PlayerState.CharacterSelection)
        {
            Listings(player.GameContext)?.Remove(player.Name);
        }

        return ValueTask.CompletedTask;
    }
}
