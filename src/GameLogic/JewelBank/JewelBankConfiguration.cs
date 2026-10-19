// <copyright file="JewelBankConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.JewelBank;

/// <summary>Operator-controlled bank limits and supported single jewels.</summary>
public class JewelBankConfiguration
{
    /// <summary>Gets or sets a value indicating whether bank requests are enabled.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets the maximum balance per jewel.</summary>
    public int MaximumBalance { get; set; } = 1_000_000;

    /// <summary>Gets or sets the supported identifiers (group * 512 + number).</summary>
    public int[] Items { get; set; } = [7181, 7182, 6159, 7184, 7190, 7199, 7210];
}
