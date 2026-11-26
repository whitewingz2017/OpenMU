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
- A reward can include up to eight items, Zen, WCoinC, WCoinP and Goblin Points.
  Players receive every item in a bundle or exactly one item on a choice day.
  Both modes grant all configured currency. Zen and items go to the claiming
  character, while coins go to the account.
- If the granted items cannot fit into the inventory or the Zen would exceed
  the carry limit, nothing is granted and the day stays claimable.

The window opens by itself when a character enters the game while today's
reward is still unclaimed. Players can also open it from **DAILY** in the
top-right button row (modern theme) or **Daily Rewards** in the window menu.
Today's card is gold, claimed days are green and missed days are red.

## Configuration

The feature is disabled by default. In the admin panel, open
**Configuration > Daily Login Rewards**:

1. Click **Activate**.
2. The page shows the month as a calendar of 31 slots. Green slots have a
   reward and list it; red slots are empty.
3. Click a day and set its Zen, WCoinC, WCoinP and Goblin Points. Use **Add item**
   for each item, searching by name or `group:number`. Click an item in the list
   to edit its level, stack quantity, skill, luck and options. A skin can be
   assigned one of its allowed bonuses.
4. Set **Item duration (hours)** for each item. Enter `24` for one day or `0`
   for permanent. The timer starts on claim and continues offline. Timed items
   require the **Item expiration** plugin.
5. Check **Let the player choose one item** if the day offers two or more
   alternatives. Leave it unchecked to grant every item as a bundle.
6. Click **Save changes**. **Clear day** removes all rewards from that day.
   **Discard changes** reloads the saved rewards.

Multi-item days require an updated client. Older clients can still claim
single-item and currency-only days.

A red slot has no reward. Days 29 to 31 only appear in months which have
them.

The claimed days are stored with the account. Existing servers need the
`DailyLoginRewards` database migration, which the server applies on start when
automatic schema updates are enabled.
