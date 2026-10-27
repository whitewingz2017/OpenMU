# Personal-store currencies

Extended clients (106.3+) can select Zen, inventory jewels, WCoin C, WCoin P or Goblin Points in the item price dialog. The Personal Store Currencies plugin advertises allowed prices and validates them on the server. Older clients see only Zen-priced listings.

Jewel prices transfer plain, level-zero single jewels from the buyer inventory to the seller inventory. Jewel Bank balances are not used. Both inventories must have space after payment. Bundles, upgraded jewels, timed items and priced store items cannot pay. Coin prices transfer the selected account coin balance; Zen is not charged.

## Deployment

Deploy the updated server with its generated `PersonalStoreCurrencies` migration, compiled models and configuration loader. Apply pending migrations through the normal OpenMU update process before starting game servers. This change adds `data.Item.StorePriceItemType`, defaulting to zero so existing prices stay Zen. No database is updated by generating or compiling these files.

Enable **Personal Store Currencies** in the plugin settings. `Jewels` contains item IDs (`group * 512 + number`); the defaults are Bless, Soul, Chaos, Life, Creation, Guardian and Harmony. `AllowCashShopCoins` controls the three account currencies. Reconnect the updated client after changing settings to refresh its price menu. Limits are 65,535 single jewels per item and a signed 32-bit amount for account coins; the client input accepts up to eight digits for coins.

Successful purchases and their payments save in one persistence operation while both players are locked. A rejected payment or failed save restores the original items and balances. A busy seller can cause a retryable unavailable response.

The Jewel Bank ledger rows come from the separate Jewel Bank plugin's `Items` configuration. It can include Gemstone (`7209`), Lower refining stone (`7211`) and Higher refining stone (`7212`) when these definitions exist in the game configuration. Tap a ledger row to deposit or withdraw.
