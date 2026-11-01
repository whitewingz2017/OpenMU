// <copyright file="StartingAccountCoins.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.CashShop;

using MUnique.OpenMU.PlugIns;

/// <summary>Initializes currency only as part of registering a new account.</summary>
public static class StartingAccountCoins
{
    /// <summary>Validates starting balances before a new account is created.</summary>
    /// <param name="configuration">The current game configuration.</param>
    /// <returns>The WCoin C and WCoin P starting balances.</returns>
    public static (int WCoinC, int WCoinP) GetBalances(GameConfiguration configuration)
    {
        var plugin = configuration.PlugInConfigurations.SingleOrDefault(p => p.IsActive && p.TypeId == typeof(CashShopFeaturePlugIn).GUID);
        var settings = plugin?.GetConfiguration<CashShopSettings>(null);
        if (settings is null)
        {
            return (0, 0);
        }

        if (settings.StartingWCoinC < 0 || settings.StartingWCoinP < 0)
        {
            throw new InvalidOperationException("Starting WCoins cannot be negative.");
        }

        return (settings.StartingWCoinC, settings.StartingWCoinP);
    }
}
