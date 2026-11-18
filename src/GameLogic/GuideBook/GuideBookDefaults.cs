// <copyright file="GuideBookDefaults.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.GuideBook;

/// <summary>
/// The guide which a new server starts with: the server's own features, as the client of this server
/// shows them. Game masters change it in the game afterwards.
/// </summary>
/// <remarks>
/// The pages quote the live settings through the keys of <see cref="GuideBookValues"/>, e.g. "{exc.base}",
/// so they stay right when an administrator changes a rate.
/// </remarks>
internal static class GuideBookDefaults
{
    private const string SystemsCategory = "cat-systems";

    /// <summary>Creates the default guide.</summary>
    /// <returns>The default guide.</returns>
    public static GuideBookContent Create() => new()
    {
        Categories = new List<GuideBookCategory>
        {
            new() { Id = "cat-start", Name = "Getting Started" },
            new() { Id = "cat-systems", Name = "Game Systems" },
            new() { Id = "cat-rewards", Name = "Rewards" },
            new() { Id = "cat-events", Name = "Events" },
        },
        Pages = new List<GuideBookPage>
        {
            new()
            {
                Id = "page-welcome",
                CategoryId = "cat-start",
                Title = "Welcome, Adventurer",
                Body = "![Interface/RmlUi/assets/guide/welcome.tga]\n"
                    + "Welcome to the Guide Book. Here you find how this server's systems, rewards and events work.\n"
                    + "\n"
                    + "## Finding your way\n"
                    + "- Open the book with **F1** or the book beside the MU Helper's start button.\n"
                    + "- Pick a category on the left, then a page.\n"
                    + "- Type in **Search** to find every page that mentions a word.\n"
                    + "- Gold underlined words open another page, like [[page-excellent|Excellent Combination]].\n"
                    + "\n"
                    + "> The **Controls** category lists every hotkey and chat command of the game.",
            },
            new()
            {
                Id = "page-menu",
                CategoryId = "cat-start",
                Title = "Where to Find Things",
                Body = "Most of the server's own windows open from the **window menu** (the menu button on the bottom bar).\n"
                    + "\n"
                    + "- **Excellent Combination**: move an Excellent option between two items. See [[page-excellent|Excellent Combination]].\n"
                    + "- **Event Timers**: when the next Blood Castle, Devil Square and Chaos Castle open.\n"
                    + "- **Daily Rewards**: the month's login calendar. See [[page-daily|Daily Rewards]].\n"
                    + "- **Muun Inventory**, **Jewel Bank** and **Rank Shop**.\n"
                    + "\n"
                    + "> While today's daily reward is unclaimed, its icon sits above the mana gauge: click it to claim.",
            },
            new()
            {
                Id = "page-excellent",
                CategoryId = "cat-systems",
                Title = "Excellent Combination",
                Body = "![Interface/RmlUi/assets/guide/excellent_combination.tga]\n"
                    + "Move one **Excellent option** from a second item onto your main gear.\n"
                    + "\n"
                    + "## How to combine\n"
                    + "1. Open **Menu > Excellent Combination**.\n"
                    + "2. Drag your **Main Gear** and the **Secondary Gear** from your bag into the two cards. Nothing moves until you press Stack.\n"
                    + "3. Optionally add [[page-core|Resonance Cores]] to raise the success rate.\n"
                    + "4. Press **Stack**.\n"
                    + "\n"
                    + "## Rates and cost\n"
                    + "- Base success rate: **{exc.base}%**\n"
                    + "- Each Resonance Core: **+{exc.bonus}%**, up to **{exc.max}%**\n"
                    + "- Cost per attempt: **{exc.zen} Zen**\n"
                    + "\n"
                    + "## Rules\n"
                    + "- Both items must be the same equipment type and different items in your bag.\n"
                    + "- Main Gear needs one or two Excellent options; Secondary Gear needs exactly one option that Main Gear does not have.\n"
                    + "- Ancient, socket, timed and listed items cannot be combined.\n"
                    + "- With two options already, success replaces one of them at random.\n"
                    + "\n"
                    + "> Success and failure both use up the Secondary Gear, the cores and the Zen. **A failure never changes your Main Gear.**",
            },
            new()
            {
                Id = "page-core",
                CategoryId = "cat-systems",
                Title = "Resonance Core",
                Body = "{{item 14 252}}\n"
                    + "The **Resonance Core** is a violet crystal that raises the success rate of an [[page-excellent|Excellent Combination]].\n"
                    + "\n"
                    + "## Where to get it\n"
                    + "- **Bosses**: a boss can drop a core when it dies, with a **{core.boss}%** chance, but each boss drops at most one core a day.\n"
                    + "- **Any other monster**: a very rare drop, **{core.monster}%** per kill.\n"
                    + "- The core drops at the monster's feet for the player who landed the **killing blow** and their party.\n"
                    + "\n"
                    + "{{boss-rewards}}\n"
                    + "\n"
                    + "## Using cores\n"
                    + "- Tap the core icon in the Excellent Combination window and choose how many to use.\n"
                    + "- Each core adds **{exc.bonus}%** to the base **{exc.base}%**, up to **{exc.max}%**.\n"
                    + "- Only cores in your normal bag count. Cores stack up to 255 in one slot.\n"
                    + "\n"
                    + "> Cores are used up whether the combination succeeds or fails.",
            },
            new()
            {
                Id = "page-skins",
                CategoryId = "cat-systems",
                Title = "Skin Shop & Mounts",
                Body = "The **Skin Shop** sells armor, weapon, wing, pet and mount skins.\n"
                    + "\n"
                    + "- Press **Buy**, choose the purchase level from +0 to +15 and a bonus, then confirm.\n"
                    + "- Bonuses only raise HP, mana or Zen: armor and shields give max HP, max mana or Zen; weapons give HP or mana after a kill; wings give full HP or mana recovery.\n"
                    + "- Drag the character in the preview to turn it.\n"
                    + "\n"
                    + "## Skin Inventory\n"
                    + "Drag a skin from your bag into the **Skin Inventory** to store it, or choose **Remove** to take it back.\n"
                    + "\n"
                    + "> Mounts are open to every class from its first class on. Class wings appear only for their class.",
            },
            new()
            {
                Id = "page-helper",
                CategoryId = "cat-systems",
                Title = "MU Helper",
                Body = "The **MU Helper** hunts for you around the spot where you start it.\n"
                    + "\n"
                    + "1. Press the gear beside the map position at the top left to open its settings.\n"
                    + "2. Choose a primary attack, and optionally more attacks, buffs and potions.\n"
                    + "3. Press the start button beside it, or the **Home** key.\n"
                    + "\n"
                    + "- Without Combo, extra attacks without Delay or Condition take turns with the primary attack.\n"
                    + "- Skills you have not learned or that are cooling down are skipped.\n"
                    + "- A healing potion is drunk first whenever your HP is below the threshold.",
            },
            OfflineLevelingPage(),
            ItemAttributesPage(),
            new()
            {
                Id = "page-bosses",
                CategoryId = "cat-rewards",
                Title = "Boss Bounties",
                Body = "![Interface/RmlUi/assets/guide/boss_rewards.tga]\n"
                    + "Bosses carry a bounty of **WCoin** and **Goblin Points**, and they can drop a [[page-core|Resonance Core]].\n"
                    + "\n"
                    + "{{boss-rewards}}\n"
                    + "\n"
                    + "## Who gets it\n"
                    + "- The coins go only to the player who lands the **killing blow**. Party members and other attackers get none of them.\n"
                    + "- The coins are added to your account at once, and the server announces who killed the boss.\n"
                    + "- A core drops at the boss's feet for the killer and their party, at most once per boss a day.\n"
                    + "\n"
                    + "> Coins go to the account, so every character of that account can spend them in the cash shop.",
            },
            new()
            {
                Id = "page-daily",
                CategoryId = "cat-rewards",
                Title = "Daily Rewards",
                Body = "Log in every day and claim the day's reward in the **Daily Rewards** window.\n"
                    + "\n"
                    + "- The window opens by itself when you enter the game while today's reward is unclaimed.\n"
                    + "- Open it any time from the window menu or the icon above the mana gauge.\n"
                    + "- Only today can be claimed. A day that passes unclaimed is lost, and the calendar starts over every month.\n"
                    + "- One claim per day for the whole account.\n"
                    + "- Days follow the **server's** time.\n"
                    + "\n"
                    + "> If the item does not fit in your bag, nothing is granted and the day stays claimable.",
            },
            new()
            {
                Id = "page-coins",
                CategoryId = "cat-rewards",
                Title = "WCoin & Goblin Points",
                Body = "![Interface/RmlUi/assets/guide/wcoin.tga]\n"
                    + "**WCoinC**, **WCoinP** and **Goblin Points** are your account's cash shop currencies.\n"
                    + "\n"
                    + "## How to earn them\n"
                    + "- [[page-bosses|Boss Bounties]]: the boss's killer earns them.\n"
                    + "- [[page-daily|Daily Rewards]]: some days pay coins.\n"
                    + "\n"
                    + "## How to spend them\n"
                    + "Open the cash shop and choose the currency to pay with.\n"
                    + "\n"
                    + "![Interface/RmlUi/assets/guide/goblin_points.tga]",
            },
            new()
            {
                Id = "page-bc",
                CategoryId = "cat-events",
                Title = "Blood Castle",
                Body = "Defeat the castle's monsters, break the gate, slay the magic skeletons and destroy the statue, then return the **Archangel's weapon** before the time runs out.\n"
                    + "\n"
                    + "- The event panel at the right edge shows the time left and every objective in order.\n"
                    + "- Finished objectives are struck through; the current one is highlighted.\n"
                    + "- Check **Menu > Event Timers** for when the next castle opens.\n"
                    + "\n"
                    + "> Bring potions and a party: the castle's monsters get stronger with its level.",
            },
            new()
            {
                Id = "page-ds",
                CategoryId = "cat-events",
                Title = "Devil Square",
                Body = "Survive the waves of monsters and score points.\n"
                    + "\n"
                    + "- The event panel shows each wave, when the next one starts, your points and your rank.\n"
                    + "- Check **Menu > Event Timers** for when Devil Square opens.",
            },
            new()
            {
                Id = "page-cc",
                CategoryId = "cat-events",
                Title = "Chaos Castle",
                Body = "The last character standing wins.\n"
                    + "\n"
                    + "- The event panel counts the characters remaining.\n"
                    + "- When the match begins every window closes and the chat stays closed until it ends.\n"
                    + "- Check **Menu > Event Timers** for when Chaos Castle opens.",
            },
        },
    };

    /// <summary>
    /// Adds the pages which came after the first default guide to a guide made before them, once: a page which a
    /// game master deletes afterwards stays deleted. The change is saved with the guide's next save.
    /// </summary>
    /// <param name="settings">The settings.</param>
    public static void AddLaterPages(GuideBookSettings settings)
    {
        lock (settings)
        {
            var added = false;
            foreach (var page in new[] { OfflineLevelingPage(), ItemAttributesPage() })
            {
                if (settings.AddedDefaultPages.Contains(page.Id))
                {
                    continue;
                }

                settings.AddedDefaultPages.Add(page.Id);
                added |= Add(settings.Published, page) | Add(settings.Draft, page);
            }

            if (added)
            {
                // A new revision, so clients show the pages; the draft stays published if it was.
                var unpublished = settings.HasUnpublishedChanges();
                settings.Published.Revision++;
                settings.Draft.Revision = unpublished ? settings.Draft.Revision + 1 : settings.Published.Revision;
            }
        }
    }

    private static bool Add(GuideBookContent content, GuideBookPage page)
    {
        if (content.Pages.Any(existing => existing.Id == page.Id) || content.Pages.Count >= GuideBookContent.MaximumPages)
        {
            return false;
        }

        if (content.Categories.All(category => category.Id != page.CategoryId))
        {
            if (content.Categories.Count >= GuideBookContent.MaximumCategories)
            {
                return false;
            }

            content.Categories.Add(new GuideBookCategory { Id = SystemsCategory, Name = "Game Systems" });
        }

        content.Pages.Add(new GuideBookPage { Id = page.Id, CategoryId = page.CategoryId, Title = page.Title, Body = page.Body });
        return true;
    }

    // The prices, the durations and the protection are the live settings: GuideBookLivePages.Expand writes them in.
    private static GuideBookPage OfflineLevelingPage() => new()
    {
        Id = "page-offline",
        CategoryId = SystemsCategory,
        Title = "Offline Leveling",
        Body = "Keep leveling while you are offline: your character stays in the world and the **MU Helper** keeps hunting.\n"
            + "\n"
            + "## How to start\n"
            + "1. Stand where you want to hunt and start the [[page-helper|MU Helper]].\n"
            + "2. Type **/offlevel** in the chat.\n"
            + "3. In the **Offline Leveling** window choose how to pay, how long and the protection, then press **Start**.\n"
            + "4. The game closes and your character keeps hunting. Log in again to stop it.\n"
            + "\n"
            + "## Prices\n"
            + "{offline.prices}\n"
            + "\n"
            + "## Duration\n"
            + "{offline.duration}\n"
            + "\n"
            + "## Protection\n"
            + "{offline.protection}\n"
            + "\n"
            + "> You must be alive, on a map, not in an event, and the MU Helper must be running.",
    };

    // The items and their attributes are the live settings: GuideBookLivePages.Expand writes them in.
    private static GuideBookPage ItemAttributesPage() => new()
    {
        Id = "page-item-attributes",
        CategoryId = SystemsCategory,
        Title = "Custom Item Attributes",
        Body = "Some of this server's custom items, like the custom mounts, carry extra attributes. Their tooltip shows them in **purple**.\n"
            + "\n"
            + "{items.attributes}\n"
            + "\n"
            + "## In Combat statistics\n"
            + "While you wear such an item, **Combat statistics** (Character window, **Statistics**) shows a statistic without it and, in purple, what the item adds: e.g. **Defense (PvM): 1676.57 (+250)**.\n"
            + "- Attributes which no statistic shows, like the movement speed, get a purple line of their own.",
    };
}
