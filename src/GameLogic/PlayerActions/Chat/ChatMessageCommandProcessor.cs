// <copyright file="ChatMessageCommandProcessor.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Chat;

using MUnique.OpenMU.GameLogic.PartySearch;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

/// <summary>
/// A chat message processor which handles chat commands.
/// </summary>
public class ChatMessageCommandProcessor : IChatMessageProcessor
{
    /// <inheritdoc />
    public async ValueTask ProcessMessageAsync(Player sender, (string Message, string PlayerName) content)
    {
        var commandKey = content.Message.Split(' ').First();
        var commandHandler = sender.GameContext.PlugInManager.GetStrategy<IChatCommandPlugIn>(commandKey);
        if (commandHandler is null)
        {
            // "/ken 1122": no command has the name, so it may send the password of ken's listed party.
            await PartySearchPasswordPlugIn.TryJoinByCommandAsync(sender, content.Message).ConfigureAwait(false);
            return;
        }

        if (sender.SelectedCharacter!.CharacterStatus < commandHandler.MinCharacterStatusRequirement)
        {
            sender.Logger.LogWarning($"{sender.Name} is trying to execute {commandKey} command without meeting the requirements");
            return;
        }

        await commandHandler.HandleCommandAsync(sender, content.Message).ConfigureAwait(false);
    }
}