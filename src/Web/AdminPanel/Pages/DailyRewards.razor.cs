// <copyright file="DailyRewards.razor.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Pages;

using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Cosmetics;
using MUnique.OpenMU.GameLogic.DailyRewards;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Web.AdminPanel.Properties;
using MUnique.OpenMU.Web.Shared.Models;
using MUnique.OpenMU.Web.Shared.Services;

/// <summary>
/// Configures the daily login rewards as a calendar: one slot per day of the month, green when the day has a reward,
/// red when not. A click on a slot opens the editor of that day.
/// </summary>
public partial class DailyRewards
{
    /// <summary>The maximum number of items listed by the item search.</summary>
    private const int MaximumItemMatches = 60;

    /// <summary>The number of items in an item group, which makes up an item type together with the group.</summary>
    private const int ItemsPerGroup = 512;

    private DailyRewardSettings? _configuration;
    private GameConfiguration? _gameConfiguration;
    private PlugInConfigurationViewItem? _plugIn;
    private DailyRewardDay? _editing;
    private DailyRewardItem? _editingItem;
    private bool _addingItem;
    private string _itemSearch = string.Empty;
    private string _status = string.Empty;
    private bool _statusIsError;
    private bool _saving;

    [Inject]
    private PlugInController PlugInController { get; set; } = null!;

    [Inject]
    private IDataSource<GameConfiguration> ConfigurationSource { get; set; } = null!;

    [Inject]
    private ILogger<DailyRewards> Logger { get; set; } = null!;

    private int ConfiguredCount => this._configuration?.Days.Count(day => day.HasReward) ?? 0;

    /// <summary>Gets a localized text of this page.</summary>
    /// <param name="key">The key, without the <c>DailyRewards</c> prefix.</param>
    /// <returns>The localized text.</returns>
    public static string Text(string key) => Resources.ResourceManager.GetString($"DailyRewards{key}", CultureInfo.CurrentUICulture) ?? key;

    /// <inheritdoc />
    protected override Task OnInitializedAsync() => this.LoadAsync();

    private static IEnumerable<string> Summary(DailyRewardDay day)
    {
        if (day.ChooseOneItem)
        {
            yield return Text("ChooseOne");
        }

        foreach (var entry in day.GetItems())
        {
            yield return ItemSummary(entry);
        }

        if (day.Zen > 0)
        {
            yield return string.Format(CultureInfo.CurrentCulture, "{0:N0} {1}", day.Zen, DailyRewardResources.Zen);
        }

        if (day.WCoinC > 0)
        {
            yield return string.Format(CultureInfo.CurrentCulture, "{0:N0} {1}", day.WCoinC, DailyRewardResources.WCoinC);
        }

        if (day.WCoinP > 0)
        {
            yield return string.Format(CultureInfo.CurrentCulture, "{0:N0} {1}", day.WCoinP, DailyRewardResources.WCoinP);
        }

        if (day.GoblinPoints > 0)
        {
            yield return string.Format(CultureInfo.CurrentCulture, "{0:N0} {1}", day.GoblinPoints, DailyRewardResources.GoblinPoints);
        }
    }

    private static string ItemSummary(DailyRewardItem entry)
    {
        var name = entry.Item!.Name.ValueInNeutralLanguage ?? string.Empty;
        if (entry.ItemLevel > 0)
        {
            name += FormattableString.Invariant($" +{entry.ItemLevel}");
        }

        var duration = entry.ItemDurationHours > 0
            ? string.Format(CultureInfo.CurrentCulture, Text("DurationSummary"), entry.ItemDurationHours)
            : Text("Permanent");
        return $"{name} · {duration}";
    }

    private static bool IsSkin(ItemDefinition definition) => CosmeticArmorRules.GetSlot(definition) is not null;

    private static bool IsStackable(ItemDefinition definition) => definition.Durability > 1 && definition.ItemSlot is null;

    private static IEnumerable<IncreasableItemOption> Options(ItemDefinition definition)
        => definition.PossibleItemOptions.SelectMany(group => group.PossibleOptions);

    private static bool HasLuck(ItemDefinition definition)
        => !IsSkin(definition) && Options(definition).Any(option => option.OptionType == ItemOptionTypes.Luck);

    private static bool HasNormalOption(ItemDefinition definition)
        => !IsSkin(definition) && Options(definition).Any(option => option.OptionType == ItemOptionTypes.Option);

    /// <summary>Gets the selectable excellent options; a skin offers its one allowed bonus instead.</summary>
    private static List<IncreasableItemOption> Bonuses(ItemDefinition definition)
        => (IsSkin(definition) ? SkinBonuses.Choices(definition) : Options(definition).Where(option => option.OptionType == ItemOptionTypes.Excellent))
            .Where(option => option.Number is >= 1 and <= 6)
            .GroupBy(option => option.Number)
            .Select(group => group.First())
            .OrderBy(option => option.Number)
            .ToList();

    private static string Label(IncreasableItemOption option) => option.PowerUpDefinition?.ToString() ?? FormattableString.Invariant($"#{option.Number}");

    /// <summary>Gives every day of the month its slot, so that each one can be clicked and set.</summary>
    private static DailyRewardSettings Normalize(DailyRewardSettings settings)
    {
        var days = settings.Days
            .Where(day => day is { Day: >= 1 and <= DailyRewardCalendar.MaximumDays })
            .GroupBy(day => day.Day)
            .Select(group => group.First())
            .ToDictionary(day => day.Day);
        settings.Days = Enumerable.Range(1, DailyRewardCalendar.MaximumDays)
            .Select(day => days.TryGetValue(day, out var existing) ? existing : new DailyRewardDay { Day = day })
            .ToList();
        return settings;
    }

    private IEnumerable<ItemDefinition> ItemMatches()
    {
        var search = this._itemSearch.Trim();
        return (this._gameConfiguration?.Items ?? [])
            .Where(item => search.Length == 0
                || item.Name.ValueInNeutralLanguage?.Contains(search, StringComparison.OrdinalIgnoreCase) == true
                || FormattableString.Invariant($"{item.Group}:{item.Number}") == search)
            .OrderBy(item => item.Group)
            .ThenBy(item => item.Number)
            .Take(MaximumItemMatches);
    }

    private async Task LoadAsync()
    {
        try
        {
            this._gameConfiguration = await this.ConfigurationSource.GetOwnerAsync(Guid.Empty).ConfigureAwait(true);
            this._plugIn = await this.PlugInController.GetByIdAsync(typeof(DailyRewardFeaturePlugIn).GUID).ConfigureAwait(true);
            if (this._plugIn is null)
            {
                this.SetStatus("Missing", true);
                return;
            }

            var loaded = await this.PlugInController.LoadCustomConfigurationAsync(this._plugIn).ConfigureAwait(true) as DailyRewardSettings;
            this._configuration = Normalize(loaded ?? new DailyRewardSettings());
        }
        catch (Exception exception)
        {
            this.Logger.LogError(exception, "Loading the daily reward configuration failed.");
            this.SetStatus("LoadFailed", true);
        }
    }

    private void Edit(DailyRewardDay day)
    {
        this._editing = day;
        this._editingItem = day.GetItems().FirstOrDefault();
        this._addingItem = this._editingItem is null;
        this._itemSearch = string.Empty;
    }

    private void CloseEditor() => this._editing = null;

    private void AddItem()
    {
        this._addingItem = true;
        this._editingItem = null;
        this._itemSearch = string.Empty;
    }

    private void EditItem(DailyRewardItem item)
    {
        this._addingItem = false;
        this._editingItem = item;
    }

    private void SelectItem(DailyRewardDay day, string? value)
    {
        if (day.ItemCount >= DailyRewardDay.MaximumItems
            || !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var type)
            || this._gameConfiguration?.Items.FirstOrDefault(item => (item.Group * ItemsPerGroup) + item.Number == type) is not { } definition)
        {
            return;
        }

        var entry = new DailyRewardItem { Item = definition };
        day.Items.Add(entry);
        this.EditItem(entry);
    }

    private void RemoveItem(DailyRewardDay day, DailyRewardItem item)
    {
        if (ReferenceEquals(day, item))
        {
            day.Item = null;
        }
        else
        {
            day.Items.Remove(item);
        }

        if (day.ItemCount < 2)
        {
            day.ChooseOneItem = false;
        }

        this.Edit(day);
    }

    private void SetBonus(DailyRewardItem day, ItemDefinition definition, byte flag, bool selected)
    {
        if (!selected)
        {
            day.ItemExcellentOptions = (byte)(day.ItemExcellentOptions & ~flag);
            return;
        }

        // A skin has one bonus only.
        day.ItemExcellentOptions = IsSkin(definition) ? flag : (byte)(day.ItemExcellentOptions | flag);
    }

    private void Clear(DailyRewardDay day)
    {
        day.Item = null;
        day.Items.Clear();
        day.ChooseOneItem = false;
        day.ItemDurationHours = 0;
        (day.Zen, day.WCoinC, day.WCoinP, day.GoblinPoints) = (0, 0, 0, 0);
        (day.ItemLevel, day.ItemQuantity, day.ItemOption, day.ItemExcellentOptions) = (0, 0, 0, 0);
        (day.ItemSkill, day.ItemLuck) = (false, false);
        this.Edit(day);
    }

    private async Task SaveAsync()
    {
        if (this._saving || this._configuration is null || this._plugIn is null)
        {
            return;
        }

        if (!this._configuration.IsValid())
        {
            this.SetStatus("Invalid", true);
            return;
        }

        this._saving = true;
        try
        {
            await this.PlugInController.SaveCustomConfigurationAsync(this._plugIn, this._configuration).ConfigureAwait(true);
            this.SetStatus("Saved", false);
        }
        catch (Exception exception)
        {
            this.Logger.LogError(exception, "Saving the daily reward configuration failed.");
            this.SetStatus("SaveFailed", true);
        }
        finally
        {
            this._saving = false;
        }
    }

    private async Task SetActiveAsync(bool active)
    {
        if (this._plugIn is null || this._saving)
        {
            return;
        }

        this._saving = true;
        try
        {
            if (active)
            {
                await this.PlugInController.ActivateAsync(this._plugIn).ConfigureAwait(true);
            }
            else
            {
                await this.PlugInController.DeactivateAsync(this._plugIn).ConfigureAwait(true);
            }

            this._plugIn = await this.PlugInController.GetByIdAsync(typeof(DailyRewardFeaturePlugIn).GUID).ConfigureAwait(true) ?? this._plugIn;
            this.SetStatus(active ? "Activated" : "Deactivated", false);
        }
        catch (Exception exception)
        {
            this.Logger.LogError(exception, "Changing the daily reward activation failed.");
            this.SetStatus("SaveFailed", true);
        }
        finally
        {
            this._saving = false;
        }
    }

    private async Task DiscardAsync()
    {
        this._configuration = null;
        this._editing = null;
        this._status = string.Empty;
        await this.LoadAsync().ConfigureAwait(true);
    }

    private void SetStatus(string key, bool isError)
    {
        this._status = Text(key);
        this._statusIsError = isError;
    }
}
