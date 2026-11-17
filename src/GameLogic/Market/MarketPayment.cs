// <copyright file="MarketPayment.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Market;

using MUnique.OpenMU.Persistence;

/// <summary>
/// The buyer's payment of a listing: zen and WCoinC are taken from the balance, jewels are taken
/// from the inventory and deleted. Refunds everything when the purchase isn't saved.
/// </summary>
internal sealed class MarketPayment
{
    private readonly Player _buyer;
    private readonly int _type;
    private readonly int _amount;
    private readonly Item[] _jewels;
    private readonly List<(Item Jewel, byte Slot)> _taken = [];
    private bool _zenTaken;
    private int? _previousCoins;

    private MarketPayment(Player buyer, int type, int amount, Item[] jewels)
    {
        this._buyer = buyer;
        this._type = type;
        this._amount = amount;
        this._jewels = jewels;
    }

    /// <summary>Creates the payment, if the buyer has the price.</summary>
    /// <param name="buyer">The buyer.</param>
    /// <param name="type">The price type.</param>
    /// <param name="amount">The price.</param>
    /// <returns>The payment, or <c>null</c> if the buyer lacks the price.</returns>
    public static MarketPayment? Create(Player buyer, int type, int amount)
    {
        if (!MarketCurrency.IsJewel(type))
        {
            return MarketCurrency.Balance(buyer, type) >= amount ? new MarketPayment(buyer, type, amount, []) : null;
        }

        var jewels = MarketCurrency.PaymentJewels(buyer, type).Take(amount).ToArray();
        return jewels.Length == amount ? new MarketPayment(buyer, type, amount, jewels) : null;
    }

    /// <summary>Takes the price; the transaction deletes the paid jewels.</summary>
    /// <param name="transaction">The transaction to which the buyer's account and character are attached.</param>
    /// <returns>The completion.</returns>
    /// <exception cref="InvalidOperationException">The balance changed meanwhile.</exception>
    public async ValueTask TakeAsync(IContext transaction)
    {
        switch (this._type)
        {
            case MarketCurrency.Zen:
                this._zenTaken = this._buyer.TryRemoveMoney(this._amount) ? true : throw new InvalidOperationException("The buyer's zen changed.");
                break;
            case MarketCurrency.WCoinC:
                var account = this._buyer.Account!;
                this._previousCoins = account.WCoinC >= this._amount ? account.WCoinC : throw new InvalidOperationException("The buyer's WCoinC changed.");
                account.WCoinC -= this._amount;
                break;
            default:
                foreach (var jewel in this._jewels)
                {
                    var slot = jewel.ItemSlot;
                    await this._buyer.Inventory!.RemoveItemAsync(jewel).ConfigureAwait(false);
                    this._taken.Add((jewel, slot));
                    await transaction.DeleteAsync(jewel).ConfigureAwait(false);
                }

                break;
        }
    }

    /// <summary>Gives back what <see cref="TakeAsync"/> took.</summary>
    /// <returns>The completion.</returns>
    public async ValueTask RefundAsync()
    {
        if (this._zenTaken)
        {
            this._buyer.TryAddMoney(this._amount);
            this._zenTaken = false;
        }

        if (this._previousCoins is { } coins)
        {
            this._buyer.Account!.WCoinC = coins;
            this._previousCoins = null;
        }

        foreach (var (jewel, slot) in this._taken)
        {
            await this._buyer.Inventory!.AddItemAsync(slot, jewel).ConfigureAwait(false);
        }

        this._taken.Clear();
    }

    /// <summary>Stops the buyer's own context from tracking the deleted jewels.</summary>
    public void DetachPaidJewels()
    {
        foreach (var (jewel, _) in this._taken)
        {
            this._buyer.PersistenceContext.Detach(jewel);
        }
    }
}
