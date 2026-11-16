// <copyright file="PartySearchPasswordPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PartySearch;

using System.ComponentModel;
using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Joins a listed party when a player sends its password to the leader: as a whisper, or as
/// <c>/&lt;leader&gt; &lt;password&gt;</c> in the chat, e.g. <c>/ken 1122</c>. The leader doesn't get the password as a
/// whisper; every other whisper passes as usual.
/// </summary>
[PlugIn]
[Display(Name = nameof(PartySearchResources.PasswordName), Description = nameof(PartySearchResources.PasswordDescription), ResourceType = typeof(PartySearchResources))]
[Guid("736e6db6-2ce9-4dfb-b536-dc20a2449da1")]
public sealed class PartySearchPasswordPlugIn : IWhisperMessageReceivedPlugIn
{
    /// <summary>
    /// Joins the party of the leader a chat command names, e.g. <c>/ken 1122</c>, if no chat command has this name
    /// and the named player listed a party with a password.
    /// </summary>
    /// <param name="sender">The player who sent the chat message.</param>
    /// <param name="message">The chat message.</param>
    /// <returns><c>true</c>, if the message named a listed party with a password.</returns>
    public static async ValueTask<bool> TryJoinByCommandAsync(Player sender, string message)
    {
        var words = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length != 2 || words[0].Length < 2 || words[0][0] != '/'
            || !sender.GameContext.PlugInManager.IsPlugInActive(typeof(PartySearchPasswordPlugIn))
            || sender.GameContext.GetPlayerByCharacterName(words[0][1..]) is not { } leader
            || Equals(leader, sender)
            || PartySearchListings.OwnListing(leader) is not { Password.Length: > 0 })
        {
            return false;
        }

        await JoinAsync(sender, leader.Name, words[1]).ConfigureAwait(false);
        return true;
    }

    /// <inheritdoc />
    public void WhisperMessageReceived(Player sender, Player receiver, string message, CancelEventArgs cancelEventArgs)
    {
        if (Equals(sender, receiver) || !PartySearchListings.IsPassword(receiver, message))
        {
            return;
        }

        cancelEventArgs.Cancel = true;
        _ = JoinAndLogAsync(sender, receiver.Name, message);
    }

    private static async Task JoinAndLogAsync(Player sender, string leaderName, string password)
    {
        try
        {
            await JoinAsync(sender, leaderName, password).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            sender.Logger.LogError(ex, "Joining the party of {Leader} by the whispered password failed.", leaderName);
        }
    }

    private static async ValueTask JoinAsync(Player sender, string leaderName, string password)
    {
        var messageKey = await PartySearchActions.JoinAsync(sender, leaderName, password).ConfigureAwait(false) switch
        {
            PartySearchResult.NotListed => "PartySearch_NotListed",
            PartySearchResult.AlreadyInParty => "PartySearch_AlreadyInParty",
            PartySearchResult.WrongPassword => "PartySearch_WrongPassword",
            PartySearchResult.LevelTooLow => "PartySearch_LevelTooLow",
            PartySearchResult.ClassNotWanted => "PartySearch_ClassNotWanted",
            PartySearchResult.GuildOnly => "PartySearch_GuildOnly",
            PartySearchResult.PartyFull => "PartySearch_PartyFull",
            PartySearchResult.NotPossibleHere => "PartySearch_NotPossibleHere",
            PartySearchResult.Denied => "PartySearch_Denied",
            PartySearchResult.TryLater => "PartySearch_TryLater",
            _ => null,
        };

        if (messageKey is not null)
        {
            await sender.ShowLocalizedBlueMessageAsync(messageKey, leaderName).ConfigureAwait(false);
        }
    }
}
