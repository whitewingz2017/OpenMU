// <copyright file="DailyRewardResources.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.DailyRewards;

using System.Globalization;

/// <summary>Provides localized metadata for the daily login reward plugins and their configuration.</summary>
public static class DailyRewardResources
{
    /// <summary>Gets the feature name.</summary>
    public static string Name => Get("DailyRewards_Name");

    /// <summary>Gets the feature description.</summary>
    public static string Description => Get("DailyRewards_Description");

    /// <summary>Gets the caption of the admin panel menu entry.</summary>
    public static string MenuCaption => Get("DailyRewards_MenuCaption");

    /// <summary>Gets the caption of the reward list.</summary>
    public static string Days => Get("DailyRewards_Days");

    /// <summary>Gets the calendar validation message used by the admin form.</summary>
    public static string InvalidCalendar => Get("DailyRewards_InvalidCalendar");

    /// <summary>Gets the day of month field caption.</summary>
    public static string Day => Get("DailyRewards_Day");

    /// <summary>Gets the zen field caption.</summary>
    public static string Zen => Get("DailyRewards_Zen");

    /// <summary>Gets the WCoinC field caption.</summary>
    public static string WCoinC => Get("DailyRewards_WCoinC");

    /// <summary>Gets the WCoinP field caption.</summary>
    public static string WCoinP => Get("DailyRewards_WCoinP");

    /// <summary>Gets the Goblin Points field caption.</summary>
    public static string GoblinPoints => Get("DailyRewards_GoblinPoints");

    /// <summary>Gets the item field caption.</summary>
    public static string Item => Get("DailyRewards_Item");

    /// <summary>Gets the item level field caption.</summary>
    public static string ItemLevel => Get("DailyRewards_ItemLevel");

    /// <summary>Gets the item quantity field caption.</summary>
    public static string ItemQuantity => Get("DailyRewards_ItemQuantity");

    /// <summary>Gets the item skill field caption.</summary>
    public static string ItemSkill => Get("DailyRewards_ItemSkill");

    /// <summary>Gets the item luck field caption.</summary>
    public static string ItemLuck => Get("DailyRewards_ItemLuck");

    /// <summary>Gets the item option level field caption.</summary>
    public static string ItemOption => Get("DailyRewards_ItemOption");

    /// <summary>Gets the item excellent options field caption.</summary>
    public static string ItemExcellentOptions => Get("DailyRewards_ItemExcellentOptions");

    private static string Get(string key) => PlugInResources.ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? string.Empty;
}
