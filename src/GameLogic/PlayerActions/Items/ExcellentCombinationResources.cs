// <copyright file="ExcellentCombinationResources.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Items;

/// <summary>Resource access following the existing Muun localization pattern.</summary>
public static class ExcellentCombinationResources
{
    /// <summary>Gets the localized Name caption.</summary>
    public static string Name => Properties.PlugInResources.ResourceManager.GetString("ExcellentCombination_Name", System.Globalization.CultureInfo.CurrentUICulture) ?? "ExcellentCombination_Name";

    /// <summary>Gets the localized Description caption.</summary>
    public static string Description => Properties.PlugInResources.ResourceManager.GetString("ExcellentCombination_Description", System.Globalization.CultureInfo.CurrentUICulture) ?? "ExcellentCombination_Description";

    /// <summary>Gets the localized BaseRate caption.</summary>
    public static string BaseRate => Properties.PlugInResources.ResourceManager.GetString("ExcellentCombination_BaseRate", System.Globalization.CultureInfo.CurrentUICulture) ?? "ExcellentCombination_BaseRate";

    /// <summary>Gets the localized BonusPerCore caption.</summary>
    public static string BonusPerCore => Properties.PlugInResources.ResourceManager.GetString("ExcellentCombination_BonusPerCore", System.Globalization.CultureInfo.CurrentUICulture) ?? "ExcellentCombination_BonusPerCore";

    /// <summary>Gets the localized MaximumRate caption.</summary>
    public static string MaximumRate => Properties.PlugInResources.ResourceManager.GetString("ExcellentCombination_MaximumRate", System.Globalization.CultureInfo.CurrentUICulture) ?? "ExcellentCombination_MaximumRate";

    /// <summary>Gets the localized ZenCost caption.</summary>
    public static string ZenCost => Properties.PlugInResources.ResourceManager.GetString("ExcellentCombination_ZenCost", System.Globalization.CultureInfo.CurrentUICulture) ?? "ExcellentCombination_ZenCost";

    /// <summary>Gets a localized player message.</summary>
    /// <param name="key">The message resource suffix.</param>
    /// <returns>The localized message.</returns>
    public static string Message(string key) => Properties.PlayerMessage.ResourceManager.GetString("ExcellentCombination_" + key, System.Globalization.CultureInfo.CurrentUICulture) ?? key;
}
