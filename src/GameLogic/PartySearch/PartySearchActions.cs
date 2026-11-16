// <copyright file="PartySearchActions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PartySearch;

using System.Text.Json;
using MUnique.OpenMU.GameLogic.PlayerActions.Party;
using MUnique.OpenMU.GameLogic.Views;

/// <summary>
/// Loads the Party Search for the client, saves the player's listing and joins listed parties.
/// </summary>
public sealed class PartySearchActions
{
    /// <summary>The longest password of a listing.</summary>
    public const int MaximumPasswordLength = 10;

    /// <summary>The highest minimum level of a listing.</summary>
    private const int MaximumLevel = 1000;

    /// <summary>Lets <paramref name="player"/> join the listed party of <paramref name="leaderName"/>.</summary>
    /// <param name="player">The player who wants to join.</param>
    /// <param name="leaderName">The character name of the leader.</param>
    /// <param name="password">The given password; ignored when the listing has none.</param>
    /// <returns><see cref="PartySearchResult.Joined"/>, or why the player couldn't join.</returns>
    public static async ValueTask<PartySearchResult> JoinAsync(Player player, string leaderName, string? password)
    {
        if (PartySearchFeaturePlugIn.Listings(player.GameContext) is not { } registry)
        {
            return PartySearchResult.Disabled;
        }

        if (registry.IsPaused(player.Name))
        {
            return PartySearchResult.TryLater;
        }

        await registry.JoinLock.WaitAsync().ConfigureAwait(false);
        Player leader;
        try
        {
            if (registry.Find(leaderName) is not { } listing
                || PartySearchListings.LeaderOf(player.GameContext, registry, listing) is not { } listedLeader)
            {
                return PartySearchResult.NotListed;
            }

            leader = listedLeader;
            if (listing.Password.Length > 0 && !string.Equals(password?.Trim(), listing.Password, StringComparison.Ordinal))
            {
                registry.Pause(player.Name);
                return PartySearchResult.WrongPassword;
            }

            var requirements = PartySearchListings.Requirements(player, leader, listing);
            if (requirements != PartySearchResult.None)
            {
                return requirements;
            }

            if (await PartyRequestAction.IsPartyDeniedAsync(leader, player).ConfigureAwait(false))
            {
                return PartySearchResult.Denied;
            }

            if (!await AddAsync(player, leader).ConfigureAwait(false))
            {
                return PartySearchResult.PartyFull;
            }
        }
        finally
        {
            registry.JoinLock.Release();
        }

        await player.ShowLocalizedBlueMessageAsync("PartySearch_Joined", leader.Name).ConfigureAwait(false);
        await leader.ShowLocalizedBlueMessageAsync("PartySearch_MemberJoined", player.Name).ConfigureAwait(false);
        return PartySearchResult.Joined;
    }

    /// <summary>Processes a Party Search request and answers it.</summary>
    /// <param name="player">The player.</param>
    /// <param name="requestId">The request identifier which the answer carries.</param>
    /// <param name="operation">The <see cref="PartySearchOperation"/>.</param>
    /// <param name="data">The UTF-8 JSON of the <see cref="PartySearchRequestData"/>, for a save or a join.</param>
    /// <returns>The completion of the request and its answer.</returns>
    public async ValueTask ExecuteAsync(Player player, uint requestId, byte operation, ReadOnlyMemory<byte> data)
    {
        var result = await ProcessAsync(player, (PartySearchOperation)operation, data).ConfigureAwait(false);
        await player.InvokeViewPlugInAsync<IPartySearchView>(view => view.ShowAsync(requestId, result)).ConfigureAwait(false);
    }

    private static async ValueTask<bool> AddAsync(Player player, Player leader)
    {
        if (leader.Party is { } party)
        {
            return await party.AddAsync(player).ConfigureAwait(false);
        }

        var newParty = player.GameContext.PartyManager.CreateParty();
        await newParty.AddAsync(leader).ConfigureAwait(false);
        return await newParty.AddAsync(player).ConfigureAwait(false);
    }

    private static async ValueTask<PartySearchResult> ProcessAsync(Player player, PartySearchOperation operation, ReadOnlyMemory<byte> data)
    {
        if (PartySearchFeaturePlugIn.Listings(player.GameContext) is not { } registry)
        {
            return PartySearchResult.Disabled;
        }

        return operation switch
        {
            PartySearchOperation.Load => PartySearchResult.None,
            PartySearchOperation.Save => Save(player, registry, Read(data)),
            PartySearchOperation.Join => Read(data) is { Leader: { Length: > 0 } leaderName } request
                ? await JoinAsync(player, leaderName, request.Password).ConfigureAwait(false)
                : PartySearchResult.Invalid,
            _ => PartySearchResult.Invalid,
        };
    }

    private static PartySearchResult Save(Player player, PartySearchRegistry registry, PartySearchRequestData? request)
    {
        if (request is null)
        {
            return PartySearchResult.Invalid;
        }

        if (!request.Active)
        {
            registry.Remove(player.Name);
            return PartySearchResult.Removed;
        }

        var password = request.Password?.Trim() ?? string.Empty;
        if (request.MinimumLevel is < 0 or > MaximumLevel
            || request.Classes is < 0 or > PartySearchListing.AllClasses
            || password.Length > MaximumPasswordLength
            || !password.All(char.IsAsciiLetterOrDigit))
        {
            return PartySearchResult.Invalid;
        }

        if (player.Party is { } party && !Equals(party.PartyMaster, player))
        {
            return PartySearchResult.NotPartyMaster;
        }

        var listing = new PartySearchListing
        {
            LeaderName = player.Name,
            MinimumLevel = request.MinimumLevel,
            GuildOnly = request.GuildOnly,
            OnePerClass = request.OnePerClass,
            Classes = (byte)(request.Classes == 0 ? PartySearchListing.AllClasses : request.Classes),
            Password = password,
        };

        return registry.Set(listing) ? PartySearchResult.Listed : PartySearchResult.ListFull;
    }

    private static PartySearchRequestData? Read(ReadOnlyMemory<byte> data)
    {
        if (data.IsEmpty)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<PartySearchRequestData>(data.Span);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
