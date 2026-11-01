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
* the kills of boss monsters,
* characters which reach a level or master level milestone,
* global notices which game masters send with the `!` chat prefix.

Each kind of event can be switched off in the plugin configuration. There you
also define which monsters are bosses, by their monster numbers (e.g. 275 for
Kundun), and which levels and master levels are milestones (by default, level
400).

## Discord integration of the client

The feature plugin *Discord integration of the client* tells game clients which
ask for it how the server is connected to Discord: the invite link, the Discord
application of the Rich Presence, the link of the account and which chats are
mirrored. It also lets players link their account from the client, and sends
them the messages written in Discord as such. See
[Discord](../server-features/discord.md#the-game-client).

## Starting items by class

Open **Game configuration → Starting items by class** and use the plugin's
configuration button. **Classes** shows seven editable profiles. Enable the
profile for the desired class, then use **Create** under its **Items** list to
add rows with **Group**, **Number**, **Level**, **Quantity**, **Luck**, **Skill**,
**Additional option level**, and **Excellent option numbers**. Look up item
identifiers in **Game configuration → Items**. Quantity counts separate items.

**ReplaceLegacyItems** replaces that class's previous starter-item grants; turn
it off to add items alongside them. Disabled profiles preserve the old grants.
An enabled profile with replacement on and no items intentionally grants none.
Use **Submit** to save, activate the plugin, and restart game servers before
creating a character. Existing characters are unaffected.

For a Dragon set +9 +Luck +16 with DSR, Reflect Damage, and Damage Decrease,
create one row per set piece: groups **7, 8, 9, 10, 11**, number **1**, level
**9**, quantity **1**, Luck enabled, Additional option level **4**, and Excellent
option numbers **2;3;4**. Keep the Dark Knight profile enabled. Add the desired
sword and small wings as separate rows using their Group/Number from **Items**.
Items are placed in the bag, not equipped. Small Wings of Satan for a Dark
Knight are **Group 12, Number 134**; leave Luck, Skill, additional option level,
and excellent numbers off/empty because small wings have no such options in
the default configuration.

Excellent numbers are separated by **semicolons**. Their meanings depend on the
item: armor **2 = DSR**, **3 = Reflect Damage**, **4 = Damage Decrease**; weapons
**1 = mana after kill**, **2 = health after kill**, **3 = attack speed**,
**4 = damage increase**, **5 = damage per level**, **6 = excellent damage chance**.
Check the item's possible options for wings and other equipment; armor options
do not apply to swords or wings. Unsupported options reject the complete starter
grant. Additional option level ranges from **0 to 7**: for damage and defense,
levels 1–7 mean +4 through +28, so **4 means +16**. Recovery options use their
item-defined values. Excellent weapons receive their skill when supported.

The **Starting items by class** and **Monster Zen drop amounts** menu entries
open their configuration dialogs directly. The plugin must be active for its
settings to take effect; the filtered list behind the dialog has its activation
button.

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

## Writing your own plugin

Implementing a plugin is documented next to the code, in the
[MUnique.OpenMU.PlugIns readme](https://github.com/MUnique/OpenMU/blob/master/src/PlugIns/Readme.md).
