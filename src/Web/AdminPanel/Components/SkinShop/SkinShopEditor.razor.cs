// <copyright file="SkinShopEditor.razor.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Components.SkinShop;

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Cosmetics;
using MUnique.OpenMU.Web.AdminPanel.Properties;

/// <summary>Edits category availability and individual skin sale prices.</summary>
public partial class SkinShopEditor
{
    /// <summary>Gets or sets the editable plugin configuration.</summary>
    [Parameter]
    [EditorRequired]
    public SkinShopSettings Item { get; set; } = null!;

    /// <summary>Gets or sets the current item definitions.</summary>
    [Parameter]
    [EditorRequired]
    public IEnumerable<ItemDefinition> Definitions { get; set; } = [];

    /// <summary>Gets a localized skin shop caption.</summary>
    /// <param name="key">The caption resource suffix.</param>
    /// <returns>The localized caption.</returns>
    public static string Text(string key) => Resources.ResourceManager.GetString($"SkinShop{key}", CultureInfo.CurrentUICulture) ?? key;

    /// <summary>Fills missing entries using existing category defaults without changing saved prices.</summary>
    /// <param name="settings">The loaded settings.</param>
    /// <param name="definitions">The available definitions.</param>
    /// <returns>The editable settings.</returns>
    public static SkinShopSettings Normalize(SkinShopSettings? settings, IEnumerable<ItemDefinition> definitions)
    {
        settings ??= new SkinShopSettings();
        settings.DisabledCategories ??= [];
        settings.Items ??= [];
        foreach (var definition in definitions.Where(FenrirMountRules.IsShopItem))
        {
            var type = (definition.Group * 512) + definition.Number;
            if (!settings.Items.ContainsKey(type))
            {
                settings.Items.Add(type, new SkinShopItemSettings { Price = Math.Max(0, settings.DefaultPrice(SkinShopSettings.Category(definition))) });
            }
        }

        return settings;
    }

    /// <summary>Validates category values and all item prices before saving.</summary>
    /// <param name="settings">The edited settings.</param>
    /// <returns>Whether all settings are valid.</returns>
    public static bool IsValid(SkinShopSettings settings)
        => Validator.TryValidateObject(settings, new ValidationContext(settings), null, true)
            && settings.DisabledCategories.All(Enum.IsDefined)
            && settings.Items.All(entry => entry.Key is >= 0 and < 8192 && entry.Value is not null
                && Validator.TryValidateObject(entry.Value, new ValidationContext(entry.Value), null, true));

    private void SetCategoryEnabled(SkinShopCategory category, bool enabled)
    {
        if (enabled)
        {
            this.Item.DisabledCategories.Remove(category);
        }
        else
        {
            this.Item.DisabledCategories.Add(category);
        }
    }
}
