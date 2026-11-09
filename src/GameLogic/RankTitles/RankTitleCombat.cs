// <copyright file="RankTitleCombat.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.RankTitles;

using MUnique.OpenMU.Interfaces;

/// <summary>Persists competitive scores and sends a durable mailbox notification for player deaths.</summary>
internal static class RankTitleCombat
{
    /// <summary>Resolves a player or the owner of a player-controlled attacker.</summary>
    /// <param name="attacker">The source of the fatal attack.</param>
    public static Player? PlayerKiller(IAttacker? attacker) => attacker switch
    {
        Player player => player,
        IPlayerSurrogate surrogate => surrogate.Owner,
        _ => null,
    };

    /// <summary>Excludes self kills and accounts outside ordinary player competition.</summary>
    /// <param name="player">The scoring player.</param>
    /// <param name="other">The defeated player.</param>
    public static bool Qualifies(Player player, Player other)
        => player != other && player.Account is { IsBot: false, IsTemplate: false }
            && other.Account is { IsBot: false, IsTemplate: false }
            && player.SelectedCharacter is { CharacterStatus: CharacterStatus.Normal }
            && other.SelectedCharacter is { CharacterStatus: CharacterStatus.Normal };

    /// <summary>Saves one player kill in the current week's standings.</summary>
    /// <param name="player">The scoring player.</param>
    public static ValueTask RecordKillAsync(Player player) => RecordScoreAsync(player, false);

    /// <summary>Saves one completed duel win.</summary>
    /// <param name="player">The winning player.</param>
    public static ValueTask RecordDuelWinAsync(Player player) => RecordScoreAsync(player, true);

    /// <summary>Saves and forwards the victim's mailbox notification.</summary>
    /// <param name="victim">The player receiving the notification.</param>
    /// <param name="killer">The player named in the notification.</param>
    public static async ValueTask SendDeathMailAsync(Player victim, Player killer)
    {
        try
        {
            using var context = victim.GameContext.PersistenceContextProvider.CreateNewPlayerContext(victim.GameContext.Configuration);
            var header = context.CreateNew<LetterHeader>();
            header.SenderName = killer.Name;
            header.ReceiverName = victim.Name;
            header.Subject = victim.GetLocalizedMessage("PlayerKilledMailSubject");
            header.LetterDate = DateTime.UtcNow;
            var body = context.CreateNew<LetterBody>();
            body.Header = header;
            body.Message = string.Format(victim.GetLocalizedMessage("PlayerKilledMailBody"), killer.Name);
            body.SenderAppearance = context.CreateNew<AppearanceData>();
            body.SenderAppearance.CharacterClass = killer.AppearanceData.CharacterClass;
            if (!await context.CanSaveLetterAsync(header).ConfigureAwait(false)
                || !await context.SaveChangesAsync().ConfigureAwait(false))
            {
                victim.Logger.LogError("Could not save the death notification for {Player}.", victim);
                return;
            }

            if (victim.GameContext is IGameServerContext server)
            {
                await server.FriendServer.ForwardLetterAsync(header).ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            victim.Logger.LogError(exception, "Saving or forwarding the death notification failed for {Player}.", victim);
        }
    }

    private static ValueTask RecordScoreAsync(Player player, bool duel) => player.RunPersistenceExclusiveAsync(async () =>
    {
        if (player.SelectedCharacter is not { } character)
        {
            return;
        }

        var previous = (character.DuelWins, character.LastDuelWinUtc, character.PlayerKillWeekStartUtc,
            character.WeeklyPlayerKills, character.LastWeeklyPlayerKillUtc);
        var saved = false;
        try
        {
            var now = DateTime.UtcNow;
            if (duel && character.DuelWins < int.MaxValue)
            {
                character.DuelWins++;
                character.LastDuelWinUtc = now;
            }
            else if (!duel)
            {
                var week = RankTitleFeaturePlugIn.WeekStartUtc(now);
                if (character.PlayerKillWeekStartUtc != week)
                {
                    character.PlayerKillWeekStartUtc = week;
                    character.WeeklyPlayerKills = 0;
                }

                if (character.WeeklyPlayerKills < int.MaxValue)
                {
                    character.WeeklyPlayerKills++;
                    character.LastWeeklyPlayerKillUtc = now;
                }
            }

            saved = await player.SaveProgressAsync().ConfigureAwait(false);
            if (!saved)
            {
                player.Logger.LogError("Could not save the competitive score for {Player}.", player);
            }
        }
        catch (Exception exception)
        {
            player.Logger.LogError(exception, "Saving the competitive score failed for {Player}.", player);
        }
        finally
        {
            if (!saved)
            {
                (character.DuelWins, character.LastDuelWinUtc, character.PlayerKillWeekStartUtc,
                    character.WeeklyPlayerKills, character.LastWeeklyPlayerKillUtc) = previous;
            }
        }
    });
}
