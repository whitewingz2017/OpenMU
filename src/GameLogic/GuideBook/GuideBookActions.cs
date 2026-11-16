// <copyright file="GuideBookActions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.GuideBook;

using System.Text.Json;
using MUnique.OpenMU.GameLogic.Views;

/// <summary>
/// Loads the Guide Book for the client, and saves and publishes the draft of game masters.
/// </summary>
public sealed class GuideBookActions
{
    private const string DisabledKey = "GuideBook_Disabled";
    private const string GameMasterOnlyKey = "GuideBook_GameMasterOnly";
    private const string InvalidKey = "GuideBook_Invalid";
    private const string DraftChangedKey = "GuideBook_DraftChanged";
    private const string SavedKey = "GuideBook_Saved";
    private const string PublishedKey = "GuideBook_Published";

    /// <summary>Determines whether the player plays a game master character, which may edit the guide.</summary>
    /// <param name="player">The player.</param>
    /// <returns><c>true</c>, if the player may edit the guide.</returns>
    public static bool IsGameMaster(Player player) => player.SelectedCharacter?.CharacterStatus == CharacterStatus.GameMaster;

    /// <summary>Processes a Guide Book request and answers it.</summary>
    /// <param name="player">The player.</param>
    /// <param name="requestId">The request identifier which the answer carries.</param>
    /// <param name="operation">The <see cref="GuideBookOperation"/>.</param>
    /// <param name="data">The UTF-8 JSON of the draft, for a save.</param>
    /// <returns>The completion of the request and its answer.</returns>
    public async ValueTask ExecuteAsync(Player player, uint requestId, byte operation, ReadOnlyMemory<byte> data)
    {
        var (shown, messageKey) = await ProcessAsync(player, (GuideBookOperation)operation, data).ConfigureAwait(false);
        await player.InvokeViewPlugInAsync<IGuideBookView>(view => view.ShowAsync(requestId, messageKey, shown)).ConfigureAwait(false);
        if (messageKey == PublishedKey)
        {
            await player.GameContext.ForEachPlayerAsync(other => other == player
                ? Task.CompletedTask
                : other.InvokeViewPlugInAsync<IGuideBookView>(view => view.ShowAsync(0, null, GuideBookShown.Published)).AsTask()).ConfigureAwait(false);
        }
    }

    private static async ValueTask<(GuideBookShown Shown, string? MessageKey)> ProcessAsync(Player player, GuideBookOperation operation, ReadOnlyMemory<byte> data)
    {
        if (GuideBookFeaturePlugIn.Settings(player.GameContext) is null)
        {
            return (GuideBookShown.None, DisabledKey);
        }

        if (operation == GuideBookOperation.Load)
        {
            return (GuideBookShown.Published, null);
        }

        if (!IsGameMaster(player))
        {
            return (GuideBookShown.None, GameMasterOnlyKey);
        }

        return operation switch
        {
            GuideBookOperation.LoadDraft => (GuideBookShown.Draft, null),
            GuideBookOperation.SaveDraft => await SaveAsync(player, data).ConfigureAwait(false),
            GuideBookOperation.Publish => await PublishAsync(player).ConfigureAwait(false),
            _ => (GuideBookShown.None, InvalidKey),
        };
    }

    private static async ValueTask<(GuideBookShown Shown, string? MessageKey)> SaveAsync(Player player, ReadOnlyMemory<byte> data)
    {
        if (Read(data) is not { } draft || !draft.IsValid())
        {
            return (GuideBookShown.None, InvalidKey);
        }

        var refusal = await GuideBookStore.UpdateAsync(player.GameContext, settings =>
        {
            if (draft.Revision != settings.Draft.Revision)
            {
                return DraftChangedKey;
            }

            draft.Revision = settings.Draft.Revision + 1;
            settings.Draft = draft;
            return null;
        }).ConfigureAwait(false);
        return refusal is null ? (GuideBookShown.Draft, SavedKey) : (GuideBookShown.None, refusal);
    }

    private static async ValueTask<(GuideBookShown Shown, string? MessageKey)> PublishAsync(Player player)
    {
        var refusal = await GuideBookStore.UpdateAsync(player.GameContext, settings =>
        {
            settings.Published = settings.Draft.Clone();
            return null;
        }).ConfigureAwait(false);
        return refusal is null ? (GuideBookShown.Published, PublishedKey) : (GuideBookShown.None, refusal);
    }

    private static GuideBookContent? Read(ReadOnlyMemory<byte> data)
    {
        try
        {
            return JsonSerializer.Deserialize<GuideBookContent>(data.Span);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
