// <copyright file="RankTitleSettings.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.RankTitles;

/// <summary>Configures the rank titles offered to custom clients.</summary>
public sealed class RankTitleSettings
{
    /// <summary>Gets or sets the catalog, using the eight TwinMU client rank identifiers.</summary>
    public List<RankTitleOffer> Titles { get; set; } =
    [
        new() { Rank = 1, Name = "VIP" },
        new() { Rank = 2, Name = "Fighter" },
        new() { Rank = 3, Name = "Legend" },
        new() { Rank = 4, Name = "TwinMU" },
        new() { Rank = 5, Name = "Slayer" },
        new() { Rank = 6, Name = "Hunter" },
        new() { Rank = 7, Name = "PvP King" },
        new() { Rank = 8, Name = "Top Tier" },
    ];

    /// <summary>Checks uniqueness and bounds, including the size of the catalog response.</summary>
    /// <returns>Whether the catalog can be used safely.</returns>
    public bool IsValid() => this.Titles is { Count: <= RankTitleFeaturePlugIn.MaximumCatalogTitles }
        && this.Titles.All(title => title is { Rank: > 0 and <= ushort.MaxValue, Price: >= 0, Name.Length: > 0 and <= RankTitleFeaturePlugIn.MaximumTitleNameLength })
        && this.Titles.Select(title => title.Rank).Distinct().Count() == this.Titles.Count;
}
