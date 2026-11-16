// <copyright file="GuideBookCategory.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.GuideBook;

/// <summary>
/// A category of the Guide Book, which groups its pages in the client's sidebar.
/// </summary>
public sealed class GuideBookCategory
{
    /// <summary>Gets or sets the identifier which the pages of the category refer to, e.g. "cat-start".</summary>
    [Display(Name = nameof(GuideBookResources.Id), ResourceType = typeof(GuideBookResources))]
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the name which the client shows in its sidebar.</summary>
    [Display(Name = nameof(GuideBookResources.CategoryName), ResourceType = typeof(GuideBookResources))]
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc />
    public override string ToString() => this.Name;
}
