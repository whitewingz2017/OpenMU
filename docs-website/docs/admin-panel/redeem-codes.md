# Redeem codes

Players open **MENU → Redeem Code**, enter a code, and tap **Redeem**. The result
appears in system messages. Codes accept 4–32 ASCII letters, digits or hyphens;
the server matches them without regard to case.

## Server setup

Deploy the updated OpenMU game server and admin panel from `C:\OpenMU\OpenMU`.
Apply its `20261028090657_RedeemCodes` database migration using your normal
database-upgrade process before enabling the feature. No live database changes
were applied while implementing it.

Confirm the **Redeem Codes** packet-handler plugin is enabled for the game
configuration. Rebuild the mobile APK to include the new menu entry and request.

## Admin panel

Sign in as an administrator and open **Redeem Codes** (`/redeem-codes`). Create a
code, select its rewards, and enable it when ready. New codes start disabled.

- Item rewards select the item definition, level (0–15), and quantity. These are
  plain items without additional skill/luck/excellent/socket options. A claim
  can deliver up to 64 items, subject to available inventory space.
- Point rewards support WCoin C, WCoin P, and Goblin Points, alone or with items.
- Expiration is entered in UTC; leaving it blank means no expiry.
- Total claims of zero means unlimited; every code still has a once-per-account
  or once-per-character restriction. That scope and the code text are fixed
  after creation. Disable a code instead of deleting its redemption history.
- The page shows the latest 100 claims, including character and account ID.

The server validates rewards and player state, limits repeated attempts, and
saves the rewards, usage count, and receipt together. A failed save, full
inventory, expired code, or duplicate claim grants nothing. A concurrency token
prevents simultaneous claims on different game servers from exceeding the limit.

The client sends `C1 FC 01`, length 36, with a zero-padded 32-byte ASCII code.
It never chooses or supplies reward amounts.
