// <copyright file="PlayerInMemoryContext.JewelStatistics.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.InMemory;

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using MUnique.OpenMU.Persistence.BasicModel;

/// <summary>Jewel reporting for the in-memory database.</summary>
public partial class PlayerInMemoryContext
{
    private static readonly ConditionalWeakTable<InMemoryRepositoryProvider, ConcurrentDictionary<int, long>> JewelTotals = new();

    /// <inheritdoc />
    public ValueTask RecordJewelDropAsync(int itemCode, long amount, DateTime utcNow)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        JewelTotals.GetValue(this.Provider, _ => new()).AddOrUpdate(itemCode, amount, (_, current) => checked(current + amount));
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask<JewelStatisticsSnapshot> GetJewelStatisticsAsync(int[] itemCodes)
    {
        var accounts = await this.Provider.GetRepository<Account>().GetAllAsync().ConfigureAwait(false);
        var characters = await this.Provider.GetRepository<Character>().GetAllAsync().ConfigureAwait(false);
        var mixes = await this.Provider.GetRepository<JewelMix>().GetAllAsync().ConfigureAwait(false);
        var configuration = new GameConfiguration();
        foreach (var mix in mixes)
        {
            configuration.JewelMixes.Add(mix);
        }

        var stores = accounts.Select(account => account.Vault).Concat(characters.Select(character => character.Inventory))
            .OfType<DataModel.Entities.ItemStorage>().DistinctBy(storage => storage.GetId());
        var owners = stores.Select(storage => new JewelOwnerSnapshot(storage.GetId(), false,
            JewelStatistics.Count(storage.Items, configuration, itemCodes, DateTime.UtcNow))).ToList();
        owners.AddRange(accounts.Select(account => new JewelOwnerSnapshot(account.GetId(), true, JewelBankLedger.Read(account.JewelBankBalances, itemCodes))));
        var totals = JewelTotals.GetValue(this.Provider, _ => new());
        return new JewelStatisticsSnapshot(owners, itemCodes.Select(code => new JewelAmount(code, totals.GetValueOrDefault(code))).ToList());
    }
}
