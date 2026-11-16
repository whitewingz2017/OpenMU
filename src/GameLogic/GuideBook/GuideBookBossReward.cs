// <copyright file="GuideBookBossReward.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.GuideBook;

/// <summary>
/// What a boss gives its killer, for the boss table of the Guide Book.
/// </summary>
/// <param name="Monster">The number of the boss monster.</param>
/// <param name="Name">The name of the boss.</param>
/// <param name="WCoinC">The WCoinC of the killer.</param>
/// <param name="WCoinP">The WCoinP of the killer.</param>
/// <param name="GoblinPoints">The Goblin Points of the killer.</param>
/// <param name="CoreChance">The chance of a Resonance Core per kill, in percent.</param>
/// <param name="CoreCount">The most Resonance Cores the boss drops in a day.</param>
public sealed record GuideBookBossReward(short Monster, string Name, int WCoinC, int WCoinP, int GoblinPoints, double CoreChance, int CoreCount);
