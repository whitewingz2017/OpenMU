// <copyright file="DailyRewardView.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Inventory;

using System.Runtime.InteropServices;
using System.Text.Json;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.DailyRewards;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>Sends the daily login rewards of the current month and the days the account claimed.</summary>
[PlugIn]
[Display(Name = nameof(DailyRewardResources.Name), Description = nameof(DailyRewardResources.Description), ResourceType = typeof(DailyRewardResources))]
[Guid("9325d675-5d89-4004-8540-0a4df2259b89")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public sealed class DailyRewardView : IDailyRewardView
{
    private readonly RemotePlayer _player;

    /// <summary>Initializes a new instance of the <see cref="DailyRewardView"/> class.</summary>
    /// <param name="player">The receiving player.</param>
    public DailyRewardView(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask ShowAsync(uint requestId, string? messageKey)
    {
        if (this._player.Connection is not { } connection)
        {
            return;
        }

        var settings = DailyRewardFeaturePlugIn.Settings(this._player.GameContext);
        var (month, today, daysInMonth) = DailyRewardCalendar.Today(DateTime.UtcNow);
        var data = JsonSerializer.SerializeToUtf8Bytes(new
        {
            RequestId = requestId,
            Status = messageKey is null ? "Ready" : this._player.GetLocalizedMessage(messageKey),
            Enabled = settings is not null,
            Month = month,
            Today = today,
            DaysInMonth = daysInMonth,
            ClaimedMask = this._player.Account is { } account ? DailyRewardCalendar.ClaimedDays(account, month) : 0,
            Days = (settings?.Days ?? []).Where(day => day.HasReward && day.Day <= daysInMonth).OrderBy(day => day.Day).Select(day => new
            {
                day.Day,
                day.Zen,
                day.WCoinC,
                day.WCoinP,
                day.GoblinPoints,
                Item = day.Item is { } item ? new
                {
                    ItemType = (item.Group * 512) + item.Number,
                    Name = item.Name.ValueInNeutralLanguage,
                    Level = Math.Min(day.ItemLevel, item.MaximumItemLevel),
                    Durability = item.Durability > 1 && item.ItemSlot is null && day.ItemQuantity > 0 ? Math.Min(day.ItemQuantity, item.Durability) : item.Durability,
                    Luck = day.ItemLuck,
                    Skill = day.ItemSkill,
                    Option = day.ItemOption,
                    Excellent = day.ItemExcellentOptions,
                } : null,
            }),
        });
        await connection.SendAsync(() =>
        {
            var size = DailyRewardResponseExtendedRef.GetRequiredSize(data.Length);
            var packet = new DailyRewardResponseExtendedRef(connection.Output.GetSpan(size)[..size]);
            data.CopyTo(packet.Data);
            return size;
        }).ConfigureAwait(false);
    }
}
