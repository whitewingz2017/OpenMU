// <copyright file="JewelBankLedger.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence;

using System.Text.Json;

/// <summary>Reads bank balances for reporting without changing the player's ledger.</summary>
public static class JewelBankLedger
{
    /// <summary>Reads nonnegative balances of the requested jewel types.</summary>
    /// <param name="json">The stored ledger.</param>
    /// <param name="itemCodes">The jewel types.</param>
    /// <returns>The available balances.</returns>
    public static IReadOnlyList<JewelAmount> Read(string json, int[] itemCodes)
    {
        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return [];
            }

            return itemCodes.Select(code => new JewelAmount(code,
                document.RootElement.TryGetProperty($"{code}:0", out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var amount)
                    ? Math.Max(0, amount) : 0)).ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
