// <copyright file="GuideBookPage.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.GuideBook;

/// <summary>
/// A page of the Guide Book.
/// </summary>
/// <remarks>
/// The client draws the <see cref="Body"/> from a small markup of its own, see the Guide Book page of the MuMain
/// documentation: e.g. "## Heading", "- bullet", "**bold**", "[[page-id|link]]", "{exc.base}" for a value of
/// <see cref="GuideBookValues"/>, "![Interface/....tga]" for an image of the client and "{{boss-rewards}}" for
/// the table of <see cref="GuideBookValues.BossRewards"/>.
/// </remarks>
public sealed class GuideBookPage
{
    /// <summary>Gets or sets the identifier which links of other pages refer to, e.g. "page-welcome".</summary>
    [Display(Name = nameof(GuideBookResources.Id), ResourceType = typeof(GuideBookResources))]
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the identifier of the <see cref="GuideBookCategory"/> of the page.</summary>
    [Display(Name = nameof(GuideBookResources.CategoryId), ResourceType = typeof(GuideBookResources))]
    public string CategoryId { get; set; } = string.Empty;

    /// <summary>Gets or sets the title.</summary>
    [Display(Name = nameof(GuideBookResources.Title), ResourceType = typeof(GuideBookResources))]
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the text of the page, in the client's markup.</summary>
    [Display(Name = nameof(GuideBookResources.Body), ResourceType = typeof(GuideBookResources))]
    [DataType(DataType.MultilineText)]
    public string Body { get; set; } = string.Empty;

    /// <inheritdoc />
    public override string ToString() => this.Title;
}
