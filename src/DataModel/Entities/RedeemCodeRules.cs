// <copyright file="RedeemCodeRules.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.DataModel.Entities;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

/// <summary>Shared code normalization, stable keys and operator-input validation.</summary>
public static class RedeemCodeRules
{
    /// <summary>Normalizes a code; returns an empty string for invalid input.</summary>
    public static string Normalize(string? text)
    {
        var code = text?.Trim().ToUpperInvariant() ?? string.Empty;
        return code.Length is >= 4 and <= 32 && code.All(c => c is >= 'A' and <= 'Z' or >= '0' and <= '9' or '-') ? code : string.Empty;
    }
    /// <summary>Creates a deterministic database key with a domain separator.</summary>
    public static Guid Key(string value) => new(SHA256.HashData(Encoding.UTF8.GetBytes("OpenMU:Redeem:" + value)).AsSpan(0, 16));
    /// <summary>Validates rewards and returns their parsed item list.</summary>
    public static IReadOnlyList<RedeemItemReward> Rewards(RedeemCode code)
    {
        var items = JsonSerializer.Deserialize<List<RedeemItemReward>>(code.ItemsJson) ?? throw new ArgumentException("Invalid item rewards.");
        if (code.MaximumUses < 0 || code.WCoinC < 0 || code.WCoinP < 0 || code.GoblinPoints < 0 || items.Count > 32
            || items.Any(i => i.ItemCode < 0 || i.ItemCode > 8191 || i.Level > 15 || i.Count < 1 || i.Count > 64)
            || items.Sum(i => (long)i.Count) > 64 || (items.Count == 0 && code.WCoinC == 0 && code.WCoinP == 0 && code.GoblinPoints == 0))
        {
            throw new ArgumentException("Choose positive rewards: up to 64 items, levels 0-15, and nonnegative coins/points and limits.");
        }
        return items;
    }
}
