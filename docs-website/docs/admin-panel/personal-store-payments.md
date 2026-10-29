# Personal-store prices and mail delivery

Hold an item in your closed store for one second to edit its price. Add each
required currency and amount, then Apply. Tap an amount in the price summary to
remove it. All listed currencies are required together: 1 Bless + 2 Soul +
100 WCoin C means the buyer must pay all three, not choose one.

Up to eight distinct currencies can be combined. Available currencies follow the
server's Personal Store Currencies configuration. Jewel payments use plain,
level-zero single jewels from the buyer's inventory, never Jewel Bank balances.

The first tap on another store's item shows its details and full price. The
second tap opens purchase confirmation. After purchase:

- All payment components transfer immediately to the seller. Jewel proceeds
  require enough free seller inventory slots; coins credit the seller's account.
- The exact purchased item goes to the buyer's mail instead of inventory.
  Open the purchase receipt and press Claim when there is enough inventory space.
- The seller receives a receipt containing the buyer, item details, and payment.
  Seller receipts have no Claim button.
- Unclaimed purchase receipts cannot be deleted. Claiming can succeed only once;
  the attachment remains available when inventory is full.

Receipts and attachments persist across reconnects and server restarts. Payment,
removal from the seller's store, and both receipts commit together. Failure rolls
back the sale. Claiming separately commits the attachment removal and inventory
addition together.

Deployment requires the updated server and client and database migration
`20261029082657_StoreMailCombinedPrices`. Do not downgrade the database while mail
attachments are unclaimed. No migration is applied automatically by this source
change. Back up the database before applying it on the VPS.

Updated clients request mail delivery for existing single-currency listings too.
Older clients retain their original delivery flow and cannot pay combined-price
listings. The combined-price editor appears only after the updated server
advertises support. The Personal store mail packet handler must be enabled along
with Personal Store Currencies.
