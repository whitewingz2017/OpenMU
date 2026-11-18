// <copyright file="FeatureListAnswer.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Features;

/// <summary>Which features of the custom client are active.</summary>
public sealed class FeatureListAnswer
{
    /// <summary>Gets or sets the request identifier.</summary>
    public uint RequestId { get; set; }

    /// <summary>Gets or sets the features: <c>true</c> for an active one, by name.</summary>
    public IReadOnlyDictionary<string, bool> Features { get; set; } = new Dictionary<string, bool>();
}
