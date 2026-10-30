---
title: Cash shop
sidebar_label: Cash shop
sidebar_position: 4
description: The in-game cash shop of Season 6, in which players spend WCoin and Goblin Points.
---

# Cash shop

The cash shop (in-game shop, key `X`) of the Season 6 client offers items for
**WCoin (C)**, **WCoin (P)** and **Goblin Points**. Every account has its own
balance of these three coins, and a storage which holds the bought and the
gifted items until the player uses them.

:::note
Not implemented yet: the event item list and items which expire.
:::

## Settings

The plugin **Cash shop** on the [Plugins](../admin-panel/plugins.md) page switches
the cash shop on and off — while it's deactivated, the shop can't be opened — and
holds its settings:

| Setting | Default | Meaning |
|---|---|---|
| Is Gifting Enabled | yes | Players can send packages as gift. |
| Can Gift To Own Account | yes | Players can send gifts to the characters of their own account. |
| Maximum Storage Items | 0 | Maximum number of items in the storage of an account, including gifts; 0 means unlimited. A purchase or gift which doesn't fit is refused. |

## Opening the shop

A player can open the cash shop when the character stands in a safezone, and
doesn't trade or talk to an npc at the same time.

## Coins

The balances are properties of the account. Coins are given by grants, which a
payment provider or a website creates through the
[web API](../admin-panel/authentication.md#granting-cash-shop-coins). The game
server applies a grant the next time the player opens the cash shop.

The plugin **Goblin Points for play time** (deactivated by default) gives players
Goblin Points each time they played for a configured interval. It's configured on
the Plugins page: the interval (default 1 hour), the points (default 10), the
minimum character level, and whether characters which level offline get them,
too.

## Product catalog

The game client doesn't get the products from the server. It loads them from
script files in its `Data\InGameShopScript` folder, and the banner from
`Data\InGameShopBanner`. The server only tells the client which version of the
script and banner to load, when the character enters the game.

The server needs its own copy of the catalog, because it decides about the
prices and the items which a player gets. It's part of the game configuration
as *Cash Shop Configuration*:

* the script version (sale zone, year and id within the year) and the banner
  version, which the server sends to the client;
* the packages, with the coin type, the price and whether they're for sale or
  can be sent as gift;
* the products of each package — either price options of which the player buys
  one, or the contents of a bundle — with the item, its level, the quantity and
  the duration.

The packages and products are identified by the sequence numbers of the client
script. If you change the catalog, change the script files of your clients the
same way, and give them a new version.

A new Season 6 database contains the catalog of the script `512.2012.084`,
which comes with the game client. Existing databases get it with the optional
configuration update *Add cash shop*.

Some packages can't be sold, and the server refuses them even if they're marked
for sale:

* packages with items which aren't defined in the game configuration — most
  special cash shop items, like seals, scrolls and talismans;
* timed application-only products without an implemented effect delivery plugin;
* packages with a [special item](#special-items) whose plugin is deactivated.

### Editing the catalog

**Game configuration → Cash shop** in the admin panel (administrators only)
edits the catalog:

* The list shows every package with its coin type, price and flags. The column
  *Sellable* shows whether a player can buy it, or why not: a product without
  item, an unsupported timed effect, no products, or a number used twice.
* **Edit** opens a package: number, name, coin type, bundle price, the flags,
  and its products with their numbers, price, item, item level, quantity and
  duration.

Changes take effect when you save. The page doesn't change what the game client
shows: a package you put on sale must exist in the client script, and a changed
price is charged, but the client still shows the price of its script.

## Buying

A purchase takes the price from the coin balance of the package's coin type and
adds the products to the storage of the account. A bundle package adds all of
its products for the price of the package; the other packages are price options
(for example 1, 3, 7 or 30 days) of which the player buys one.

## Gifts

A player can buy a package as gift for any character, also of an offline player.
The sender pays, and the products go to the gift storage of the recipient's
account, together with the name of the sender and the message. Only packages
which are marked as giftable can be sent; in the catalog of the game client
these are the Goblin Point packages and some WCoin (P) packages.

## Using items

A bought or gifted item waits in the storage of the account until the player
uses it with any character of the account. Using it puts its items into the
inventory, with the item level and quantity of the product. If the inventory
doesn't have space for all of them, nothing is added and the item stays in the
storage.

### Special items

Some items aren't put into the inventory. A plugin applies them when the player
uses them from the storage, and each of them can be deactivated and configured
on the **Plugins** page:

| Plugin | Item | Effect |
|---|---|---|
| Cash shop: Vault Expansion Certificate | Vault Expansion Certificate | extends the vault of the account |
| Cash shop: Magic Backpack | Magic Backpack | adds an inventory extension to the character, up to *Maximum Extensions* (at most 4) |
| Cash shop: Summoner Character Card | Summoner Character Card | lets the account create Summoners |
| Cash shop: Rage Fighter Character Card | Rage Fighter Character Card | lets the account create Rage Fighters |

An item can't be used when its effect is already applied, for example when the
vault is already extended; it stays in the storage then. A new inventory
extension can be used after selecting the character again.

A Magic Backpack which is already held in the character's inventory can also
be consumed there. The inventory consume plugin grants one extension and consumes
one backpack only when the character is below its configured maximum (at most
four). At the limit, the backpack is retained. Select the character again to use
the newly granted space, just as after cash-shop delivery.

The plugin *UnlockSummonerAtLevel1* unlocks the Summoner for every account,
which makes the Summoner Character Card useless. Deactivate that plugin if you
sell the card.

While the plugin of a special item is deactivated, its packages can't be bought,
and bought items stay in the storage until the plugin is active again.

### Timed equipment

Supported timed pets and other wearable items start their duration when collected
from shop storage. Their UTC deadline persists through logout, restart and vault
storage. Expired equipment loses its bonuses and is removed by the server.
Temporary items cannot be dropped, traded or used in crafting; inventory and vault
moves preserve the original deadline.

Upgrading an existing server requires the `AddItemExpiration` database migration
and the optional **Add cash shop** configuration update, version 6. Timed buffs
also require the `AddPurchasedMagicEffects` database migration. The update
adds missing catalog definitions, repairs product links and adds expiration plugins.
Keep **Item expiration** enabled; disabling it blocks timed purchases and
collection.


### Repairing existing product links

Version 6 of **Add cash shop** repairs missing product-to-item references for
script `512.2012.084`, using the same catalog records as fresh initialization.
It enables repaired packages only when every product is deliverable. Existing
prices, durations and explicitly assigned items remain unchanged. Talisman rate
quantities are corrected to match the script's 5% and 10% choices.
Run this optional configuration update after deploying the updated server.
Rebuilding the game client alone does not update the server catalog.

Version 5 also registers the new item-drop-rate attribute before creating buff
bonuses. This repairs the version 4 upgrade failure **Sequence contains no matching
element** in `StatsExtensions.GetPersistent`. Apply version 5 after deploying the
corrected server, including if version 4 is already listed as installed.

Version 6 also fills empty bonus definitions when an item already has an effect
link. Existing nonempty effect configurations are retained. Rejected purchases
now log the package sale flag, item link, effect bonus count and expiration-plugin
state, so operators can distinguish delivery failures in the running server.

A rejection can also mean that the package is not for sale, expiration enforcement
is disabled, an item definition is missing, or its delivery/effect plugin is not
implemented or active. The admin cash shop catalog lists definition and duration
issues. In particular, **Product has no item** means the persisted product link
is missing; checking **For sale** alone cannot repair it. Deploy the updated
server, apply schema migrations on **Setup**, apply **Add cash shop v6** on
**Updates** (`/config-updates`), then restart the server. The update repairs all
four Seal of Wealth price options, not just the selected duration.

Configured seals and scrolls activate directly from shop storage. Their bonuses
persist through logout and server restart until the purchased UTC deadline.
Conflicting effects remain in storage instead of being consumed. Gold Channel
access and Premium Service are unavailable until their server access rules and
benefits are defined.

Period-item deadlines now retain their UTC instant when a persistence provider returns
a local timestamp. A 24-hour purchase must not gain the server time-zone offset
after the item is loaded again. Existing incorrectly saved deadlines are not rewritten.

Seal buffs publish their configured remaining duration to the owning client, including
experience, master-experience and item-drop bonuses. If a client shows the Seal of
Wealth name but no countdown, update and restart the game server as well as the
client: older servers sent these effects only as untimed status updates. The
countdown uses the restored remaining duration, rather than restarting the
purchased period at login.

Owner countdowns now apply to every visible timed magic effect, including NPC and
skill buffs, even when its legacy `SendDuration` flag is disabled or its bonus
attribute has no item-specific packet mapping. Refreshing an active buff resets
the displayed countdown along with the server timer. Untimed effects keep their
status-only notification; duration packets are sent only to the affected player.

Timed pets retain their displayed HP and do not lose it from combat damage.
They remain available until their purchased UTC expiration; ordinary pets without
an expiration still take durability damage and can die. This rule applies to all
period pets, including Panda, rather than a list of pet species.

## Catalog recovery when opening

The server sends the configured catalog and banner versions again before an allowed
Cash Shop open response. This lets the mobile client recover if it missed the
login-time version announcement, without choosing a different catalog locally.
Opening still requires the Cash Shop feature and catalog to be configured and the
player to be alive in a safe zone, outside other inventory operations.

If an open request is denied, the game server logs `Cash Shop open denied` at
Warning level with `catalogConfigured`, `featureEnabled`, `state`, `alive`, and
`safezone`. The first two must be true, state must be `EnteredWorld`, and the last
two must be true. In the admin panel, open **Log Files**, select the latest game
server log, and search for that message immediately after trying SHOP. The
`Client version changed` warning is a packet-handler refresh and does not identify
an access denial. Rebuild, deploy, and restart the server to enable this diagnostic;
no APK change is needed.
