---
title: Daily login rewards
sidebar_label: Daily login rewards
description: A calendar month of rewards which every account claims once per day in the custom client.
---

# Daily login rewards

Every day of the month has its own reward. An account claims the reward of the
current day once, in the custom client's **Daily Rewards** window. Original
Season 6 clients don't have this window.

## How it works

- Day *N* of the month grants the reward configured for day *N*.
- Only the current day can be claimed. A day that passes unclaimed is lost.
- The claimed days start over on the first day of each month.
- Rewards are tracked per account: when one character claims the day's reward,
  the account's other characters can't claim it again that day.
- Days follow the server's local time. A new day starts at the server's midnight.
- A reward can combine zen, WCoinC, WCoinP, Goblin Points and one item. Zen and
  the item go to the claiming character; the coins go to the account. The
  claim is saved as a whole: if the item doesn't fit into the inventory, or the
  zen would exceed the carry limit, nothing is granted and the day stays
  claimable.

The window opens by itself when a character enters the game while today's
reward is still unclaimed. Players can also open it from **DAILY** in the
top-right button row (modern theme) or **Daily Rewards** in the window menu.
Today's card is gold, claimed days are green and missed days are red.

## Configuration

The feature is disabled by default. In the admin panel:

1. Open **Configuration > Daily Login Rewards**.
2. Activate the plugin.
3. Edit **Rewards per day**. Each entry has a day of the month (1 to 31) and
   the amounts of zen, WCoinC, WCoinP and Goblin Points. Optionally choose an
   item with its level, quantity (stackable items such as potions; 0 gives a
   full stack), skill, luck, option level (1 is +4) and excellent options.
   Excellent options are flags like those of the GM item command: 1, 2, 4, 8,
   16 and 32 for the first to sixth option, added together. A skin receives the
   allowed bonus of its flag.
4. Save.

A day without an entry, or with an entry that grants nothing, has no reward.
Days 29 to 31 only appear in months which have them. Each day can be
configured once; the settings are rejected otherwise.

The claimed days are stored with the account. Existing servers need the
`DailyLoginRewards` database migration, which the server applies on start when
automatic schema updates are enabled.
