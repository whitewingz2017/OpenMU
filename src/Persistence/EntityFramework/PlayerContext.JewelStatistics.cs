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
        var rows = await this.Context.Database.SqlQuery<JewelStorageRow>($"""
            WITH mixes AS (
                SELECT DISTINCT "MixedJewelId", "SingleJewelId" FROM config."JewelMix"
            ), owned AS (
                SELECT "VaultId" AS id FROM data."Account" WHERE "VaultId" IS NOT NULL
                UNION SELECT "InventoryId" FROM data."Character" WHERE "InventoryId" IS NOT NULL
            )
            SELECT i."ItemStorageId" AS "OwnerId",
                   CASE WHEN s."Id" IS NULL THEN d."Group" * 512 + d."Number"
                        ELSE s."Group" * 512 + s."Number" END AS "ItemCode",
                   SUM(CASE WHEN s."Id" IS NULL THEN 1::bigint ELSE (i."Level" + 1) * 10::bigint END)::bigint AS "Amount"
            FROM data."Item" i JOIN owned o ON o.id = i."ItemStorageId"
            JOIN config."ItemDefinition" d ON d."Id" = i."DefinitionId"
            LEFT JOIN mixes m ON m."MixedJewelId" = d."Id"
            LEFT JOIN config."ItemDefinition" s ON s."Id" = m."SingleJewelId"
            WHERE (i."ExpiresAtUtc" IS NULL OR i."ExpiresAtUtc" > {DateTime.UtcNow})
              AND ((s."Id" IS NULL AND i."Level" = 0 AND d."Group" * 512 + d."Number" = ANY({itemCodes}))
                OR (s."Group" * 512 + s."Number" = ANY({itemCodes}) AND i."Level" BETWEEN 0 AND 2))
            GROUP BY i."ItemStorageId", "ItemCode"
            """).ToListAsync().ConfigureAwait(false);
        var owners = rows.GroupBy(row => row.OwnerId)
            .Select(group => new JewelOwnerSnapshot(group.Key, false, group.Select(row => new JewelAmount(row.ItemCode, row.Amount)).ToList())).ToList();
        var banks = await this.Context.Set<Account>().AsNoTracking()
            .Select(account => new { account.Id, account.JewelBankBalances }).ToListAsync().ConfigureAwait(false);
        owners.AddRange(banks.Select(bank => new JewelOwnerSnapshot(bank.Id, true, JewelBankLedger.Read(bank.JewelBankBalances, itemCodes))));
        var produced = await this.Context.Set<JewelDropTotal>().AsNoTracking()
            .Where(total => itemCodes.Contains(total.ItemCode))
            .Select(total => new JewelAmount(total.ItemCode, total.Total)).ToListAsync().ConfigureAwait(false);
        return new JewelStatisticsSnapshot(owners, produced);
    }
}
