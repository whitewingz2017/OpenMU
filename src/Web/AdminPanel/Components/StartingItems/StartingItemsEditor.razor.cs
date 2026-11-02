// <copyright file="StartingItemsEditor.razor.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Components.StartingItems;

using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.Persistence.Initialization.PlugIns.CharacterCreated;

/// <summary>Edits the starting items and Zen of each class in expandable sections.</summary>
public partial class StartingItemsEditor
{
    private readonly HashSet<StartingCharacterClass> _expanded = [];

    /// <summary>Gets or sets the editable plugin configuration.</summary>
    [Parameter]
    [EditorRequired]
    public StartingItemsConfiguration Item { get; set; } = null!;

    /// <summary>Gets or sets the current configuration's item definitions.</summary>
    [Parameter]
    [EditorRequired]
    public IEnumerable<ItemDefinition> Definitions { get; set; } = [];

    /// <summary>
    /// Ensures that every class has exactly one profile and that collections loaded from old JSON aren't null.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The normalized configuration.</returns>
    public static StartingItemsConfiguration Normalize(StartingItemsConfiguration? configuration)
    {
        configuration ??= new StartingItemsConfiguration();
        var existing = (configuration.Classes ?? []).Where(c => Enum.IsDefined(c.Class)).GroupBy(c => c.Class).ToDictionary(g => g.Key, g => g.First());
        configuration.Classes = Enum.GetValues<StartingCharacterClass>()
            .Select(c => existing.TryGetValue(c, out var profile) ? profile : new StartingClassItems { Class = c, ReplaceLegacyItems = true })
            .ToList();
        foreach (var profile in configuration.Classes)
        {
            profile.Items ??= [];
            foreach (var reward in profile.Items)
            {
                reward.ExcellentOptions ??= [];
            }
        }

        return configuration;
    }

    /// <summary>Validates the configuration against the item definitions.</summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="definitions">The item definitions.</param>
    /// <returns><c>true</c>, if the configuration is valid.</returns>
    public static bool IsValid(StartingItemsConfiguration configuration, IEnumerable<ItemDefinition> definitions)
    {
        if (configuration.Classes.Select(c => c.Class).Distinct().Count() != configuration.Classes.Count)
        {
            return false;
        }

        foreach (var profile in configuration.Classes)
        {
            if (!Enum.IsDefined(profile.Class) || profile.StartingZen < 0 || profile.Items.Sum(i => (long)i.Quantity) > 64)
            {
                return false;
            }

            foreach (var reward in profile.Items)
            {
                var definition = definitions.SingleOrDefault(i => i.Group == reward.Group && i.Number == reward.Number);
                if (definition is null || !Validator.TryValidateObject(reward, new ValidationContext(reward), null, true))
                {
                    return false;
                }

                try
                {
                    StartingItemOptions.CreateItem(reward, definition);
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static string ClassCaption(StartingCharacterClass characterClass)
    {
        return Regex.Replace(characterClass.ToString(), "(?<=[a-z])(?=[A-Z])", " ");
    }

    private static void AddItem(StartingClassItems profile)
    {
        profile.Items.Add(new StartingItemReward { Number = -1 });
    }

    private void ToggleExpanded(StartingCharacterClass characterClass)
    {
        if (!this._expanded.Remove(characterClass))
        {
            this._expanded.Add(characterClass);
        }
    }
}
