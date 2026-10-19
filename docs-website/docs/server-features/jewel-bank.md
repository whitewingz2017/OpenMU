# Jewel Bank

The custom MonoGame client can keep jewel counts in an account bank, shared by all
characters on that account. Original Season 6 clients do not have this window.

## Use

Press **J** in the desktop client or open **Bank** from the tablet function menu.
Select a jewel, then deposit all matching jewels or withdraw 1, 10, 20, 30, or all
that fits. Withdrawals produce individual jewels. Deposits also accept the
configured Lahap bundles at levels 0, 1 and 2 as 10, 20 and 30 single jewels.
Bundles are transferred whole; a bundle that exceeds the bank limit stays in the
inventory. Equipment slots, personal store items, optioned items and unsupported
jewel levels are excluded.

The list shows bank and inventory counts. Buttons wait for server confirmation.
After a connection timeout, refresh to get the committed balances before sending
another transaction. Closing the window does not cancel an already sent request.

Close NPC, warehouse, trade, player shop, cash shop and mixing interfaces before
transferring. Dead, changing-map and template players cannot transfer. Banking
uses no Zen fee. Auto-pick into the bank is not part of this first version.

## Server setup

Install the updated OpenMU server and client together. Apply the generated
`AddJewelBankBalances` migration through the normal OpenMU database upgrade
procedure before running the updated server. It adds one non-null text column on
`data.Account`; existing accounts start with an empty ledger. Back up the database
before upgrading. A running older server does not support the new requests.

The **Jewel Bank** packet handler exposes a custom configuration in the plugin
settings: `Enabled`, `MaximumBalance` (default 1,000,000 per jewel), and `Items`.
Identifiers use `group * 512 + number`. Default entries are Bless, Soul, Chaos,
Life, Creation, Guardian and Harmony. Only configured 1-by-1 single jewels at
level zero are accepted; bundle definitions come from `GameConfiguration.JewelMixes`.
The **Jewel Bank View** plugin sends results only to clients which request them.

Account balances and inventory changes are saved through the same player context
transaction, serialized against normal inventory actions and periodic saves.
The server checks balances, limits, free slots, item definitions and current
player state. Request identifiers prevent replaying the same request within a
session. Failed saves restore the tentative ledger and inventory changes.

## Custom protocol

`JewelBankRequestExtended` is C1 FA 20, length 15: operation byte at 4, unsigned
little-endian item identifier at 5, amount at 7 and request identifier at 11.
Operations are 0 refresh, 1 deposit all and 2 withdraw (amount 0 means all that fits).
Request identifiers are positive and strictly increasing for the connection.

`JewelBankResponseExtended` is C2 FA 19 with UTF-8 JSON starting at byte 5:
`RequestId`, `Status`, and `Rows`. Each row has `Item`, `Name`, `Balance`,
`Inventory` and `Limit`. The client accepts only its pending request's snapshot.
This is OpenMU's custom-client protocol; it does not reuse Pegasus packet layouts.
