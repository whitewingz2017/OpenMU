// <copyright file="PvpBalanceFeaturePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Pvp;

using System.Globalization;
using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>Configures PvP accuracy and damage by character class family.</summary>
[PlugIn]
[Display(Name = nameof(Name), Description = nameof(Description), ResourceType = typeof(PvpBalanceFeaturePlugIn))]
[Guid("01367b12-a0f8-4a6b-a36f-572936da60c1")]
public sealed class PvpBalanceFeaturePlugIn : IFeaturePlugIn, IDisabledByDefault, ISupportCustomConfiguration<PvpBalanceSettings>, ISupportDefaultCustomConfiguration
{
    /// <summary>Gets the localized feature name.</summary>
    public static string Name => PlugInResources.ResourceManager.GetString("PvpBalance_Name", CultureInfo.CurrentUICulture) ?? string.Empty;

    /// <summary>Gets the localized feature description.</summary>
    public static string Description => PlugInResources.ResourceManager.GetString("PvpBalance_Description", CultureInfo.CurrentUICulture) ?? string.Empty;

    /// <inheritdoc />
    public PvpBalanceSettings? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new PvpBalanceSettings();

    /// <summary>Gets the active settings for a player's character class family.</summary>
    /// <param name="player">The player whose class family determines the rates.</param>
    /// <returns>The settings for the class family, if configured.</returns>
    internal static PvpClassRate? GetClassRate(Player player)
    {
        if (player.GameContext.FeaturePlugIns.GetPlugIn<PvpBalanceFeaturePlugIn>()?.Configuration?.ClassRates is not { Count: > 0 } rates
            || CharacterClassFamily.Of(player) is not { } family)
        {
            return null;
        }

        foreach (var rate in rates)
        {
            if (rate?.BaseClassNumber == family.Number)
            {
                return rate;
            }
        }

        return null;
    }
}
