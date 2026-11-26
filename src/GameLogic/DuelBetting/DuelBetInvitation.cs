// <copyright file="DuelBetInvitation.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.DuelBetting;

/// <summary>Server settings associated with an incoming duel invitation.</summary>
/// <param name="Requester">The character who issued the challenge.</param>
/// <param name="RequesterClass">The challenger's character class.</param>
/// <param name="Public">Whether the challenger opened the duel to betting.</param>
/// <param name="Seconds">The countdown before combat.</param>
/// <param name="FirstTo">The score that wins the duel.</param>
public sealed record DuelBetInvitation(string Requester, byte? RequesterClass, bool Public, int Seconds, int FirstTo);
