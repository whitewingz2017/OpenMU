// <copyright file="GuideBookSettings.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.GuideBook;

using MUnique.OpenMU.DataModel.Composition;

/// <summary>
/// The configuration of the <see cref="GuideBookFeaturePlugIn"/>: the published guide and the draft. Game masters
/// edit and publish the draft in the game; administrators can edit both here, too.
/// </summary>
public sealed class GuideBookSettings
{
    /// <summary>Gets or sets the guide which every player reads.</summary>
    [Display(Name = nameof(GuideBookResources.Published), ResourceType = typeof(GuideBookResources))]
    [MemberOfAggregate]
    public GuideBookContent Published { get; set; } = GuideBookDefaults.Create();

    /// <summary>Gets or sets the guide which game masters edit until they publish it.</summary>
    [Display(Name = nameof(GuideBookResources.Draft), ResourceType = typeof(GuideBookResources))]
    [MemberOfAggregate]
    public GuideBookContent Draft { get; set; } = GuideBookDefaults.Create();

    /// <summary>Determines whether the draft holds saves which aren't published yet.</summary>
    /// <returns><c>true</c>, if the draft differs from the published guide.</returns>
    public bool HasUnpublishedChanges() => this.Draft.Revision != this.Published.Revision;
}
