// <copyright file="MarketRegistry.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Market;

using System.Threading;

/// <summary>
/// The listings of the Marketplace, cached for a few seconds: the active ones and the recent history.
/// Writes of this process drop the cache; the stored <see cref="MarketListing.Revision"/> guards every
/// sale, so a listing which another process sold meanwhile can't be sold twice.
/// </summary>
public sealed class MarketRegistry
{
    /// <summary>How long loaded listings are used before they're loaded again.</summary>
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(10);

    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private IReadOnlyList<MarketListingSummary> _listings = [];
    private DateTime _loadedAt = DateTime.MinValue;
    private int _version;

    /// <summary>Gets the lock which serializes the market's writes of this process.</summary>
    /// <remarks>Taken after a player's persistence lock, never before.</remarks>
    public SemaphoreSlim WriteLock { get; } = new(1, 1);

    /// <summary>Drops the cache after a write.</summary>
    public void Invalidate()
    {
        Interlocked.Increment(ref this._version);
        this._loadedAt = DateTime.MinValue;
    }

    /// <summary>Gets the active listings and the history of the last <see cref="MarketSettings.HistoryDays"/>.</summary>
    /// <param name="context">The game context.</param>
    /// <param name="settings">The settings.</param>
    /// <returns>The listings.</returns>
    public async ValueTask<IReadOnlyList<MarketListingSummary>> GetAsync(IGameContext context, MarketSettings settings)
    {
        if (DateTime.UtcNow - this._loadedAt < CacheDuration)
        {
            return this._listings;
        }

        await this._loadLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (DateTime.UtcNow - this._loadedAt >= CacheDuration)
            {
                var version = Volatile.Read(ref this._version);
                this._listings = await LoadAsync(context, settings).ConfigureAwait(false);
                this._loadedAt = version == Volatile.Read(ref this._version) ? DateTime.UtcNow : DateTime.MinValue;
            }

            return this._listings;
        }
        finally
        {
            this._loadLock.Release();
        }
    }

    private static async ValueTask<IReadOnlyList<MarketListingSummary>> LoadAsync(IGameContext context, MarketSettings settings)
    {
        var historySince = DateTime.UtcNow.AddDays(-settings.HistoryDays);
        using var persistence = context.PersistenceContextProvider.CreateNewPlayerContext(context.Configuration);
        var listings = await persistence.GetAsync<MarketListing>().ConfigureAwait(false);
        var muun = context.FeaturePlugIns.GetPlugIn<Muun.MuunFeaturePlugIn>()?.Configuration;
        return listings
            .Where(l => l.State == (int)MarketListingState.Active || l.ClosedAt >= historySince)
            .Select(l => MarketListingSummary.From(l, settings, muun))
            .ToList();
    }
}
