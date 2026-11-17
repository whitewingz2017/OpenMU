// <copyright file="OfflineLevelingAnswer.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Offline;

/// <summary>The offline leveling window's offer: the currencies with their prices, and the result of a request.</summary>
public sealed class OfflineLevelingAnswer
{
    /// <summary>Gets or sets the request identifier; 0 when /offlevel opens the window.</summary>
    public uint RequestId { get; set; }

    /// <summary>Gets or sets the result.</summary>
    public OfflineLevelingResult Result { get; set; }

    /// <summary>Gets or sets a value indicating whether the client opens the window (/offlevel).</summary>
    public bool Open { get; set; }

    /// <summary>Gets or sets the allowed currencies.</summary>
    public IReadOnlyList<OfflineLevelingCurrencyOffer> Currencies { get; set; } = [];

    /// <summary>Gets or sets a value indicating whether the protection may be bought.</summary>
    public bool AllowProtection { get; set; }

    /// <summary>Gets or sets the most hours a player may pay in advance.</summary>
    public int MaximumHours { get; set; }

    /// <summary>Gets or sets a value indicating whether zen costs the MU Helper's level-based zen.</summary>
    public bool LevelBasedZen { get; set; }

    /// <summary>Gets or sets the level-based zen at the start of a session.</summary>
    public long ZenStart { get; set; }

    /// <summary>Gets or sets the level-based zen of 1 to <see cref="MaximumHours"/> prepaid hours, without the start.</summary>
    public IReadOnlyList<long> ZenByHours { get; set; } = [];
}
