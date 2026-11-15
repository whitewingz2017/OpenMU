// <copyright file="MonsterDropPickedUpEvent.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Interfaces;

/// <summary>
/// A player picked up a notable item, e.g. a jewel, which a monster dropped.
/// </summary>
/// <remarks>
/// Names of configuration objects, like maps, monsters or items, are passed as the full value of a <see cref="LocalizedString"/>,
/// so that a receiver can translate them with <c>new LocalizedString(name).GetTranslation(culture)</c>.
/// </remarks>
/// <param name="ServerId">The identifier of the game server on which the event happened.</param>
/// <param name="TimestampUtc">The timestamp, in UTC, when the item was picked up.</param>
/// <param name="CharacterName">The name of the character who picked up the item.</param>
/// <param name="MonsterName">The name of the monster which dropped the item.</param>
/// <param name="MapName">The name of the map.</param>
/// <param name="X">The x coordinate where the item lay.</param>
/// <param name="Y">The y coordinate where the item lay.</param>
/// <param name="ItemName">The name of the item.</param>
/// <param name="ItemLevel">The level of the item.</param>
public sealed record MonsterDropPickedUpEvent(byte ServerId, DateTime TimestampUtc, string CharacterName, string MonsterName, string MapName, byte X, byte Y, string ItemName, byte ItemLevel)
    : GameEvent(ServerId, TimestampUtc);
