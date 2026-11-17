// <copyright file="OfflineLevelingChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Offline;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Handles the <c>/offlevel</c> chat command.
/// <list type="bullet">
///   <item>Logs the login-server entry off so the real player can re-connect at any time.</item>
///   <item>Disconnects the real client connection.</item>
///   <item>Creates a silent ghost player on the current map using the character's MU Helper config.</item>
///   <item>With the paid offline leveling (<see cref="OfflineLevelingFeaturePlugIn"/>), opens its window first.</item>
///   <item>On next login the ghost is automatically stopped before character selection.</item>
/// </list>
/// </summary>
[Guid("A1C4E7F2-3B8D-4A09-8E5C-2D6F0B3A7E14")]
[PlugIn]
[Display(
    Name = nameof(PlugInResources.OfflineLevelingChatCommandPlugIn_Name),
    Description = nameof(PlugInResources.OfflineLevelingChatCommandPlugIn_Description),
    ResourceType = typeof(PlugInResources))]
[ChatCommandHelp(Command, CharacterStatus.Normal)]
public sealed class OfflineLevelingChatCommandPlugIn : IChatCommandPlugIn
{
    private const string Command = "/offlevel";

    /// <inheritdoc />
    public string Key => Command;

    /// <inheritdoc />
    public CharacterStatus MinCharacterStatusRequirement => CharacterStatus.Normal;

    /// <inheritdoc />
    public async ValueTask HandleCommandAsync(Player player, string command)
    {
        var result = OfflineLevelingRules.Check(player);
        if (result != OfflineLevelingResult.None)
        {
            await player.ShowLocalizedBlueMessageAsync(MessageOf(result)).ConfigureAwait(false);
            return;
        }

        // With the paid offline leveling, the window chooses the currency, the hours and the protection.
        if (OfflineLevelingFeaturePlugIn.GetSettings(player.GameContext) is { } settings)
        {
            await OfflineLevelingActions.ShowOfferAsync(player, settings).ConfigureAwait(false);
            return;
        }

        await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.OfflineLevelingStarted)).ConfigureAwait(false);
        if (!await player.GameContext.OfflinePlayerManager.StartAsync(player, player.Account!.LoginName).ConfigureAwait(false))
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.OfflineLevelingFailed)).ConfigureAwait(false);
        }
    }

    private static string MessageOf(OfflineLevelingResult result) => result switch
    {
        OfflineLevelingResult.Dead => nameof(PlayerMessage.OfflineLevelingMustBeAlive),
        OfflineLevelingResult.NotOnMap => nameof(PlayerMessage.OfflineLevelingNotOnMap),
        OfflineLevelingResult.InMiniGame => nameof(PlayerMessage.OfflineLevelingInMiniGame),
        OfflineLevelingResult.MuHelperOff => nameof(PlayerMessage.OfflineLevelingMuHelperNotRunning),
        OfflineLevelingResult.AlreadyActive => nameof(PlayerMessage.OfflineLevelingAlreadyActive),
        _ => nameof(PlayerMessage.OfflineLevelingNoCharacterSelected),
    };
}