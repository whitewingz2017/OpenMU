---
title: Plugins
sidebar_position: 8
description: Activate, deactivate and configure the plugins which implement the game features.
---

# Plugins

**Navigation:** *Configuration → Plugins* — route `/plugins`

Large parts of OpenMU are implemented as plugins: packet handlers, view plugins,
chat commands, periodic tasks, item crafting, quest logic, and much more. Each
plugin implements an **extension point** (a plugin interface), and the server
picks the plugins which are active for the client version it is serving.

This page lists every plugin the server knows and lets you turn it on and off.

## The list

| Column | Meaning |
|---|---|
| Extension point | The interface this plugin implements. Hover for its description. |
| Plugin name / Help | The display name. Hover over the name or expand Help to read the description. |
| Plugin type | The .NET type. Hover for its type id (a GUID). |
| Action | Activate/Deactivate and, if the plugin has one, its configuration |

Above the list you can filter by extension point, by name or description, and by
type. The name search highlights matching words in the expanded help text.

## Activating and deactivating

**Deactivate** disables a plugin, **Activate** enables it again. This is how you

* switch off a feature you do not want on your server,
* replace a built-in behaviour by your own plugin: deactivate the original,
  activate your extended or modified version.

:::warning[Deactivate carefully]
Many plugins are not optional features but the implementation of a game
mechanic — a packet handler or a view plugin. Deactivating one can make the
client behave strangely or disconnect. If in doubt, note the type name before you
change anything, so you can turn it back on.
:::

## Plugin configuration

Plugins with their own configuration have a ⚙ button which opens it. The
configuration is stored per plugin and, where applicable, per client version, so
the same plugin can behave differently for different clients.

The custom plugin configuration can also contain a *custom plugin source*, which
is how a plugin can be adapted without rebuilding the server.

## Periodic event server scope

Periodic tasks — invasions, Happy Hour, Blood Castle, Devil Square and the
like — run on every game server by default, each with its own copy of the
event. Their configuration has an **Active on servers** list: leave it empty
to keep the default, or tick selected servers to run the event only there.
Restricting an open-world event to selected servers keeps it contested —
players can't clear it on one server and farm it again on another, and a
no-PvP server doesn't become a risk-free farm.

A few semantics worth knowing:

* A forced start (game master command or admin action) starts the event on the
  server where it was requested — every game server runs its own copy, so a
  force never crosses servers. A game master on an excluded server only gets a
  note that the event isn't available there; nothing starts.
* Removing a server from the list stops new runs there, but a running event
  always finishes and cleans up. Open mini-game lobbies stay joinable until
  they end.
* An explicit selection pins today's servers: servers added later don't run
  the event until ticked. Leave the list empty to include future servers.
  Make sure the event server has headroom (`MaximumPlayers`) for players
  coming over from other servers.

## When do changes take effect?

Activation state is stored in the configuration. Depending on the extension
point, the change is picked up by the running server or requires a reload — use
**Reload configuration and restart all game servers** on the
[Servers page](servers.md) if you don't see an effect.

## Feature plugins

Some plugins are not a single mechanic but a whole feature with its own
configuration, grouped in the *Feature Plugins* extension point. They are
documented separately:

* [Server-side AI bots](../server-features/bots.md)

## Game event publisher

The *Game Event Publisher* plugin publishes notable events of the game to the
whole server, so that external systems can react on them, for example a Discord
integration (see [Discord](../server-features/discord.md)). It's
disabled by default; activate it when such a system is connected.

It publishes:

* the opening, start and end of mini games, like Blood Castle, with the winner,
* the start and end of invasions, with the invaded maps,
* the state changes of the castle siege, with the owner of the castle,
* excellent and ancient items which are dropped by monsters,
* who picked up a jewel or a Resonance Core which a monster dropped, with the
  monster, the coordinates where it lay and the time,
* the kills of boss monsters,
* characters which reach a level or master level milestone,
* global notices which game masters send with the `!` chat prefix.

Each kind of event can be switched off in the plugin configuration. There you
also define which monsters are bosses, by their monster numbers (e.g. 275 for
Kundun), and which levels and master levels are milestones (by default, level
400).
**Announced pickup items** lists the items whose pickup is published, each as
group × 512 + number (e.g. 7181 for the Jewel of Bless, 14 × 512 + 13). By
default these are the jewels of Bless, Soul, Chaos, Life, Creation, Guardian and
Harmony, and the Resonance Core (7420). Only items which a monster dropped are
announced, not items which players dropped.

## Discord integration of the client

The feature plugin *Discord integration of the client* tells game clients which
ask for it how the server is connected to Discord: the invite link, the Discord
application of the Rich Presence, the link of the account and which chats are
mirrored. It also lets players link their account from the client, and sends
them the messages written in Discord as such. See
[Discord](../server-features/discord.md#the-game-client).

## Starting items by class

Open **Game configuration → Starting items by class**. Enable the profile for the
desired class and click **Create** to add a starter item. The item editor uses
the same searchable **Definition**, named **Excellent Options**, **Luck**, and
additional-option selectors as the inventory/vault editor. Select the item by
name, choose its level and supported options, and set **Quantity** (separate items).
Options which the selected item does not support are hidden. Changing the item
clears the previous item's level and options. Existing numeric settings reopen
with their item and named options selected.

For a Dragon set +9 +Luck +16 with DSR, Reflect Damage, and Damage Decrease,
create one row for each Dragon set piece. Select **Level 9**, enable **Luck**,
select **Defense rate**, **Reflect damage**, and **Damage decrease** under
**Excellent Options**, and select the **+16 defense** additional-option level.
Add your sword and **Small Wings of Satan** as separate rows. Small wings have
no Luck or excellent options in the default item definitions. Bag placement
and durability are automatic; these controls do not appear in the starter editor.
Ancient, Harmony, and socket options are not part of the starter policy.

Under **Buffs**, click **Add** to give the class a buff which is active from the
creation of the character, like a **Seal of Wealth** or a **Scroll of
Protection**. Select the buff and its **Duration**: 1 hour, 3 hours, 5 hours,
1 day, 3 days or 7 days. The duration counts from the creation of the character,
also while it is offline, like the same buff bought in the cash shop. Buffs which
would replace each other, like two seals, prevent saving.

**Replace legacy items** replaces that class's previous starter-item grants;
turn it off to add items alongside them. Disabled profiles preserve the old grants.
An enabled profile with replacement on and no items intentionally grants none.
Use **Submit** to save, activate the plugin in the filtered list behind the dialog,
and restart game servers before creating a character. Existing characters are
unaffected. All items must fit in the initial bag, with a maximum of 64 items per
class. Missing items, duplicate classes, and unsupported options prevent saving.
**Cancel** discards the dialog's changes without creating inventory objects.
Cleared class, item, or excellent-option lists in older saved settings reopen safely.
Missing class lists show the default disabled profiles; changes are saved only with **Submit**.

The **Starting items by class** and **Monster Zen drop amounts** menu entries
open their configuration dialogs directly.

## Starting WCoins

Open **Game configuration → Starting WCoins (new accounts)**. This opens the
Cash Shop plugin settings, where **Starting WCoin C** and **Starting WCoin P**
set the balances for newly registered accounts. Keep the Cash Shop plugin active.
Both amounts default to zero, and negative values are rejected.

The registration API (including client test registration) and the admin panel's
account creation form save these balances together with the new account. Existing
accounts are unchanged. Creating or deleting characters grants no WCoins.
This requires the updated AdminPanel; external registration tools which write
directly to the database must initialize the balances themselves.

## Boss kill rewards

Open **Game configuration → Boss kill rewards**; if the plugin is inactive,
click **Activate** there. Each row is one boss: choose it from the monster list
(e.g. *Kundun (275)*), enter the **WCoinC**, **WCoinP** and **Goblin Points**
which the player who kills it gets, and untick **Enabled** to pause a boss
without removing it. **Add boss** adds a row; **Save changes** stores the list,
which requires a boss in every row and each boss only once. Only the killer gets the points, not the party;
the player sees a message with the received points. With **Announce who killed
the boss**, all players see a golden message like *Player has defeated Kundun!*
A boss which isn't listed gives no points and isn't announced.

## Guide Book

The **Guide Book** plugin is the in-game guide of the custom client (F1 or the
book beside the MU Helper's start button). It's active by default and starts
with pages about the server's features. Two copies are stored in its
configuration: the **Published guide**, which every player reads, and the
**Draft of the game masters**.

Game master characters edit the draft in the game: **Switch to GM/Admin
Editor**, change pages and categories, **Save Changes** (stores the draft in the
database) and **Publish** (copies the draft to the published guide and sends it
to every player online on that game server; players of the other game servers
get it when they open the book next). Other characters can't load, save or
publish the draft. A save is refused when another game master saved the draft
in the meantime; **Discard** loads the newer draft.

The pages quote live settings, so they follow when you change them:
`{exc.base}`, `{exc.bonus}`, `{exc.max}` and `{exc.zen}` of **Excellent
Combination**, `{core.boss}` and `{core.monster}` of the **Resonance Core drop**,
and `{{boss-rewards}}` shows the bosses of **Boss kill rewards** and the
Resonance Core drop, as far as these plugins are active.

You can also edit both copies on the plugin's **Configure** page. The client
refuses a guide beyond 32 categories, 256 pages, 8000 bytes of text per page or
52000 bytes in total, and every page needs a category.

## Skill buff durations

Open **Game configuration → Skill buff durations** to change how long timed
effects last, e.g. the buff skills Infinite Arrow, Greater Damage and Greater
Defense, or potions and scrolls. **Duration (seconds)** is the base duration.
Some effects also get longer with a stat of the caster, e.g. energy, which the
**Also depends on** column shows; effects with a separate PvP duration show
it in its own column. Save and restart the game servers to apply the changes.

## Skin shop prices in Goblin Points

On **Game configuration → Skin Shop**, each skin has a **Goblin Points price**
next to its WCoinC price. With a Goblin Points price, players choose in the
purchase window whether they pay with WCoinC or Goblin Points; zero sells the
skin for WCoinC only. **Goblin Points defaults for new skins** are the prices
of skins which aren't in the list yet.

## Party reconnect timeout

The **Party Auto-Rejoin** plugin reserves a disconnected character's party slot for
**15 minutes** by default. Reconnecting within this window restores the character
to the same party. After the timeout, the member is removed and the remaining
players receive the updated party list. A two-member party disbands when its
disconnected member expires. Intentional logout removes the character immediately.

Keep this plugin active and use **Reconnect timeout** to adjust the window.
Offline leveling characters which remain active in the world are unaffected.

## Party Search

The **Party Search** plugin is the party list of the custom client (**Party
Search** in the window menu). It's active by default. A party master, or a
player without a party, lists the party with a minimum level, the wanted
classes, **Guild only**, **One per class** and an optional password of up to 10
letters and digits. Other players see the leader, the map and position, the
level, the members and the classes the party still looks for, and join with
**Join** without the leader having to accept. With **One per class**, the
classes already in the party aren't wanted anymore.

A party with a password asks for it on **Join**. The **Party Search: join by
password** plugin also lets players join by sending the password to the
leader: as a whisper, or as `/<leader> <password>` in the chat, e.g.
`/ken 1122`. The leader doesn't get the password as a whisper. A wrong password
makes the player wait three seconds before the next try.

The listings are kept in memory per game server, at most 100 at once. A listing
ends when its leader removes it, leaves the lead of the party or leaves the
game; a full party stays listed but can't be joined. Events which don't allow
parties, and other party plugins (e.g. the gens rules), still apply.

## Marketplace

The **Marketplace** plugin is the trading hall of the custom client
(**Marketplace** in the window menu). It's active by default. A player lists an
inventory item for a price in zen, WCoinC or a jewel; the item leaves the
inventory and stays on the market until it's sold, cancelled or expired. Other
players browse by category (weapons, armor, wings, jewels, materials), search
by name, filter by currency, sort by price, and buy with **Purchase Item**.

Everything goes by mail from the sender **Market**:

- the buyer gets the item, to claim from the letter;
- the seller gets the price after the tax, to claim from the letter: zen up to
  the inventory limit, WCoinC, or the jewels as single jewels up to the free
  inventory space. What doesn't fit stays in the letter until there's room;
- a cancelled or expired listing returns the item to the seller.

A letter can't be deleted before its item or currency is claimed. Buyers pay
jewel prices with plain single jewels of their inventory (level 0, no options,
not bundled), at most 250.

Settings on **Configure**:

| Setting | Default | |
|---|---|---|
| Allow Zen prices / Zen tax (%) | on / 10 | |
| Allow WCoinC prices / WCoinC tax (%) | on / 10 | |
| Jewel currencies | Bless, Soul, Chaos, Life, Creation | item numbers as group * 512 + number |
| Jewel tax (%) | 10 | taken in jewels: 10% of 10 Jewels of Bless is 1 |
| Tax rounding | Nearest | for a tax which isn't whole: 10% of 5 jewels is 0.5, Nearest and Up take 1, Down 0 |
| Listing duration (hours) | 48 | |
| Listings per character | 10 | |
| Minimum character level | 1 | to sell and to buy |
| History (days) | 14 | ended listings stay in the history, then they're deleted |

The listings are stored in the database and shared by the game servers.
Expired listings are returned about once a minute. Only items which may be
sold in a personal store and traded can be listed; worn items, items with a
time limit and items priced for the personal store can't.

## Writing your own plugin

Implementing a plugin is documented next to the code, in the
[MUnique.OpenMU.PlugIns readme](https://github.com/MUnique/OpenMU/blob/master/src/PlugIns/Readme.md).
