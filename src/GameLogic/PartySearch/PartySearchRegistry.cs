// <copyright file="PartySearchRegistry.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PartySearch;

using System.Collections.Concurrent;
using System.Threading;

/// <summary>
/// The listed parties of one game server, in memory: a listing ends when its leader goes offline, leaves the
/// lead of the party or removes it.
/// </summary>
public sealed class PartySearchRegistry
{
    /// <summary>The most parties listed at once.</summary>
    public const int MaximumListings = 100;

    /// <summary>How long a player waits after a wrong password, so passwords can't be tried quickly.</summary>
    private static readonly TimeSpan WrongPasswordPause = TimeSpan.FromSeconds(3);

    private readonly ConcurrentDictionary<string, PartySearchListing> _listings = new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, DateTime> _pausedUntil = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets the lock of joins, so two players never create two parties for one leader.</summary>
    internal SemaphoreSlim JoinLock { get; } = new(1, 1);

    /// <summary>Gets the listings, ordered by the leaders' names.</summary>
    /// <returns>The listings.</returns>
    public IReadOnlyList<PartySearchListing> Snapshot()
        => this._listings.Values.OrderBy(listing => listing.LeaderName, StringComparer.OrdinalIgnoreCase).ToList();

    /// <summary>Gets the listing of a leader.</summary>
    /// <param name="leaderName">The character name of the leader.</param>
    /// <returns>The listing, or <c>null</c>.</returns>
    public PartySearchListing? Find(string leaderName) => this._listings.GetValueOrDefault(leaderName);

    /// <summary>Lists a party, or replaces its listing.</summary>
    /// <param name="listing">The listing.</param>
    /// <returns><c>false</c>, if the list holds no more parties.</returns>
    public bool Set(PartySearchListing listing)
    {
        if (!this._listings.ContainsKey(listing.LeaderName) && this._listings.Count >= MaximumListings)
        {
            return false;
        }

        this._listings[listing.LeaderName] = listing;
        return true;
    }

    /// <summary>Removes the listing of a leader.</summary>
    /// <param name="leaderName">The character name of the leader.</param>
    public void Remove(string leaderName) => this._listings.TryRemove(leaderName, out _);

    /// <summary>Determines whether a player gave a wrong password a moment ago.</summary>
    /// <param name="playerName">The character name of the player.</param>
    /// <returns><c>true</c>, if the player has to wait.</returns>
    internal bool IsPaused(string playerName)
        => this._pausedUntil.TryGetValue(playerName, out var until) && DateTime.UtcNow < until;

    /// <summary>Makes a player wait after a wrong password.</summary>
    /// <param name="playerName">The character name of the player.</param>
    internal void Pause(string playerName)
    {
        foreach (var expired in this._pausedUntil.Where(entry => entry.Value <= DateTime.UtcNow).Select(entry => entry.Key).ToList())
        {
            this._pausedUntil.TryRemove(expired, out _);
        }

        this._pausedUntil[playerName] = DateTime.UtcNow + WrongPasswordPause;
    }
}
