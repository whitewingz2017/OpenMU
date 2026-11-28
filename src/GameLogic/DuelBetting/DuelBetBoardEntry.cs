// <copyright file="DuelBetBoardEntry.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.DuelBetting;

/// <summary>A snapshot of a public duel and its pools.</summary>
/// <param name="Duel">The match identity.</param>
/// <param name="Requester">The first fighter.</param>
/// <param name="Opponent">The second fighter.</param>
/// <param name="ScoreRequester">The first fighter's score.</param>
/// <param name="ScoreOpponent">The second fighter's score.</param>
/// <param name="Open">Whether the betting window is open.</param>
/// <param name="CanBet">Whether this viewer may place a bet.</param>
/// <param name="SecondsLeft">Time remaining to place bets.</param>
/// <param name="Pools">The confirmed currency pools.</param>
/// <param name="RequesterClass">The first fighter's character class.</param>
/// <param name="OpponentClass">The second fighter's character class.</param>
/// <param name="Started">Whether combat started.</param>
/// <param name="Completed">Whether this duel has a result.</param>
/// <param name="HasBet">Whether the viewer has a confirmed stake.</param>
/// <param name="CanSpectate">Whether the viewer may request a spectator slot.</param>
/// <param name="FirstTo">The winning score.</param>
/// <param name="Wager">The WCoinC stake per fighter, or zero for a jewel pool.</param>
public sealed record DuelBetBoardEntry(
    Guid Duel, string Requester, string Opponent, byte ScoreRequester, byte ScoreOpponent,
    bool Open, bool CanBet, int SecondsLeft, DuelBetPool[] Pools, byte? RequesterClass = null, byte? OpponentClass = null,
    bool Started = false, bool Completed = false, bool HasBet = false, bool CanSpectate = false, int FirstTo = 0, int Wager = 0);
