// <copyright file="RankTitleOffer.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.RankTitles;

/// <summary>A permanent character title and its WCoinC price.</summary>
public sealed class RankTitleOffer
{
    /// <summary>Gets or sets the rank identifier defined by the client catalog.</summary>
    [Range(1, ushort.MaxValue)]
    public int Rank { get; set; }

    /// <summary>Gets or sets the title name used by the shop.</summary>
    [Required]
    [StringLength(RankTitleFeaturePlugIn.MaximumTitleNameLength)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the WCoinC price. Zero prevents purchases.</summary>
    [Display(Name = nameof(RankTitleResources.Price), ResourceType = typeof(RankTitleResources))]
    [Range(0, int.MaxValue)]
    public int Price { get; set; } = 100;
}
