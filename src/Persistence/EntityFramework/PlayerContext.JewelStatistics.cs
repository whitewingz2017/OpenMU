// <copyright file="PlayerContext.JewelStatistics.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.EntityFramework;

using Microsoft.EntityFrameworkCore;
using MUnique.OpenMU.Persistence.EntityFramework.Model;

/// <summary>Jewel reporting and atomic production counters.</summary>
internal partial class PlayerContext
{
    /// <inheritdoc />
    public async ValueTask RecordJewelDropAsync(int itemCode, long amount, DateTime utcNow)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        using var l = await this.LockAsync(default).ConfigureAwait(false);

        // One PostgreSQL statement serializes increments across every game server, without lost updates.
        await this.Context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO data."JewelDropTotal" ("ItemCode", "Total", "TrackingSinceUtc")
            VALUES ({itemCode}, {amount}, {utcNow})
            ON CONFLICT ("ItemCode") DO UPDATE
            SET "Total" = data."JewelDropTotal"."Total" + EXCLUDED."Total"
            """).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<JewelStatisticsSnapshot> GetJewelStatisticsAsync(int[] itemCodes)
    {
        using var l = await this.LockAsync(default).ConfigureAwait(false);

        // Account connections deliberately cannot read config; resolve definitions with the read-only configuration role.
        await using var configuration = new ConfigurationContext();
        var singles = await configuration.Set<ItemDefinition>().AsNoTracking()
            .Where(item => itemCodes.Contains((item.Group * 512) + item.Number))
            .Select(item => new { item.Id, item.Group, item.Number }).ToListAsync().ConfigureAwait(false);
        var bundles = await configuration.Set<JewelMix>().AsNoTracking()
            .Where(mix => itemCodes.Contains((mix.RawSingleJewel.Group * 512) + mix.RawSingleJewel.Number))
            .Select(mix => new { Id = mix.RawMixedJewel.Id, mix.RawSingleJewel.Group, mix.RawSingleJewel.Number })
            .Distinct().ToListAsync().ConfigureAwait(false);

        // Compute codes in CLR int arithmetic; a PostgreSQL arithmetic projection can inherit the byte Group mapping.
        var units = singles.ToDictionary(item => (item.Id, Level: 0), item => new JewelAmount((item.Group * 512) + item.Number, 1));
        foreach (var bundle in bundles)
        {
            for (var level = 0; level <= 2; level++)
            {
                units.TryAdd((bundle.Id, level), new JewelAmount((bundle.Group * 512) + bundle.Number, (level + 1) * 10));
            }
        }

        var definitionIds = units.Keys.Select(key => key.Id).Distinct().ToArray();
        var rows = await this.Context.Database.SqlQuery<JewelStorageRow>($"""
            WITH owned AS (
                SELECT "VaultId" AS id FROM data."Account" WHERE "VaultId" IS NOT NULL
                UNION SELECT "InventoryId" FROM data."Character" WHERE "InventoryId" IS NOT NULL
            )
            SELECT i."ItemStorageId" AS "OwnerId", i."DefinitionId", i."Level"::integer AS "Level", COUNT(*) AS "Amount"
            FROM data."Item" i JOIN owned o ON o.id = i."ItemStorageId"
            WHERE (i."ExpiresAtUtc" IS NULL OR i."ExpiresAtUtc" > {DateTime.UtcNow})
              AND i."DefinitionId" = ANY({definitionIds}) AND i."Level" BETWEEN 0 AND 2
            GROUP BY i."ItemStorageId", i."DefinitionId", i."Level"
            """).ToListAsync().ConfigureAwait(false);
        var owners = rows.Where(row => units.ContainsKey((row.DefinitionId, row.Level))).GroupBy(row => row.OwnerId)
            .Select(group => new JewelOwnerSnapshot(group.Key, false, group
                .Select(row => new JewelAmount(units[(row.DefinitionId, row.Level)].ItemCode, row.Amount * units[(row.DefinitionId, row.Level)].Amount))
                .GroupBy(amount => amount.ItemCode).Select(amounts => new JewelAmount(amounts.Key, amounts.Sum(amount => amount.Amount))).ToList())).ToList();
        var banks = await this.Context.Set<Account>().AsNoTracking()
            .Select(account => new { account.Id, account.JewelBankBalances }).ToListAsync().ConfigureAwait(false);
        owners.AddRange(banks.Select(bank => new JewelOwnerSnapshot(bank.Id, true, JewelBankLedger.Read(bank.JewelBankBalances, itemCodes))));
        var produced = await this.Context.Set<JewelDropTotal>().AsNoTracking()
            .Where(total => itemCodes.Contains(total.ItemCode))
            .Select(total => new JewelAmount(total.ItemCode, total.Total)).ToListAsync().ConfigureAwait(false);
        return new JewelStatisticsSnapshot(owners, produced);
    }
}
