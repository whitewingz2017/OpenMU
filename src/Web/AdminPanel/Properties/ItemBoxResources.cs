// <copyright file="ItemBoxResources.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Properties;

using MUnique.OpenMU.DataModel.Configuration.Items;

/// <summary>Localized reward controls and the client-compatible names of Box of Luck levels.</summary>
internal static class ItemBoxResources
{
    /// <summary>Gets a localized reward editor caption.</summary>
    /// <param name="key">The resource key.</param>
    /// <returns>The localized caption.</returns>
    internal static string Text(string key)
        => Resources.ResourceManager.GetString(key, System.Globalization.CultureInfo.CurrentUICulture)!;

    /// <summary>Gets the visible name of a box variant.</summary>
    /// <param name="item">The box definition.</param>
    /// <param name="level">The source level.</param>
    /// <returns>The localized name shown in the game.</returns>
    internal static string Name(ItemDefinition item, byte level)
    {
        // The Season Six client represents these event items as levels of item 14:11.
        if (item.Group == 14 && item.Number == 11 && level <= 15)
        {
            return Text("EventBoxNames").Split(';')[level];
        }

        return item.GetNameForLevel(level);
    }
}
